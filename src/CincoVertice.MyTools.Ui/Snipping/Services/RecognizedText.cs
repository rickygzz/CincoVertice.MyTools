using System.Text;
using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Snipping.Services;

/// <summary>
///     Makes the lines read from a snip the text to copy, and what the user is told about it, the same from every place
///     that reads it.
/// </summary>
public static class RecognizedText
{
    public const string NoneFoundMessage = "No text found in the snip.";

    // What the OCR makes of the dots some editors draw for spaces, such as Visual Studio's View White Space
    private static readonly char[] _dots = ['•', '·'];

    // What never has a space before it
    private static readonly char[] _closingPunctuation = ['.', ',', ';', ':', '!', '?'];

    /// <returns>
    ///     The lines without the dots read from spaces drawn as dots, without a space before punctuation, and indented as
    ///     far as they are in the image, in spaces as wide as its letters; the line furthest left isn't. Lines with nothing
    ///     left are left out.
    /// </returns>
    public static IReadOnlyList<string> Format(IReadOnlyList<RecognizedLineModel> lines)
    {
        List<IReadOnlyList<RecognizedWordModel>> cleaned =
            [.. lines.Select(line => Clean(line.Words)).Where(words => words.Count > 0)];

        if (cleaned.Count == 0)
        {
            return [];
        }

        double letterWidth = LetterWidth(cleaned);
        double left = cleaned.Min(words => words[0].Left);

        return
        [
            .. cleaned.Select(words =>
                new string(' ', Indent(words[0].Left - left, letterWidth))
                + JoinWords(words)),
        ];
    }

    /// <returns>
    ///     The words without dots and apostrophes read from spaces drawn as dots: on their own, or stuck to the start of a
    ///     word. A dot or apostrophe first on a line, before a word, is kept, as a bullet or a comment, and so is anything
    ///     stuck to the first word.
    /// </returns>
    public static IReadOnlyList<RecognizedWordModel> Clean(IReadOnlyList<RecognizedWordModel> words)
    {
        List<RecognizedWordModel> kept = [];

        for (int i = 0; i < words.Count; i++)
        {
            string text = words[i].Text;

            if (IsSpaceDots(text))
            {
                // A bullet, or the apostrophe of a Visual Basic comment
                bool isFirstMark = i == 0
                    && text.Length == 1
                    && i + 1 < words.Count
                    && !IsSpaceDots(words[i + 1].Text);

                if (isFirstMark)
                {
                    kept.Add(words[i]);
                }

                continue;
            }

            if (i > 0)
            {
                text = text.Trim(_dots);

                // A quote, as in 'quoted', has another one
                if (text.Length > 1 && text[0] == '\'' && char.IsLetter(text[1]) && text.IndexOf('\'', 1) < 0)
                {
                    text = text[1..];
                }
            }

            kept.Add(words[i] with { Text = text });
        }

        return kept;
    }

    /// <returns>The lines as one text, to copy.</returns>
    public static string Join(IReadOnlyList<string> lines) => string.Join(Environment.NewLine, lines);

    public static string CopiedMessage(int lineCount) =>
        lineCount == 1 ? "Copied 1 line of text." : $"Copied {lineCount} lines of text.";

    // One space apart, but punctuation the OCR reads as a word of its own goes against the word before it, as in "held."
    // or "Run();"
    private static string JoinWords(IReadOnlyList<RecognizedWordModel> words)
    {
        var text = new StringBuilder(words[0].Text);

        foreach (RecognizedWordModel word in words.Skip(1))
        {
            if (!word.Text.All(_closingPunctuation.Contains))
            {
                text.Append(' ');
            }

            text.Append(word.Text);
        }

        return text.ToString();
    }

    private static bool IsSpaceDots(string text) => text.All(c => _dots.Contains(c) || c == '\'');

    // How wide a letter is, for the width of a space: the usual one, from words of two letters or more, since one
    // letter alone can be as narrow as an i
    private static double LetterWidth(IEnumerable<IReadOnlyList<RecognizedWordModel>> lines)
    {
        List<RecognizedWordModel> words = [.. lines.SelectMany(line => line)];
        List<RecognizedWordModel> longer = [.. words.Where(word => word.Text.Length > 1)];
        List<double> widths = [.. (longer.Count > 0 ? longer : words).Select(word => word.Width / word.Text.Length).Order()];

        return widths[widths.Count / 2];
    }

    private static int Indent(double offset, double letterWidth) =>
        letterWidth > 0 ? (int)Math.Round(offset / letterWidth) : 0;
}
