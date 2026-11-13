using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Services;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;
using Windows.Media.Ocr;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Snipping;

/// <summary>With the real OCR of Windows, which needs a language with text recognition, as English has.</summary>
public sealed class WindowsTextRecognizerTests
{
    private readonly WindowsTextRecognizer _recognizer = new();

    /// <summary>A white image with the lines in black, as small as text on the screen.</summary>
    private static byte[] Image(int width, params string[] lines)
    {
        using var image = new Bitmap(width, 20 + (lines.Length * 20), PixelFormat.Format24bppRgb);

        using (var graphics = Graphics.FromImage(image))
        using (var font = new Font("Segoe UI", 9f))
        {
            graphics.Clear(Color.White);
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            for (int i = 0; i < lines.Length; i++)
            {
                graphics.DrawString(lines[i], font, Brushes.Black, 10, 10 + (i * 20));
            }
        }

        return PngEncoder.Encode(image);
    }

    [Fact]
    public async Task ReadLinesAsync_ReadsSmallScreenText_LineByLine()
    {
        // Act
        IReadOnlyList<RecognizedLineModel> lines = await _recognizer.ReadLinesAsync(
            Image(300, "The quick brown fox jumps", "over the lazy dog 2026"));

        // Assert
        Assert.Equal(["The quick brown fox jumps", "over the lazy dog 2026"], RecognizedText.Format(lines));
    }

    [Fact]
    public async Task ReadLinesAsync_GivesWhereEachWordIs_SoCodeKeepsItsIndentation()
    {
        // Arrange: code in a font of letters as wide as each other, as in editors
        using var image = new Bitmap(300, 90, PixelFormat.Format24bppRgb);

        using (var graphics = Graphics.FromImage(image))
        using (var font = new Font("Consolas", 10f))
        {
            graphics.Clear(Color.White);
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            graphics.DrawString("if ready then", font, Brushes.Black, 10, 10);
            graphics.DrawString("    run the task", font, Brushes.Black, 10, 30);
            graphics.DrawString("end if", font, Brushes.Black, 10, 50);
        }

        // Act
        IReadOnlyList<RecognizedLineModel> lines = await _recognizer.ReadLinesAsync(PngEncoder.Encode(image));

        // Assert
        Assert.Equal(["if ready then", "    run the task", "end if"], RecognizedText.Format(lines));
    }

    [Fact]
    public async Task ReadLinesAsync_WithoutText_IsEmpty()
    {
        // Act
        IReadOnlyList<RecognizedLineModel> lines = await _recognizer.ReadLinesAsync(Image(200));

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public async Task ReadLinesAsync_AnImageWiderThanTheEngineReads_IsMadeSmallerToFit()
    {
        // Arrange
        int width = (int)OcrEngine.MaxImageDimension + 500;

        // Act
        IReadOnlyList<RecognizedLineModel> lines = await _recognizer.ReadLinesAsync(Image(width, "WIDE"));

        // Assert: no error; the text, small to begin with, may be too small to read once made smaller
        Assert.True(lines.Count <= 1);
    }
}
