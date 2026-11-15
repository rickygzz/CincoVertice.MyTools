using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Services;

/// <summary>
///     The parts of a text in bold or italic, kept as spans over the plain text, so finding, wrapping and everything
///     else that reads the text needn't know about them. Each change works on a format per character, then makes
///     spans of it again.
/// </summary>
public static class DiagramRichText
{
    private const DiagramTextFormat AllFormats = DiagramTextFormat.Bold | DiagramTextFormat.Italic;

    // The line breaks string.ReplaceLineEndings knows besides "\n", as numbers
    private const char Newline = (char)0x0A;
    private const char CarriageReturn = (char)0x0D;
    private const char FormFeed = (char)0x0C;
    private const char NextLine = (char)0x85;
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;

    /// <summary>What each character is written as; spans out of the text are cut to it, and later ones win.</summary>
    public static DiagramTextFormat[] FormatsOf(int length, IReadOnlyList<DiagramTextSpanModel>? spans)
    {
        var formats = new DiagramTextFormat[length];

        foreach (DiagramTextSpanModel span in spans ?? [])
        {
            int start = Math.Clamp(span.Start, 0, length);
            // In long, since a file's numbers can be anything
            int end = (int)Math.Clamp((long)span.Start + Math.Max(span.Length, 0), start, length);

            Array.Fill(formats, span.Format & AllFormats, start, end - start);
        }

        return formats;
    }

    /// <summary>The spans of the characters that are bold or italic, each as long as it can be.</summary>
    public static IReadOnlyList<DiagramTextSpanModel> SpansOf(IReadOnlyList<DiagramTextFormat> formats)
    {
        List<DiagramTextSpanModel> spans = [];
        int start = 0;

        for (int i = 1; i <= formats.Count; i++)
        {
            if (i < formats.Count && formats[i] == formats[start])
            {
                continue;
            }

            if (formats[start] != DiagramTextFormat.None)
            {
                spans.Add(new DiagramTextSpanModel(start, i - start, formats[start]));
            }

            start = i;
        }

        return spans;
    }

    /// <summary>Null for none, as a shape or a connector keeps them, so a plain text stays as it always was.</summary>
    public static IReadOnlyList<DiagramTextSpanModel>? OrNull(IReadOnlyList<DiagramTextSpanModel> spans) =>
        spans.Count > 0 ? spans : null;

    /// <summary>The spans, cut to the text and merged, as a file or a view may give them in any way.</summary>
    public static IReadOnlyList<DiagramTextSpanModel> Valid(string text, IReadOnlyList<DiagramTextSpanModel>? spans) =>
        SpansOf(FormatsOf(text.Length, spans));

    /// <summary>
    ///     The text with each line break as "\n", as it's kept, and the spans moved along with its characters.
    /// </summary>
    /// <param name="text">As typed.</param>
    /// <param name="spans">Over the text as typed.</param>
    /// <param name="trimEnd">True to leave out the spaces and empty lines at the end too.</param>
    public static DiagramRichTextModel Normalized(
        string text,
        IReadOnlyList<DiagramTextSpanModel>? spans,
        bool trimEnd = true)
    {
        DiagramTextFormat[] formats = FormatsOf(text.Length, spans);
        var characters = new System.Text.StringBuilder(text.Length);
        List<DiagramTextFormat> kept = new(text.Length);

        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];

            // The line breaks string.ReplaceLineEndings knows; a "\r\n" is one
            if (character == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
            {
                continue;
            }

            characters.Append(IsLineBreak(character) ? Newline : character);
            kept.Add(formats[i]);
        }

        int length = characters.Length;

        while (trimEnd && length > 0 && char.IsWhiteSpace(characters[length - 1]))
        {
            length--;
        }

        return new DiagramRichTextModel(characters.ToString(0, length), SpansOf(kept[..length]));
    }

    private static bool IsLineBreak(char character) =>
        character is CarriageReturn or FormFeed or NextLine or LineSeparator or ParagraphSeparator;

    /// <summary>
    ///     The spans after the text was changed as a whole, e.g. in the properties panel: what's kept at its start and
    ///     its end keeps its format, and what's typed between takes the format of the character before it.
    /// </summary>
    public static IReadOnlyList<DiagramTextSpanModel> AfterEdit(
        string oldText,
        IReadOnlyList<DiagramTextSpanModel>? spans,
        string newText)
    {
        if (spans is not { Count: > 0 })
        {
            return [];
        }

        DiagramTextFormat[] old = FormatsOf(oldText.Length, spans);
        int shorter = Math.Min(oldText.Length, newText.Length);
        int prefix = 0;

        while (prefix < shorter && oldText[prefix] == newText[prefix])
        {
            prefix++;
        }

        int suffix = 0;

        while (suffix < shorter - prefix && oldText[^(suffix + 1)] == newText[^(suffix + 1)])
        {
            suffix++;
        }

        DiagramTextFormat typed = prefix > 0 ? old[prefix - 1]
            : old.Length > 0 ? old[0]
            : DiagramTextFormat.None;
        var formats = new DiagramTextFormat[newText.Length];

        Array.Copy(old, formats, prefix);
        Array.Fill(formats, typed, prefix, newText.Length - prefix - suffix);
        Array.Copy(old, oldText.Length - suffix, formats, newText.Length - suffix, suffix);

        return SpansOf(formats);
    }

    /// <summary>The whole text in the format, or all of it out of it, keeping its other formats.</summary>
    public static IReadOnlyList<DiagramTextSpanModel> WithFormat(
        string text,
        IReadOnlyList<DiagramTextSpanModel>? spans,
        DiagramTextFormat format,
        bool isOn)
    {
        DiagramTextFormat[] formats = FormatsOf(text.Length, spans);

        for (int i = 0; i < formats.Length; i++)
        {
            formats[i] = isOn ? formats[i] | format : formats[i] & ~format;
        }

        return SpansOf(formats);
    }

    /// <summary>Whether every letter of the text, spaces aside, has the format; false for an empty text.</summary>
    public static bool HasFormat(string text, IReadOnlyList<DiagramTextSpanModel>? spans, DiagramTextFormat format)
    {
        DiagramTextFormat[] formats = FormatsOf(text.Length, spans);
        bool hasLetters = false;

        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                continue;
            }

            hasLetters = true;

            if ((formats[i] & format) != format)
            {
                return false;
            }
        }

        return hasLetters;
    }

    /// <summary>A part of the text in pieces written the same way, in order.</summary>
    public static IReadOnlyList<DiagramTextRunModel> RunsOf(
        string text,
        IReadOnlyList<DiagramTextFormat> formats,
        int start,
        int length)
    {
        List<DiagramTextRunModel> runs = [];
        int runStart = start;

        for (int i = start + 1; i <= start + length; i++)
        {
            if (i < start + length && formats[i] == formats[runStart])
            {
                continue;
            }

            runs.Add(new DiagramTextRunModel(text[runStart..i], formats[runStart]));
            runStart = i;
        }

        return runs;
    }
}
