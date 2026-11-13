using System.Runtime.InteropServices;
using System.Text;
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
            await WriteGifAsync(temporaryPath, path, settings);
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
        TimeSpan step = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / settings.GifFramesPerSecond);
        List<TimeSpan> times = [];

        for (TimeSpan time = TimeSpan.Zero; time < clip.OriginalDuration; time += step)
        {
            times.Add(time);
        }

        if (times.Count == 0)
        {
            times.Add(TimeSpan.Zero);
        }

        Windows.Media.MediaProperties.VideoEncodingProperties properties = clip.GetVideoEncodingProperties();
        double scale = Math.Min(1, (double)settings.GifMaxWidth / properties.Width);
        int width = Math.Max(1, (int)(properties.Width * scale));
        int height = Math.Max(1, (int)(properties.Height * scale));

        IReadOnlyList<ImageStream> frames = await composition.GetThumbnailsAsync(
            times.ToArray(),
            width,
            height,
            VideoFramePrecision.NearestFrame);

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

        // In hundredths of a second, as GIFs keep it
        ushort delay = (ushort)(100 / settings.GifFramesPerSecond);

        // The encoder writes a frame's pixels only when it goes to the next frame, or at the end: each is kept till
        // then
        SoftwareBitmap? previous = null;

        try
        {
            for (int i = 0; i < frames.Count; i++)
            {
                if (i > 0)
                {
                    await encoder.GoToNextFrameAsync();
                    previous?.Dispose();
                }

                using ImageStream frame = frames[i];
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(frame);
                previous = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

                encoder.SetSoftwareBitmap(previous);
                await encoder.BitmapProperties.SetPropertiesAsync(new KeyValuePair<string, BitmapTypedValue>[]
                {
                    new("/grctlext/Delay", new BitmapTypedValue(delay, Windows.Foundation.PropertyType.UInt16)),
                });
            }

            await encoder.FlushAsync();
        }
        finally
        {
            previous?.Dispose();
        }
    }
}
