using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Models;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     Records an area of the screen to an MP4 file (H.264), with the encoder built into Windows, which uses the
///     graphics card when it can. Windows asks for each frame when it's ready for it; the area is copied then, at most
///     as many times a second as the settings say, and timed by the clock, so the video plays as fast as it happened;
///     paused, the clock stops too. The mouse pointer is drawn in, and as the settings say, rings where it's clicked
///     and the shortcuts pressed. One recording per instance; start and stop it on the UI thread, where the clicks and
///     keys come in.
/// </summary>
public sealed class ScreenRecorder : IScreenRecorder
{
    private readonly Stopwatch _clock = new();
    private readonly RecordingOverlay _overlay = new();

    private RecordingSettingsModel _settings = RecordingSettingsModel.Default;
    private TimeSpan _frameTime;
    private Rectangle _area;
    private Task? _transcoding;
    private IRandomAccessStream? _output;
    private InputHook? _input;
    private long _frames;
    private volatile bool _isStopping;
    private volatile bool _isPaused;

    public TimeSpan Elapsed => _clock.Elapsed;

    public async Task StartAsync(Rectangle area, string path, RecordingSettingsModel settings)
    {
        _settings = settings;
        _frameTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / settings.FramesPerSecond);

        // The encoder takes only even sizes
        _area = new Rectangle(area.X, area.Y, Math.Max(2, area.Width & ~1), Math.Max(2, area.Height & ~1));
        uint width = (uint)_area.Width;
        uint height = (uint)_area.Height;
        uint rate = (uint)settings.FramesPerSecond;

        VideoEncodingProperties frames = VideoEncodingProperties.CreateUncompressed(
            MediaEncodingSubtypes.Bgra8,
            width,
            height);
        frames.FrameRate.Numerator = rate;
        frames.FrameRate.Denominator = 1;

        var source = new MediaStreamSource(new VideoStreamDescriptor(frames)) { BufferTime = TimeSpan.Zero };
        source.Starting += (_, e) => e.Request.SetActualStartPosition(TimeSpan.Zero);
        source.SampleRequested += OnSampleRequested;

        // Not Auto: with it, the graphics card's encoder refuses the frames (MF_E_TRANSFORM_TYPE_NOT_SET); the size,
        // rate and bitrate are set below anyway
        MediaEncodingProfile profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD720p);
        profile.Audio = null;
        profile.Video.Width = width;
        profile.Video.Height = height;
        profile.Video.FrameRate.Numerator = rate;
        profile.Video.FrameRate.Denominator = 1;
        profile.Video.Bitrate = (uint)Math.Clamp(
            width * height * rate * BitsPerPixel(settings.Quality),
            500_000,
            40_000_000);

        try
        {
            StartWatchingInput();

            // Windows' own file stream: the media pipeline needs more of a stream than a wrapped FileStream gives
            File.Create(path).Dispose();
            StorageFile file = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(path));
            _output = await file.OpenAsync(FileAccessMode.ReadWrite);

            PrepareTranscodeResult prepared;

            try
            {
                prepared = await new MediaTranscoder { HardwareAccelerationEnabled = true }
                    .PrepareMediaStreamSourceTranscodeAsync(source, _output, profile);
            }
            catch (COMException)
            {
                // A graphics card whose encoder takes none of it: Windows' own encoder, on the processor
                prepared = await new MediaTranscoder { HardwareAccelerationEnabled = false }
                    .PrepareMediaStreamSourceTranscodeAsync(source, _output, profile);
            }

            if (!prepared.CanTranscode)
            {
                throw new InvalidOperationException($"Windows can't record video: {prepared.FailureReason}.");
            }

            _clock.Start();
            _transcoding = prepared.TranscodeAsync().AsTask();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or COMException)
        {
            Close();

            throw new InvalidOperationException($"Could not start recording: {ex.Message} (0x{ex.HResult:X8})", ex);
        }
        catch (InvalidOperationException)
        {
            Close();

            throw;
        }
    }

    public void Pause()
    {
        _isPaused = true;
        _clock.Stop();
    }

    public void Resume()
    {
        _isPaused = false;
        _clock.Start();
    }

    public async Task StopAsync()
    {
        _isStopping = true;
        _isPaused = false;
        _input?.Dispose();
        _input = null;

        try
        {
            if (_transcoding is not null)
            {
                await _transcoding;
            }
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException)
        {
            throw new InvalidOperationException($"Could not finish the video: {ex.Message}", ex);
        }
        finally
        {
            _clock.Stop();
            Close();
        }
    }

    // How many bits the encoder may use per pixel of each frame: enough for sharp text at Medium
    private static double BitsPerPixel(RecordingQuality quality) => quality switch
    {
        RecordingQuality.Low => 0.1,
        RecordingQuality.High => 0.5,
        _ => 0.25,
    };

    // The clicks and the shortcuts, as the settings say; on this thread, the UI thread, whose messages bring them
    private void StartWatchingInput()
    {
        if (!_settings.ShowClicks && !_settings.ShowKeys)
        {
            return;
        }

        try
        {
            _input = new InputHook();
        }
        catch (Win32Exception)
        {
            // Recorded without them, rather than not at all
            return;
        }

        if (_settings.ShowClicks)
        {
            _input.MouseDown += (_, click) =>
            {
                if (!_isPaused)
                {
                    _overlay.AddClick(_clock.Elapsed, click.Point, click.Button);
                }
            };
        }

        if (_settings.ShowKeys)
        {
            _input.KeyDown += (_, key) =>
            {
                if (!_isPaused && KeyComboText.From(key, CurrentModifiers()) is string text)
                {
                    _overlay.AddKeys(_clock.Elapsed, text);
                }
            };
        }
    }

    private static Keys CurrentModifiers()
    {
        Keys modifiers = Control.ModifierKeys;

        // Control.ModifierKeys has no Win
        if (IsDown(Keys.LWin) || IsDown(Keys.RWin))
        {
            modifiers |= Keys.LWin;
        }

        return modifiers;
    }

    private static bool IsDown(Keys key) =>
        (Common.WinApi.Libs.User32.GetAsyncKeyState((Common.WinApi.Libs.Enums.KeyCode)key) & 0x8000) != 0;

    // Windows asks for the next frame; it gets one when it's due, or none once stopped, which ends the video
    private void OnSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
    {
        MediaStreamSourceSampleRequest request = args.Request;
        MediaStreamSourceSampleRequestDeferral deferral = request.GetDeferral();

        _ = Task.Run(async () =>
        {
            try
            {
                // Paused: nothing is added, and the clock waits too
                while (_isPaused && !_isStopping)
                {
                    await Task.Delay(50);
                }

                TimeSpan due = TimeSpan.FromTicks(_frames * _frameTime.Ticks);
                TimeSpan wait = due - _clock.Elapsed;

                if (wait > TimeSpan.Zero && !_isStopping)
                {
                    await Task.Delay(wait);
                }

                // At least one frame, so even the shortest recording is a video
                if (_isStopping && _frames > 0)
                {
                    request.Sample = null;

                    return;
                }

                // When it happened: a slow copy makes the next frames later, not the video faster
                TimeSpan time = _clock.Elapsed;
                MediaStreamSample sample = MediaStreamSample.CreateFromBuffer(CaptureFrame(time).AsBuffer(), time);
                sample.Duration = _frameTime;
                request.Sample = sample;
                _frames = Math.Max(_frames + 1, (long)(time.Ticks / _frameTime.Ticks) + 1);
            }
            catch (Exception ex) when (ex is ExternalException or ArgumentException or InvalidOperationException)
            {
                // A frame that can't be copied, e.g. while the desktop is switching, ends the video there
                request.Sample = null;
            }
            finally
            {
                deferral.Complete();
            }
        });
    }

    /// <returns>
    ///     The area's pixels, blue, green, red and alpha, row by row from the bottom: the encoder takes uncompressed
    ///     frames bottom up, as Windows bitmaps are.
    /// </returns>
    private byte[] CaptureFrame(TimeSpan time)
    {
        Bitmap frame;

        // Physical pixels on every monitor, as the area is, the pointer's position too
        using (DpiAwarenessScope.PerMonitorV2())
        {
            frame = ScreenCapture.Capture(_area);

            using var graphics = Graphics.FromImage(frame);
            _overlay.Draw(graphics, _area, time);

            // A copy of the screen doesn't have the pointer: Windows draws it over the screen apart
            CursorPainter.Draw(graphics, _area.Location);
        }

        using (frame)
        {
            int stride = frame.Width * 4;
            byte[] pixels = new byte[stride * frame.Height];
            BitmapData data = frame.LockBits(
                new Rectangle(Point.Empty, frame.Size),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                for (int y = 0; y < frame.Height; y++)
                {
                    Marshal.Copy(data.Scan0 + (y * data.Stride), pixels, (frame.Height - 1 - y) * stride, stride);
                }
            }
            finally
            {
                frame.UnlockBits(data);
            }

            return pixels;
        }
    }

    private void Close()
    {
        _input?.Dispose();
        _input = null;
        _output?.Dispose();
        _output = null;
    }
}
