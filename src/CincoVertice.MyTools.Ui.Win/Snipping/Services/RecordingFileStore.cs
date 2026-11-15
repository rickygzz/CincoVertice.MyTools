using System.Runtime.InteropServices;
using System.Text;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Models;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     Records to a file of its own in the temporary folder, then moves it where the user chose, or makes it a GIF
///     there: the video's frames at the GIF's own rate, made smaller to its width at most, playing over and over.
/// </summary>
public sealed class RecordingFileStore : IRecordingFileStore
{
    private const Windows.Foundation.PropertyType UInt8Array = Windows.Foundation.PropertyType.UInt8Array;

    public string CreateTemporaryPath() =>
        Path.Combine(Path.GetTempPath(), $"MyTools recording {Guid.NewGuid():N}.mp4");

    public async Task KeepAsync(string temporaryPath, string path, RecordingSettingsModel settings)
    {
        if (!path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
        {
            File.Move(temporaryPath, path, overwrite: true);

            return;
        }

        try
        {
            await AtomicFile.WriteAsync(path, outputPath => WriteGifAsync(temporaryPath, outputPath, settings));
        }
        catch (COMException ex)
        {
            throw new InvalidOperationException($"the video couldn't be made a GIF ({ex.Message.Trim()}).", ex);
        }

        Discard(temporaryPath);
    }

    public void Discard(string temporaryPath)
    {
        try
        {
            File.Delete(temporaryPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Left in the temporary folder, which Windows cleans up
        }
    }

    private static async Task WriteGifAsync(string videoPath, string gifPath, RecordingSettingsModel settings)
    {
        StorageFile video = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(videoPath));
        MediaClip clip = await MediaClip.CreateFromFileAsync(video);
        var composition = new MediaComposition();
        composition.Clips.Add(clip);

        // The frames to keep, and their size: no wider than the settings allow, as tall as that keeps them
        Windows.Media.MediaProperties.VideoEncodingProperties properties = clip.GetVideoEncodingProperties();
        double scale = Math.Min(1, (double)settings.GifMaxWidth / properties.Width);
        int width = Math.Max(1, (int)(properties.Width * scale));
        int height = Math.Max(1, (int)(properties.Height * scale));

        File.Create(gifPath).Dispose();
        StorageFile gif = await StorageFile.GetFileFromPathAsync(Path.GetFullPath(gifPath));

        using IRandomAccessStream output = await gif.OpenAsync(FileAccessMode.ReadWrite);
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.GifEncoderId, output);

        // Over and over: the Netscape extension, with 0 for a loop count that never ends
        // Arrays: Windows can't take the lists C# makes of collection expressions
        await encoder.BitmapContainerProperties.SetPropertiesAsync(new KeyValuePair<string, BitmapTypedValue>[]
        {
            new("/appext/application", new BitmapTypedValue(Encoding.ASCII.GetBytes("NETSCAPE2.0"), UInt8Array)),
            new("/appext/data", new BitmapTypedValue(new byte[] { 3, 1, 0, 0, 0 }, UInt8Array)),
        });

        // The encoder writes a frame's pixels only when it goes to the next frame, or at the end: each is kept till
        // then
        SoftwareBitmap? previous = null;

        try
        {
            await GifFrameSequence.WriteAsync<ImageStream>(
                GifFrames(clip.OriginalDuration, settings.GifFramesPerSecond),
                async times => await composition.GetThumbnailsAsync(times, width, height, VideoFramePrecision.NearestFrame),
                async (frame, delay) =>
                {
                    if (previous is not null)
                    {
                        await encoder.GoToNextFrameAsync();
                        previous.Dispose();
                        previous = null;
                    }

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(frame);
                    previous = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                    encoder.SetSoftwareBitmap(previous);
                    await encoder.BitmapProperties.SetPropertiesAsync(new KeyValuePair<string, BitmapTypedValue>[]
                    {
                        new("/grctlext/Delay", new BitmapTypedValue(delay, Windows.Foundation.PropertyType.UInt16)),
                    });
                });

            await encoder.FlushAsync();
        }
        finally
        {
            previous?.Dispose();
        }
    }

    /// <summary>
    ///     Samples on the video's timeline. GIF delays are differences between rounded boundaries, so rounding never
    ///     accumulates, including for frame rates such as 15 and for the last, shorter frame.
    /// </summary>
    internal static IEnumerable<(TimeSpan Time, ushort Delay)> GifFrames(TimeSpan duration, int framesPerSecond)
    {
        const long ticksPerHundredth = TimeSpan.TicksPerSecond / 100;

        if (framesPerSecond is < 2 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(framesPerSecond));
        }

        for (long index = 0; ; index++)
        {
            long start = index * TimeSpan.TicksPerSecond / framesPerSecond;

            if (index > 0 && start >= duration.Ticks)
            {
                yield break;
            }

            long end = Math.Min((index + 1) * TimeSpan.TicksPerSecond / framesPerSecond, duration.Ticks);
            long roundedStart = (start + ticksPerHundredth / 2) / ticksPerHundredth;
            long roundedEnd = (end + ticksPerHundredth / 2) / ticksPerHundredth;

            yield return (TimeSpan.FromTicks(start), (ushort)Math.Max(1, roundedEnd - roundedStart));
        }
    }
}
