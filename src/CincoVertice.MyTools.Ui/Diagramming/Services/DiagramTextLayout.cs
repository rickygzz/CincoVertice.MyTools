using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     Splits a text into lines. An SVG file has no word wrap, so the lines are worked out here, from an estimate of
///     the text's width, and the screen and the file both draw these same lines.
/// </summary>
public static class DiagramTextLayout
{
    private const float AverageCharacterWidth = 0.55f;
    private const float LineHeightFactor = 1.25f;

    /// <summary>An estimate of how wide a line is, in pixels.</summary>
    public static float WidthOf(string line, int fontSize) => line.Length * fontSize * AverageCharacterWidth;

    /// <summary>The lines of a text centered in an area, wrapped to its width.</summary>
    public static DiagramTextBlockModel Layout(string text, RectangleF area, int fontSize)
    {
        IReadOnlyList<string> lines = Wrap(text, area.Width, fontSize);

        return Place(lines, area.Left + area.Width / 2, area.Top + area.Height / 2, fontSize);
    }

    /// <summary>The lines of a text centered on a point, wrapped only at its own line breaks.</summary>
    public static DiagramTextBlockModel LayoutAt(string text, PointF center, int fontSize) =>
        Place(text.ReplaceLineEndings("\n").Split('\n'), center.X, center.Y, fontSize);

    /// <summary>Breaks at spaces; a word too long for a whole line is broken too.</summary>
    public static IReadOnlyList<string> Wrap(string text, float width, int fontSize)
    {
        int maxCharacters = Math.Max(1, (int)(width / (fontSize * AverageCharacterWidth)));
        List<string> lines = [];

        foreach (string paragraph in text.ReplaceLineEndings("\n").Split('\n'))
        {
            string line = string.Empty;

            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string rest = word;

                if (line.Length > 0 && line.Length + 1 + rest.Length <= maxCharacters)
                {
                    line += " " + rest;

                    continue;
                }

                if (line.Length > 0)
                {
                    lines.Add(line);
                }

                while (rest.Length > maxCharacters)
                {
                    lines.Add(rest[..maxCharacters]);
                    rest = rest[maxCharacters..];
                }

                line = rest;
            }

            lines.Add(line);
        }

        return lines;
    }

    private static DiagramTextBlockModel Place(IReadOnlyList<string> lines, float centerX, float centerY, int fontSize)
    {
        float lineHeight = fontSize * LineHeightFactor;
        float height = lines.Count * lineHeight;
        float width = lines.Max(line => WidthOf(line, fontSize));
        float top = centerY - height / 2;

        return new DiagramTextBlockModel(
            lines,
            centerX,
            top,
            lineHeight,
            new RectangleF(centerX - width / 2, top, width, height));
    }
}
