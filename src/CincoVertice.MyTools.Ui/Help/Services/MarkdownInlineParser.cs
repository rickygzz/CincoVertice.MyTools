using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using CincoVertice.MyTools.Ui.Help.Models;

namespace CincoVertice.MyTools.Ui.Help.Services;

/// <summary>
///     The text of a Markdown block, as pieces of one style each: **bold**, *italic*, `code`, [links](page.md),
///     [links][by-name] to a page's link list, web addresses, &lt;br&gt; and line breaks. Anything it doesn't know
///     stays as written.
/// </summary>
public sealed partial class MarkdownInlineParser
{
    private static readonly Dictionary<string, string> _noReferences = [];

    private readonly IReadOnlyDictionary<string, string> _references;
    private readonly List<MarkdownInlineModel> _pieces = [];

    private MarkdownInlineParser(IReadOnlyDictionary<string, string> references)
    {
        _references = references;
    }

    /// <param name="references">
    ///     The page's link list, "[name]: target" lines, by <see cref="ReferenceName"/>; none when null.
    /// </param>
    public static IReadOnlyList<MarkdownInlineModel> Parse(
        string text,
        IReadOnlyDictionary<string, string>? references = null)
    {
        var parser = new MarkdownInlineParser(references ?? _noReferences);
        parser.Read(new InlineLevel(text, MarkdownInlineStyle.None, null));

        return Merge(parser._pieces);
    }

    /// <summary>A link name as the link list keeps it: case and runs of spaces don't count.</summary>
    public static string ReferenceName(string name) =>
        string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    // One level of text, of one style and link: what's inside emphasis or a link is a level of its own
    private void Read(InlineLevel level)
    {
        while (level.Index < level.Text.Length)
        {
            if (!TryEscape(level) && !TryCode(level) && !TryTag(level) && !TryLink(level) && !TryBareLink(level)
                && !TryEmphasis(level) && !TryEntity(level))
            {
                level.Plain.Append(level.Text[level.Index++]);
            }
        }

        Flush(level);
    }

    private void Flush(InlineLevel level)
    {
        if (level.Plain.Length > 0)
        {
            _pieces.Add(new MarkdownInlineModel(level.Plain.ToString(), level.Style, level.Link));
            level.Plain.Clear();
        }
    }

    private void Add(InlineLevel level, string text, MarkdownInlineStyle style, string? link)
    {
        Flush(level);
        _pieces.Add(new MarkdownInlineModel(text, style, link));
    }

    // \* is a *, not emphasis
    private static bool TryEscape(InlineLevel level)
    {
        string text = level.Text;
        int i = level.Index;

        if (text[i] != '\\' || i + 1 >= text.Length || !(char.IsPunctuation(text[i + 1]) || char.IsSymbol(text[i + 1])))
        {
            return false;
        }

        level.Plain.Append(text[i + 1]);
        level.Index += 2;

        return true;
    }

    // `code`; backticks that don't close stay as written
    private bool TryCode(InlineLevel level)
    {
        string text = level.Text;
        int i = level.Index;

        if (text[i] != '`')
        {
            return false;
        }

        int run = RunLength(text, i);
        int close = FindCodeEnd(text, i + run, run);

        if (close < 0)
        {
            level.Plain.Append('`', run);
            level.Index += run;

            return true;
        }

        string code = text[(i + run)..close].Replace('\n', ' ');

        // "`` `x` ``": one space on each side is only there to keep the backticks apart
        if (code.Length >= 2 && code[0] == ' ' && code[^1] == ' ' && code.Trim().Length > 0)
        {
            code = code[1..^1];
        }

        Add(level, code, level.Style | MarkdownInlineStyle.Code, level.Link);
        level.Index = close + run;

        return true;
    }

    // <br> is a line break; <https://...> a link; any other tag stays as written
    private bool TryTag(InlineLevel level)
    {
        if (level.Text[level.Index] != '<')
        {
            return false;
        }

        Match tag = BreakTag().Match(level.Text, level.Index);

        if (tag.Success)
        {
            level.Plain.Append('\n');
            level.Index += tag.Length;

            return true;
        }

        Match address = AngleLink().Match(level.Text, level.Index);

        if (!address.Success || level.Link is not null)
        {
            return false;
        }

        string url = address.Groups["url"].Value;
        Add(level, url, level.Style, url);
        level.Index += address.Length;

        return true;
    }

    // [label](target), [label][name], [label][] or [name], and images, ![description](file), whose description shows
    private bool TryLink(InlineLevel level)
    {
        string text = level.Text;
        int i = level.Index;
        bool isImage = text[i] == '!' && i + 1 < text.Length && text[i + 1] == '[';

        if (level.Link is not null || text[i] != '[' && !isImage
            || !TryReadLink(text, isImage ? i + 1 : i, out string label, out string target, out int end))
        {
            return false;
        }

        Flush(level);

        if (isImage)
        {
            Read(new InlineLevel(label, level.Style | MarkdownInlineStyle.Italic, null));
        }
        else
        {
            Read(new InlineLevel(label, level.Style, target));
        }

        level.Index = end;

        return true;
    }

    // A web address in the text
    private bool TryBareLink(InlineLevel level)
    {
        string text = level.Text;
        int i = level.Index;

        if (level.Link is not null || text[i] != 'h' || i > 0 && char.IsLetterOrDigit(text[i - 1]))
        {
            return false;
        }

        Match address = BareLink().Match(text, i);

        if (!address.Success)
        {
            return false;
        }

        // The punctuation after it ends the sentence
        string url = address.Value.TrimEnd('.', ',', ';', ':', '!', '?');
        Add(level, url, level.Style, url);
        level.Index += url.Length;

        return true;
    }

    // *italic*, **bold**, _italic_, __bold__; marks that don't close stay as written
    private bool TryEmphasis(InlineLevel level)
    {
        string text = level.Text;
        int i = level.Index;
        char mark = text[i];

        if (mark is not ('*' or '_'))
        {
            return false;
        }

        int run = RunLength(text, i);
        int length = run >= 2 ? 2 : 1;
        int close = CanOpen(text, i, run) ? FindEmphasisEnd(text, i + length, mark, length) : -1;

        if (close < 0)
        {
            level.Plain.Append(mark, run);
            level.Index += run;

            return true;
        }

        Flush(level);
        MarkdownInlineStyle emphasis = length == 2 ? MarkdownInlineStyle.Bold : MarkdownInlineStyle.Italic;
        Read(new InlineLevel(text[(i + length)..close], level.Style | emphasis, level.Link));
        level.Index = close + length;

        return true;
    }

    // &amp; and the like
    private static bool TryEntity(InlineLevel level)
    {
        if (level.Text[level.Index] != '&')
        {
            return false;
        }

        Match entity = Entity().Match(level.Text, level.Index);

        if (!entity.Success)
        {
            return false;
        }

        level.Plain.Append(WebUtility.HtmlDecode(entity.Value));
        level.Index += entity.Length;

        return true;
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

    // A link from the "[" at start, brackets inside its label included: to a target, or to a name in the link list
    private bool TryReadLink(string text, int start, out string label, out string target, out int end)
    {
        target = string.Empty;
        end = start;
        int close = FindClosing(text, start, '[', ']');
        label = close < 0 ? string.Empty : text[(start + 1)..close];

        if (close < 0)
        {
            return false;
        }

        if (close + 1 < text.Length && text[close + 1] == '(' && TryReadTarget(text, close + 1, out target, out end))
        {
            return true;
        }

        // [label][name], or [label][] for the label as the name
        if (close + 1 < text.Length && text[close + 1] == '[')
        {
            int nameEnd = text.IndexOf(']', close + 2);

            if (nameEnd > 0)
            {
                string name = text[(close + 2)..nameEnd];
                end = nameEnd + 1;

                return TryFindReference(name.Length == 0 ? label : name, out target);
            }
        }

        // [name] alone
        end = close + 1;

        return TryFindReference(label, out target);
    }

    private bool TryFindReference(string name, out string target) =>
        _references.TryGetValue(ReferenceName(name), out target!);

    // "(target)", or "(target "Title")": a title isn't part of it
    private static bool TryReadTarget(string text, int open, out string target, out int end)
    {
        target = string.Empty;
        end = open;
        int close = FindClosing(text, open, '(', ')');

        if (close < 0)
        {
            return false;
        }

        target = text[(open + 1)..close].Trim();
        int space = target.IndexOf(' ', StringComparison.Ordinal);

        if (space > 0)
        {
            target = target[..space];
        }

        target = target.TrimStart('<').TrimEnd('>');
        end = close + 1;

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

    /// <summary>A level of text being read: its text, its style and link, and how far it's read.</summary>
    private sealed class InlineLevel(string text, MarkdownInlineStyle style, string? link)
    {
        public string Text { get; } = text;

        public MarkdownInlineStyle Style { get; } = style;

        public string? Link { get; } = link;

        public StringBuilder Plain { get; } = new();

        public int Index { get; set; }
    }
}
