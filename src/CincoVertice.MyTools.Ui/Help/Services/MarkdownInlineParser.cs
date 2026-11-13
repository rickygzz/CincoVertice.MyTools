using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CincoVertice.MyTools.Ui.Help.Models;

namespace CincoVertice.MyTools.Ui.Help.Services;

/// <summary>
///     The text of a Markdown block, as pieces of one style each: **bold**, *italic*, `code`, [links](page.md), web
///     addresses, &lt;br&gt; and line breaks. Anything it doesn't know stays as written.
/// </summary>
public static partial class MarkdownInlineParser
{
    public static IReadOnlyList<MarkdownInlineModel> Parse(string text)
    {
        List<MarkdownInlineModel> pieces = [];
        Parse(text, MarkdownInlineStyle.None, null, pieces);

        return Merge(pieces);
    }

    private static void Parse(string text, MarkdownInlineStyle style, string? link, List<MarkdownInlineModel> pieces)
    {
        var plain = new StringBuilder();

        void Flush()
        {
            if (plain.Length > 0)
            {
                pieces.Add(new MarkdownInlineModel(plain.ToString(), style, link));
                plain.Clear();
            }
        }

        int i = 0;

        while (i < text.Length)
        {
            char character = text[i];

            if (character == '\\' && i + 1 < text.Length && IsEscapable(text[i + 1]))
            {
                plain.Append(text[i + 1]);
                i += 2;

                continue;
            }

            if (character == '`')
            {
                int run = RunLength(text, i);
                int close = FindCodeEnd(text, i + run, run);

                if (close < 0)
                {
                    plain.Append('`', run);
                    i += run;

                    continue;
                }

                Flush();
                pieces.Add(new MarkdownInlineModel(
                    CodeText(text[(i + run)..close]),
                    style | MarkdownInlineStyle.Code,
                    link));
                i = close + run;

                continue;
            }

            if (character == '<' && TryReadTag(text, i, link, out string? tagText, out string? url, out int tagLength))
            {
                if (url is null)
                {
                    plain.Append(tagText);
                }
                else
                {
                    Flush();
                    pieces.Add(new MarkdownInlineModel(url, style, url));
                }

                i += tagLength;

                continue;
            }

            bool isImage = character == '!' && i + 1 < text.Length && text[i + 1] == '[';

            if (link is null && (character == '[' || isImage)
                && TryReadLink(text, isImage ? i + 1 : i, out string label, out string target, out int end))
            {
                Flush();

                // An image can't be shown: its description is
                if (isImage)
                {
                    Parse(label, style | MarkdownInlineStyle.Italic, null, pieces);
                }
                else
                {
                    Parse(label, style, target, pieces);
                }

                i = end;

                continue;
            }

            if (link is null && character == 'h' && (i == 0 || !char.IsLetterOrDigit(text[i - 1])))
            {
                Match address = BareLink().Match(text, i);

                if (address.Success)
                {
                    Flush();
                    string bare = address.Value.TrimEnd('.', ',', ';', ':', '!', '?');
                    pieces.Add(new MarkdownInlineModel(bare, style, bare));
                    i += bare.Length;

                    continue;
                }
            }

            if (character is '*' or '_')
            {
                int run = RunLength(text, i);
                int length = run >= 2 ? 2 : 1;
                int close = CanOpen(text, i, run) ? FindEmphasisEnd(text, i + length, character, length) : -1;

                if (close < 0)
                {
                    plain.Append(character, run);
                    i += run;

                    continue;
                }

                Flush();
                MarkdownInlineStyle emphasis = length == 2 ? MarkdownInlineStyle.Bold : MarkdownInlineStyle.Italic;
                Parse(text[(i + length)..close], style | emphasis, link, pieces);
                i = close + length;

                continue;
            }

            if (character == '&')
            {
                Match entity = Entity().Match(text, i);

                if (entity.Success)
                {
                    plain.Append(WebUtility.HtmlDecode(entity.Value));
                    i += entity.Length;

                    continue;
                }
            }

            plain.Append(character);
            i++;
        }

        Flush();
    }

    private static bool IsEscapable(char character) => char.IsPunctuation(character) || char.IsSymbol(character);

    // A code span's text: "`` `x` ``" has one space on each side only to keep the backticks apart
    private static string CodeText(string code)
    {
        code = code.Replace('\n', ' ');

        return code.Length >= 2 && code[0] == ' ' && code[^1] == ' ' && code.Trim().Length > 0 ? code[1..^1] : code;
    }

    // <br> is a line break; <https://...> a link; any other tag stays as written
    private static bool TryReadTag(
        string text,
        int start,
        string? link,
        out string? tagText,
        out string? url,
        out int length)
    {
        tagText = url = null;
        length = 0;
        Match tag = BreakTag().Match(text, start);

        if (tag.Success)
        {
            tagText = "\n";
            length = tag.Length;

            return true;
        }

        Match address = AngleLink().Match(text, start);

        if (address.Success && link is null)
        {
            url = address.Groups["url"].Value;
            length = address.Length;

            return true;
        }

        return false;
    }

    // How many of the same character there are from start
    private static int RunLength(string text, int start)
    {
        int end = start;

        while (end < text.Length && text[end] == text[start])
        {
            end++;
        }

        return end - start;
    }

    // Where the backticks that end a code span begin: as many as it started with
    private static int FindCodeEnd(string text, int start, int run)
    {
        for (int i = start; i < text.Length; i++)
        {
            if (text[i] != '`')
            {
                continue;
            }

            int length = RunLength(text, i);

            if (length == run)
            {
                return i;
            }

            i += length - 1;
        }

        return -1;
    }

    // Past a code span that starts at start; just past its backticks when it doesn't end
    private static int SkipCode(string text, int start)
    {
        int run = RunLength(text, start);
        int close = FindCodeEnd(text, start + run, run);

        return close < 0 ? start + run : close + run;
    }

    // * opens before a word; _ only at the start of one, so snake_case stays as written
    private static bool CanOpen(string text, int start, int run)
    {
        int after = start + run;

        if (after >= text.Length || char.IsWhiteSpace(text[after]))
        {
            return false;
        }

        return text[start] == '*' || start == 0 || !char.IsLetterOrDigit(text[start - 1]);
    }

    // Where the closing marks begin: after a word, never inside a code span
    private static int FindEmphasisEnd(string text, int start, char mark, int length)
    {
        (int run, int marks) = FindClosingRun(text, start, mark, length);

        // "***both***": the last two close the bold, the one before them the italic inside it
        return run < 0 ? -1 : run + marks - length;
    }

    /// <returns>
    ///     The run of marks that closes emphasis opened just before start: where it starts and how many marks it
    ///     has; (-1, 0) when there's none. Emphasis that opens inside is skipped to its own end, so in
    ///     "*a **b** c*" the italic doesn't end at the bold's closing marks.
    /// </returns>
    private static (int Start, int Marks) FindClosingRun(string text, int start, char mark, int length)
    {
        int i = start;

        while (i < text.Length)
        {
            char character = text[i];

            if (character == '\\')
            {
                i += 2;

                continue;
            }

            if (character == '`')
            {
                i = SkipCode(text, i);

                continue;
            }

            if (character != mark)
            {
                i++;

                continue;
            }

            int marks = RunLength(text, i);
            int after = i + marks;
            bool isAfterWord = i > start && !char.IsWhiteSpace(text[i - 1]);
            bool isWordEnd = mark == '*' || after >= text.Length || !char.IsLetterOrDigit(text[after]);

            if (marks >= length && isAfterWord && isWordEnd)
            {
                return (i, marks);
            }

            // Emphasis of its own: past the marks that close it, the first of their run; the rest can close this one
            if (!isAfterWord && CanOpen(text, i, marks))
            {
                (int inner, _) = FindClosingRun(text, after, mark, marks);

                if (inner >= 0)
                {
                    i = inner + marks;

                    continue;
                }
            }

            i = after;
        }

        return (-1, 0);
    }

    // "[label](target)" from the "[" at start, brackets inside it included
    private static bool TryReadLink(string text, int start, out string label, out string target, out int end)
    {
        label = target = string.Empty;
        end = start;
        int close = FindClosing(text, start, '[', ']');

        if (close < 0 || close + 1 >= text.Length || text[close + 1] != '(')
        {
            return false;
        }

        int targetEnd = FindClosing(text, close + 1, '(', ')');

        if (targetEnd < 0)
        {
            return false;
        }

        label = text[(start + 1)..close];
        target = text[(close + 2)..targetEnd].Trim();

        // A title after the address, as in (page.md "Title"), isn't part of it
        int space = target.IndexOf(' ', StringComparison.Ordinal);

        if (space > 0)
        {
            target = target[..space];
        }

        target = target.TrimStart('<').TrimEnd('>');
        end = targetEnd + 1;

        return target.Length > 0;
    }

    private static int FindClosing(string text, int start, char open, char close)
    {
        int depth = 0;
        int i = start;

        while (i < text.Length)
        {
            char character = text[i];

            if (character == '\\')
            {
                i += 2;

                continue;
            }

            if (character == '`' && open == '[')
            {
                i = SkipCode(text, i);

                continue;
            }

            if (character == open)
            {
                depth++;
            }
            else if (character == close && --depth == 0)
            {
                return i;
            }

            i++;
        }

        return -1;
    }

    // Pieces of the same style and link next to each other, as one
    private static List<MarkdownInlineModel> Merge(List<MarkdownInlineModel> pieces)
    {
        List<MarkdownInlineModel> merged = [];

        foreach (MarkdownInlineModel piece in pieces)
        {
            if (merged.Count > 0 && merged[^1].Style == piece.Style && merged[^1].Link == piece.Link)
            {
                merged[^1] = merged[^1] with { Text = merged[^1].Text + piece.Text };
            }
            else
            {
                merged.Add(piece);
            }
        }

        return merged;
    }

    [GeneratedRegex(@"\G<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BreakTag();

    [GeneratedRegex(@"\G<(?<url>(https?://|mailto:)[^>\s]+)>", RegexOptions.IgnoreCase)]
    private static partial Regex AngleLink();

    [GeneratedRegex(@"\Ghttps?://[^\s<>()\[\]`]+", RegexOptions.IgnoreCase)]
    private static partial Regex BareLink();

    [GeneratedRegex(@"\G&(amp|lt|gt|quot|apos|nbsp|#[0-9]{1,6}|#x[0-9a-fA-F]{1,6});")]
    private static partial Regex Entity();
}
