using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     Records an area of the screen to an MP4 file (H.264), with the encoder built into Windows, which uses the
///     graphics card when it can. Windows asks for each frame when it's ready for it; the area is copied then, at most
///     <see cref="FramesPerSecond"/> times a second, and timed by the clock, so the video plays as fast as it happened.
///     One recording per instance.
/// </summary>
public sealed class ScreenRecorder : IScreenRecorder
{
    public const int FramesPerSecond = 20;

    // Bits per pixel and second the encoder may use: enough for sharp text, which screen recordings are mostly about
    private const double BitsPerPixel = 0.25;

    private static readonly TimeSpan _frameTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / FramesPerSecond);

    private readonly Stopwatch _clock = new();

    private Rectangle _area;
    private Task? _transcoding;
    private IRandomAccessStream? _output;
    private long _frames;
    private volatile bool _isStopping;

    public TimeSpan Elapsed => _clock.Elapsed;

    public async Task StartAsync(Rectangle area, string path)
    {
        // The encoder takes only even sizes
        _area = new Rectangle(area.X, area.Y, Math.Max(2, area.Width & ~1), Math.Max(2, area.Height & ~1));
        uint width = (uint)_area.Width;
        uint height = (uint)_area.Height;

        VideoEncodingProperties frames = VideoEncodingProperties.CreateUncompressed(
            MediaEncodingSubtypes.Bgra8,
            width,
            height);
        frames.FrameRate.Numerator = FramesPerSecond;
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
        profile.Video.FrameRate.Numerator = FramesPerSecond;
        profile.Video.FrameRate.Denominator = 1;
        profile.Video.Bitrate = (uint)Math.Clamp(
            width * height * FramesPerSecond * BitsPerPixel,
            1_000_000,
            40_000_000);

        try
        {
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

    public async Task StopAsync()
    {
        _isStopping = true;

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

    // Windows asks for the next frame; it gets one when it's due, or none once stopped, which ends the video
    private void OnSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
    {
        MediaStreamSourceSampleRequest request = args.Request;
        MediaStreamSourceSampleRequestDeferral deferral = request.GetDeferral();

        _ = Task.Run(async () =>
        {
            try
            {
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
                MediaStreamSample sample = MediaStreamSample.CreateFromBuffer(CaptureFrame().AsBuffer(), time);
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
    private byte[] CaptureFrame()
    {
        Bitmap frame;

        // Physical pixels on every monitor, as the area is
        using (DpiAwarenessScope.PerMonitorV2())
        {
            frame = ScreenCapture.Capture(_area);
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
        _output?.Dispose();
        _output = null;
    }
}
