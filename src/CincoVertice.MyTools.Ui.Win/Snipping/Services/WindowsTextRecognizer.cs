using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Services;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     Reads text with the OCR built into Windows, offline, in the first of the user's Windows languages that it can
///     read.
/// </summary>
public sealed class WindowsTextRecognizer : ITextRecognizer
{
    public const string NoLanguageMessage =
        "Windows can't read text in your languages. Add one with its optical character recognition in Settings, "
        + "Time & language, Language & region.";

    // Text on the screen is small for the engine: read this much larger, it misses fewer letters
    private const double Enlargement = 2;

    public async Task<IReadOnlyList<RecognizedLineModel>> ReadLinesAsync(byte[] image)
    {
        OcrEngine engine = OcrEngine.TryCreateFromUserProfileLanguages()
            ?? throw new TextRecognitionException(NoLanguageMessage);

        try
        {
            (SoftwareBitmap bitmap, double scale) = await DecodeAsync(image);

            using (bitmap)
            {
                OcrResult result = await engine.RecognizeAsync(bitmap);

                // In the image's own pixels
                return
                [
                    .. result.Lines.Select(line => new RecognizedLineModel(
                    [
                        .. line.Words.Select(word => new RecognizedWordModel(
                            word.Text,
                            word.BoundingRect.X / scale,
                            word.BoundingRect.Width / scale)),
                    ])),
                ];
            }
        }
        catch (Exception ex) when (ex is not TextRecognitionException)
        {
            throw new TextRecognitionException($"Could not read the text: {ex.Message}", ex);
        }
    }

    /// <returns>The image, and how much larger it was made.</returns>
    private static async Task<(SoftwareBitmap Bitmap, double Scale)> DecodeAsync(byte[] image)
    {
        using var stream = new MemoryStream(image);
        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());

        // Never larger than the engine reads
        double scale = Math.Min(
            Enlargement,
            Math.Min(
                (double)OcrEngine.MaxImageDimension / decoder.PixelWidth,
                (double)OcrEngine.MaxImageDimension / decoder.PixelHeight));

        var transform = new BitmapTransform
        {
            ScaledWidth = Math.Max(1, (uint)(decoder.PixelWidth * scale)),
            ScaledHeight = Math.Max(1, (uint)(decoder.PixelHeight * scale)),
            InterpolationMode = BitmapInterpolationMode.Fant,
        };

        SoftwareBitmap bitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            transform,
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);

        return (bitmap, scale);
    }
}
