using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Win.Diagramming.Services;

/// <summary>
///     Places the pieces of a line in bold or italic one after the other, each as wide as GDI+ measures it in its own
///     font, and the whole line at the left, the middle or the right of where the text goes.
/// </summary>
public static class DiagramTextRuns
{
    // Measures without hinting, at the font's own size: the same widths at any zoom
    private static readonly Lock _measuring = new();
    private static readonly Bitmap _measureImage = new(1, 1);
    private static readonly Graphics _measurer = CreateMeasurer();

    private static readonly StringFormat _format = new(StringFormat.GenericTypographic)
    {
        FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.MeasureTrailingSpaces,
    };

    /// <summary>A format that draws a piece from its left edge, typographic as it's measured.</summary>
    public static StringFormat Format => _format;

    public static FontStyle StyleOf(DiagramTextFormat format) =>
        (format.HasFlag(DiagramTextFormat.Bold) ? FontStyle.Bold : FontStyle.Regular)
        | (format.HasFlag(DiagramTextFormat.Italic) ? FontStyle.Italic : FontStyle.Regular);

    /// <summary>The font in the format; the caller disposes it.</summary>
    public static Font FontOf(Font font, DiagramTextFormat format) =>
        new(font.FontFamily, font.Size, StyleOf(format), GraphicsUnit.Pixel);

    /// <summary>Each piece of the line, with where its left edge is.</summary>
    /// <param name="block">The text the line is in.</param>
    /// <param name="line">Which line.</param>
    /// <param name="font">The text's font, regular.</param>
    public static IReadOnlyList<(DiagramTextRunModel Run, float X)> Placed(
        DiagramTextBlockModel block,
        int line,
        Font font)
    {
        IReadOnlyList<DiagramTextRunModel> runs = block.Runs![line];
        float[] widths = [.. runs.Select(run => WidthOf(run, font))];
        float total = widths.Sum();
        float x = block.Alignment switch
        {
            DiagramTextAlignment.Left => block.X,
            DiagramTextAlignment.Right => block.X - total,
            _ => block.X - total / 2,
        };
        List<(DiagramTextRunModel, float)> placed = [];

        for (int i = 0; i < runs.Count; i++)
        {
            placed.Add((runs[i], x));
            x += widths[i];
        }

        return placed;
    }

    private static float WidthOf(DiagramTextRunModel run, Font font)
    {
        using Font runFont = FontOf(font, run.Format);

        // GDI+ isn't safe to share between threads, and tests draw on several at once
        lock (_measuring)
        {
            return _measurer.MeasureString(run.Text, runFont, PointF.Empty, _format).Width;
        }
    }

    private static Graphics CreateMeasurer()
    {
        Graphics graphics = Graphics.FromImage(_measureImage);

        graphics.PageUnit = GraphicsUnit.Pixel;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        return graphics;
    }
}
