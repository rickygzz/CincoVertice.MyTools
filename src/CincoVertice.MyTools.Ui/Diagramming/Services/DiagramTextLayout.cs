using System.Drawing;
using System.Text.RegularExpressions;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     Splits a text into lines. An SVG file has no word wrap, so the lines are worked out here, from an estimate of
///     the text's width, and the screen and the file both draw these same lines.
/// </summary>
public static class DiagramTextLayout
{
    /// <summary>How wide an image's text can be when the image is narrower, in pixels.</summary>
    public const int ImageTextWidth = 160;

    private const float AverageCharacterWidth = 0.55f;
    private const float LineHeightFactor = 1.25f;
    private const float ImageTextGap = 4;

    private static readonly Regex _spacesThenWord = new("( *)([^ ]+)", RegexOptions.CultureInvariant);

    /// <summary>An estimate of how wide a line is, in pixels.</summary>
    public static float WidthOf(string line, int fontSize) => line.Length * fontSize * AverageCharacterWidth;

    /// <summary>From the top of a line to the top of the next, in pixels.</summary>
    public static float LineHeightOf(int fontSize) => fontSize * LineHeightFactor;

    /// <summary>The lines of a text in the middle of an area, top to bottom, wrapped to its width.</summary>
    /// <param name="text">What to lay out.</param>
    /// <param name="area">Where.</param>
    /// <param name="fontSize">In pixels.</param>
    /// <param name="alignment">Where the lines go across the area.</param>
    public static DiagramTextBlockModel Layout(
        string text,
        RectangleF area,
        int fontSize,
        DiagramTextAlignment alignment = DiagramTextAlignment.Center,
        IReadOnlyList<DiagramTextSpanModel>? spans = null)
    {
        DiagramRichTextModel normalized = DiagramRichText.Normalized(text, spans, trimEnd: false);
        IReadOnlyList<Range> lines = WrapRanges(normalized.Text, area.Width, fontSize);
        float x = alignment switch
        {
            DiagramTextAlignment.Left => area.Left,
            DiagramTextAlignment.Right => area.Right,
            _ => area.Left + area.Width / 2,
        };

        return Place(normalized, lines, x, area.Top + area.Height / 2, fontSize, alignment);
    }

    /// <summary>A shape's text: in the middle of the part of it a text fits in, or under an image.</summary>
    public static DiagramTextBlockModel OfShape(DiagramShapeModel shape)
    {
        int fontSize = shape.Style.FontSize;

        DiagramTextAlignment alignment = shape.Style.TextAlignment;

        if (shape.Kind != DiagramShapeKind.Image)
        {
            return Layout(shape.Text, DiagramFigures.TextArea(shape), fontSize, alignment, shape.TextSpans);
        }

        // A name under an icon is often wider than the icon
        Rectangle bounds = shape.Bounds;
        DiagramRichTextModel normalized = DiagramRichText.Normalized(shape.Text, shape.TextSpans, trimEnd: false);
        IReadOnlyList<Range> lines = WrapRanges(normalized.Text, Math.Max(bounds.Width, ImageTextWidth), fontSize);
        float height = lines.Count * fontSize * LineHeightFactor;

        // At the left or right, from the image's own edge
        float x = alignment switch
        {
            DiagramTextAlignment.Left => bounds.Left,
            DiagramTextAlignment.Right => bounds.Right,
            _ => bounds.Left + bounds.Width / 2f,
        };

        return Place(normalized, lines, x, bounds.Bottom + ImageTextGap + height / 2, fontSize, alignment);
    }

    /// <summary>A connector's label, around the point its route gives it.</summary>
    public static DiagramTextBlockModel OfLabel(DiagramConnectorModel connector, PointF center) => LayoutAt(
        connector.Label,
        center,
        connector.Style.FontSize,
        connector.Style.TextAlignment,
        connector.LabelSpans);

    /// <summary>
    ///     The lines of a text around a point, wrapped only at its own line breaks; at the left, the right or the
    ///     middle of the block they make, which is centered on the point.
    /// </summary>
    public static DiagramTextBlockModel LayoutAt(
        string text,
        PointF center,
        int fontSize,
        DiagramTextAlignment alignment = DiagramTextAlignment.Center,
        IReadOnlyList<DiagramTextSpanModel>? spans = null)
    {
        DiagramRichTextModel normalized = DiagramRichText.Normalized(text, spans, trimEnd: false);
        List<Range> lines = [];
        int start = 0;

        foreach (string line in normalized.Text.Split('\n'))
        {
            lines.Add(new Range(start, start + line.Length));
            start += line.Length + 1;
        }

        float half = lines.Max(line => WidthOf(normalized.Text[line], fontSize)) / 2;
        float x = alignment switch
        {
            DiagramTextAlignment.Left => center.X - half,
            DiagramTextAlignment.Right => center.X + half,
            _ => center.X,
        };

        return Place(normalized, lines, x, center.Y, fontSize, alignment);
    }

    /// <summary>
    ///     Breaks at spaces, which are kept as typed except at a break and at the end of a line, where they'd only move
    ///     it off center; a word too long for a whole line is broken too.
    /// </summary>
    public static IReadOnlyList<string> Wrap(string text, float width, int fontSize)
    {
        string normalized = text.ReplaceLineEndings("\n");

        return [.. WrapRanges(normalized, width, fontSize).Select(line => normalized[line])];
    }

    /// <summary>Where each line of <see cref="Wrap"/> is in the text, whose line breaks must be "\n".</summary>
    private static IReadOnlyList<Range> WrapRanges(string text, float width, int fontSize)
    {
        int maxCharacters = Math.Max(1, (int)(width / (fontSize * AverageCharacterWidth)));
        List<Range> lines = [];
        int paragraphStart = 0;

        foreach (string paragraph in text.Split('\n'))
        {
            // The line so far, from its start to its end in the text; a line is always one piece of it
            int lineStart = paragraphStart;
            int lineEnd = paragraphStart;

            foreach (Match match in _spacesThenWord.Matches(paragraph))
            {
                int spacesStart = paragraphStart + match.Index;
                int restStart = paragraphStart + match.Groups[2].Index;
                int restEnd = restStart + match.Groups[2].Length;
                int longerStart = lineEnd > lineStart ? lineStart : spacesStart;

                if (restEnd - longerStart <= maxCharacters)
                {
                    (lineStart, lineEnd) = (longerStart, restEnd);

                    continue;
                }

                if (lineEnd > lineStart)
                {
                    lines.Add(new Range(lineStart, lineEnd));
                }

                while (restEnd - restStart > maxCharacters)
                {
                    int split = SplitPoint(text[restStart..restEnd], maxCharacters);

                    if (split >= restEnd - restStart)
                    {
                        break;
                    }

                    lines.Add(new Range(restStart, restStart + split));
                    restStart += split;
                }

                (lineStart, lineEnd) = (restStart, restEnd);
            }

            lines.Add(new Range(lineStart, lineEnd));
            paragraphStart += paragraph.Length + 1;
        }

        return lines;
    }

    /// <summary>
    ///     Where to break a word, without splitting a surrogate pair such as an emoji, which can't be written to a file.
    ///     One wider than the whole line stays whole.
    /// </summary>
    private static int SplitPoint(string word, int maxCharacters)
    {
        if (!char.IsSurrogatePair(word[maxCharacters - 1], word[maxCharacters]))
        {
            return maxCharacters;
        }

        return maxCharacters > 1 ? maxCharacters - 1 : maxCharacters + 1;
    }

    /// <param name="text">The whole text, with its spans, which the lines are parts of.</param>
    /// <param name="ranges">Where each line is in it.</param>
    private static DiagramTextBlockModel Place(
        DiagramRichTextModel text,
        IReadOnlyList<Range> ranges,
        float x,
        float centerY,
        int fontSize,
        DiagramTextAlignment alignment)
    {
        IReadOnlyList<string> lines = [.. ranges.Select(range => text.Text[range])];
        IReadOnlyList<IReadOnlyList<DiagramTextRunModel>>? runs = null;

        if (text.Spans.Count > 0)
        {
            DiagramTextFormat[] formats = DiagramRichText.FormatsOf(text.Text.Length, text.Spans);

            runs =
            [
                .. ranges.Select(range =>
                {
                    (int start, int length) = range.GetOffsetAndLength(text.Text.Length);

                    return DiagramRichText.RunsOf(text.Text, formats, start, length);
                }),
            ];
        }

        return Place(lines, x, centerY, fontSize, alignment) with { Runs = runs };
    }

    private static DiagramTextBlockModel Place(
        IReadOnlyList<string> lines,
        float x,
        float centerY,
        int fontSize,
        DiagramTextAlignment alignment)
    {
        float lineHeight = fontSize * LineHeightFactor;
        float height = lines.Count * lineHeight;
        float width = lines.Max(line => WidthOf(line, fontSize));
        float top = centerY - height / 2;

        float left = alignment switch
        {
            DiagramTextAlignment.Left => x,
            DiagramTextAlignment.Right => x - width,
            _ => x - width / 2,
        };

        return new DiagramTextBlockModel(
            lines,
            x,
            top,
            lineHeight,
            new RectangleF(left, top, width, height),
            alignment);
    }
}
