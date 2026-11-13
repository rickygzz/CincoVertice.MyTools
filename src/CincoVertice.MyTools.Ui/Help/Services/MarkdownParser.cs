using System.Text;
using System.Text.RegularExpressions;
using CincoVertice.MyTools.Ui.Help.Models;

namespace CincoVertice.MyTools.Ui.Help.Services;

/// <summary>
///     Reads a Markdown page as GitHub shows it, as far as the user guide needs: headings, paragraphs, lists inside
///     lists, fenced and indented code, tables, quotes and rules. A line it doesn't know is text of a paragraph.
/// </summary>
public sealed partial class MarkdownParser
{
    private static readonly string[] _bullets = ["•", "◦", "▪"];

    private readonly string[] _lines;
    private readonly List<MarkdownBlockModel> _blocks = [];

    // How many headings had each anchor, so a second "Usage" is "usage-1", as on GitHub
    private readonly Dictionary<string, int> _anchors = [];

    // The list items the lines are in, the deepest last: where their marker is, and where their text starts
    private readonly List<(int Marker, int Text)> _openItems = [];

    // The paragraph or list item being read: its lines so far
    private readonly List<string> _pendingLines = [];

    private int _index;
    private bool _isAfterBlank = true;
    private MarkdownBlockKind _pendingKind = MarkdownBlockKind.Paragraph;
    private int _pendingLevel;
    private string? _pendingMarker;

    private MarkdownParser(string markdown)
    {
        _lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
    }

    public static MarkdownDocumentModel Parse(string markdown) => new MarkdownParser(markdown).Read();

    private MarkdownDocumentModel Read()
    {
        while (_index < _lines.Length)
        {
            string line = LineAt(_index);

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushPending();
                _isAfterBlank = true;
                _index++;

                continue;
            }

            bool isAfterBlank = _isAfterBlank;
            _isAfterBlank = false;
            ReadLine(line, isAfterBlank);
        }

        FlushPending();

        return new MarkdownDocumentModel(_blocks);
    }

    private void ReadLine(string line, bool isAfterBlank)
    {
        int indent = IndentOf(line);
        string text = line[indent..];

        // A paragraph's underline makes it a heading
        if (_pendingLines.Count > 0 && _pendingKind == MarkdownBlockKind.Paragraph && SetextLine().IsMatch(line))
        {
            string heading = JoinLines(_pendingLines);
            _pendingLines.Clear();
            AddHeading(text[0] == '=' ? 1 : 2, heading);
            _index++;

            return;
        }

        if (RuleLine().IsMatch(line))
        {
            FlushPending();
            _openItems.Clear();
            _blocks.Add(new MarkdownBlockModel(MarkdownBlockKind.Rule, []));
            _index++;

            return;
        }

        if (TryReadListItem(line, indent))
        {
            return;
        }

        // Right after a paragraph's line, a line that starts nothing else goes on with it, however it's indented
        if (_pendingLines.Count > 0 && !isAfterBlank && !StartsBlock(text))
        {
            _pendingLines.Add(text);
            _index++;

            return;
        }

        FlushPending();
        int level = DepthOf(indent);

        if (FenceLine().Match(text) is { Success: true } fence)
        {
            ReadFence(fence, indent, level);
        }
        else if (HeadingLine().Match(line) is { Success: true } heading)
        {
            _openItems.Clear();
            AddHeading(heading.Groups["marks"].Length, heading.Groups["text"].Value);
            _index++;
        }
        else if (IsTableStart(text))
        {
            ReadTable(level);
        }
        else if (QuoteLine().IsMatch(line))
        {
            ReadQuote(level);
        }
        else if (isAfterBlank && indent - TextIndentOf(level) >= 4)
        {
            ReadIndentedCode(TextIndentOf(level) + 4, level);
        }
        else
        {
            StartPending(MarkdownBlockKind.Paragraph, level, null, text);
        }
    }

    private bool TryReadListItem(string line, int indent)
    {
        Match item = ListItemLine().Match(line);

        if (!item.Success)
        {
            return false;
        }

        FlushPending();

        // Items whose marker is as far in or further are done: this one is next to them, or out of them
        while (_openItems.Count > 0 && _openItems[^1].Marker >= indent)
        {
            _openItems.RemoveAt(_openItems.Count - 1);
        }

        string marker = item.Groups["marker"].Value;
        int spaces = item.Groups["space"].Length;
        int level = _openItems.Count;
        _openItems.Add((indent, indent + marker.Length + (spaces is >= 1 and <= 4 ? spaces : 1)));

        string shown = char.IsDigit(marker[0]) ? marker[..^1] + "." : _bullets[level % _bullets.Length];
        StartPending(MarkdownBlockKind.ListItem, level, shown, item.Groups["text"].Value);

        return true;
    }

    private void ReadFence(Match fence, int indent, int level)
    {
        string marks = fence.Groups["fence"].Value;
        string language = fence.Groups["info"].Value.Trim().Split(' ')[0];
        List<string> code = [];
        _index++;

        while (_index < _lines.Length)
        {
            string line = LineAt(_index++);
            string text = line.TrimStart();

            if (text.StartsWith(marks, StringComparison.Ordinal) && text.TrimEnd().Trim(marks[0]).Length == 0)
            {
                break;
            }

            code.Add(WithoutIndent(line, indent));
        }

        AddCode(code, level, language);
    }

    private void ReadIndentedCode(int indent, int level)
    {
        List<string> code = [];

        while (_index < _lines.Length)
        {
            string line = LineAt(_index);

            if (!string.IsNullOrWhiteSpace(line) && IndentOf(line) < indent)
            {
                break;
            }

            code.Add(WithoutIndent(line, indent));
            _index++;
        }

        // The blank lines after it are between it and what's next
        while (code.Count > 0 && string.IsNullOrWhiteSpace(code[^1]))
        {
            code.RemoveAt(code.Count - 1);
        }

        _isAfterBlank = true;
        AddCode(code, level, string.Empty);
    }

    private void AddCode(List<string> code, int level, string language)
    {
        _blocks.Add(new MarkdownBlockModel(
            MarkdownBlockKind.CodeBlock,
            [new MarkdownInlineModel(string.Join("\n", code), MarkdownInlineStyle.Code)],
            level,
            Language: language));
    }

    private void ReadTable(int level)
    {
        List<IReadOnlyList<IReadOnlyList<MarkdownInlineModel>>> rows = [CellsOf(LineAt(_index))];
        int columns = rows[0].Count;

        // Past the header and the line under it
        _index += 2;

        while (_index < _lines.Length && LineAt(_index).TrimStart().StartsWith('|'))
        {
            rows.Add(CellsOf(LineAt(_index++)));
        }

        // As many cells in every row as in the header
        for (int i = 1; i < rows.Count; i++)
        {
            IReadOnlyList<IReadOnlyList<MarkdownInlineModel>> cells = rows[i];
            rows[i] = [.. cells.Take(columns), .. Enumerable.Repeat<IReadOnlyList<MarkdownInlineModel>>(
                [], Math.Max(0, columns - cells.Count))];
        }

        _blocks.Add(new MarkdownBlockModel(MarkdownBlockKind.Table, [], level, Rows: rows));
    }

    private void ReadQuote(int level)
    {
        List<string> lines = [];

        while (_index < _lines.Length && QuoteLine().Match(LineAt(_index)) is { Success: true } quote)
        {
            lines.Add(quote.Groups["text"].Value.Trim());
            _index++;
        }

        _blocks.Add(new MarkdownBlockModel(
            MarkdownBlockKind.Quote,
            MarkdownInlineParser.Parse(JoinLines(lines)),
            level));
    }

    private void AddHeading(int level, string text)
    {
        IReadOnlyList<MarkdownInlineModel> inlines = MarkdownInlineParser.Parse(text.Trim());
        string anchor = MarkdownAnchor.Of(string.Concat(inlines.Select(inline => inline.Text)));

        if (_anchors.TryGetValue(anchor, out int count))
        {
            _anchors[anchor] = count + 1;
            anchor = $"{anchor}-{count}";
        }
        else
        {
            _anchors[anchor] = 1;
        }

        _blocks.Add(new MarkdownBlockModel(MarkdownBlockKind.Heading, inlines, level, Anchor: anchor));
    }

    private void StartPending(MarkdownBlockKind kind, int level, string? marker, string text)
    {
        _pendingKind = kind;
        _pendingLevel = level;
        _pendingMarker = marker;
        _pendingLines.Add(text);
        _index++;
    }

    private void FlushPending()
    {
        if (_pendingLines.Count == 0)
        {
            return;
        }

        _blocks.Add(new MarkdownBlockModel(
            _pendingKind,
            MarkdownInlineParser.Parse(JoinLines(_pendingLines)),
            _pendingLevel,
            _pendingMarker));
        _pendingLines.Clear();
    }

    /// <returns>
    ///     How many of the open list items an indented line is in: those whose text starts as far in or less. The
    ///     others are done.
    /// </returns>
    private int DepthOf(int indent)
    {
        int depth = 0;

        while (depth < _openItems.Count && _openItems[depth].Text <= indent)
        {
            depth++;
        }

        _openItems.RemoveRange(depth, _openItems.Count - depth);

        return depth;
    }

    // Where the text of the list items a line is in starts
    private int TextIndentOf(int level) => level == 0 ? 0 : _openItems[level - 1].Text;

    private bool StartsBlock(string text) =>
        FenceLine().IsMatch(text) || HeadingLine().IsMatch(text) || QuoteLine().IsMatch(text) || IsTableStart(text);

    // A table is a row of cells with the line of dashes under it
    private bool IsTableStart(string text) =>
        text.StartsWith('|') && _index + 1 < _lines.Length && TableSeparatorLine().IsMatch(LineAt(_index + 1));

    private string LineAt(int index) => _lines[index].Replace("\t", "    ", StringComparison.Ordinal);

    private static int IndentOf(string line)
    {
        int indent = 0;

        while (indent < line.Length && line[indent] == ' ')
        {
            indent++;
        }

        return indent;
    }

    private static string WithoutIndent(string line, int indent) => line[Math.Min(indent, IndentOf(line))..];

    /// <summary>
    ///     A paragraph's lines as one text: a line that ends in two spaces or a backslash breaks there, the others go
    ///     on in the same line, as they're shown; an empty line is a break.
    /// </summary>
    private static string JoinLines(IReadOnlyList<string> lines)
    {
        var text = new StringBuilder();

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            bool isBreak = line.EndsWith("  ", StringComparison.Ordinal) || line.EndsWith('\\') || line.Length == 0;
            text.Append(line.TrimEnd().TrimEnd('\\'));

            if (i < lines.Count - 1)
            {
                text.Append(isBreak ? '\n' : ' ');
            }
        }

        return text.ToString().Trim();
    }

    // "| a | `b \| c` |": the cells between the bars, but not an escaped one, nor one inside code
    private static IReadOnlyList<IReadOnlyList<MarkdownInlineModel>> CellsOf(string row)
    {
        string text = row.Trim();
        List<string> cells = [];
        var cell = new StringBuilder();
        bool isInCode = false;

        for (int i = text.StartsWith('|') ? 1 : 0; i < text.Length; i++)
        {
            char character = text[i];

            if (character == '\\' && i + 1 < text.Length && text[i + 1] == '|')
            {
                cell.Append('|');
                i++;
            }
            else if (character == '|' && !isInCode)
            {
                cells.Add(cell.ToString());
                cell.Clear();
            }
            else
            {
                isInCode ^= character == '`';
                cell.Append(character);
            }
        }

        // What's after the last bar is a cell only when there's something in it
        if (cell.ToString().Trim().Length > 0)
        {
            cells.Add(cell.ToString());
        }

        return [.. cells.Select(cellText => MarkdownInlineParser.Parse(cellText.Trim()))];
    }

    [GeneratedRegex(@"^ {0,3}(?<marks>#{1,6})(?:[ \t]+(?<text>.*?))?(?:[ \t]+#+)?[ \t]*$")]
    private static partial Regex HeadingLine();

    [GeneratedRegex(@"^ {0,3}(?:(?:\*[ \t]*){3,}|(?:-[ \t]*){3,}|(?:_[ \t]*){3,})$")]
    private static partial Regex RuleLine();

    [GeneratedRegex(@"^ {0,3}(?:=+|-+)[ \t]*$")]
    private static partial Regex SetextLine();

    [GeneratedRegex(@"^ *(?<marker>[-*+]|[0-9]{1,9}[.)])(?:(?<space> +)(?<text>.*))?$")]
    private static partial Regex ListItemLine();

    [GeneratedRegex(@"^(?<fence>`{3,}|~{3,})[ \t]*(?<info>[^`]*)$")]
    private static partial Regex FenceLine();

    [GeneratedRegex(@"^ *> ?(?<text>.*)$")]
    private static partial Regex QuoteLine();

    [GeneratedRegex(@"^ *\|? *:?-+:? *(\| *:?-+:? *)*\|? *$")]
    private static partial Regex TableSeparatorLine();
}
