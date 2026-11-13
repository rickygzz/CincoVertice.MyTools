using System.Globalization;
using System.Text;
using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Win.Help.Models;

namespace CincoVertice.MyTools.Ui.Win.Help.Services;

/// <summary>
///     A Markdown page as RTF, for a RichTextBox to show: headings, lists, code on a gray box, tables with lines.
///     A RichTextBox can't tell where a link or heading is in its text, so each starts with <see cref="Mark"/>: the
///     reader finds the marks, takes them out and keeps where they were.
/// </summary>
public static class MarkdownRtfWriter
{
    /// <summary>A character no page has: one of Unicode's for private use.</summary>
    public const char Mark = '';

    // Sizes in half points
    private const int TextSize = 20;
    private const int CodeSize = 18;

    // Lengths in twips, 1440 an inch
    private const int LevelIndent = 360;
    private const int MarkerWidth = 300;

    // In the color table: 1 text, 2 links, 3 code's background, 4 quiet text, 5 lines, 6 a table header's background
    private const string ColorTable =
        @"{\colortbl ;\red32\green32\blue32;\red0\green95\blue184;\red243\green243\blue243;\red96\green96\blue96;"
        + @"\red208\green208\blue208;\red234\green238\blue242;}";

    /// <param name="width">How wide the text can be, in twips: tables and code boxes take all of it.</param>
    public static HelpRtfModel Write(MarkdownDocumentModel document, int width)
    {
        var rtf = new StringBuilder(@"{\rtf1\ansi\ansicpg1252\deff0\uc1");
        rtf.Append(@"{\fonttbl{\f0\fswiss\fcharset0 Segoe UI;}{\f1\fmodern\fcharset0 Consolas;}}");
        rtf.Append(ColorTable);
        rtf.Append(CultureInfo.InvariantCulture, $@"\viewkind4\f0\fs{TextSize}\cf1 ");
        List<HelpRtfMarkModel> marks = [];

        for (int i = 0; i < document.Blocks.Count; i++)
        {
            WriteBlock(rtf, marks, document.Blocks[i], i == 0, Math.Max(width, 2000));
        }

        rtf.Append('}');

        return new HelpRtfModel(rtf.ToString(), marks);
    }

    private static void WriteBlock(
        StringBuilder rtf,
        List<HelpRtfMarkModel> marks,
        MarkdownBlockModel block,
        bool isFirst,
        int width)
    {
        int indent = block.Level * LevelIndent;

        switch (block.Kind)
        {
            case MarkdownBlockKind.Heading:
                WriteHeading(rtf, marks, block, isFirst);
                break;

            case MarkdownBlockKind.ListItem:
                int itemIndent = indent + LevelIndent;
                Append(rtf, $@"\pard\li{itemIndent}\fi-{MarkerWidth}\tx{itemIndent}\sa60 ");
                rtf.Append(Escape(block.Marker ?? "•")).Append(@"\tab ");
                WriteInlines(rtf, marks, block.Inlines, TextSize);
                rtf.Append(@"\par").AppendLine();
                break;

            case MarkdownBlockKind.CodeBlock:
                WriteCode(rtf, block, indent, width);
                break;

            case MarkdownBlockKind.Table:
                WriteTable(rtf, marks, block, indent, width);
                break;

            case MarkdownBlockKind.Quote:
                Append(rtf, $@"\pard\li{indent + LevelIndent}\sa120\cf4 ");
                WriteInlines(rtf, marks, block.Inlines, TextSize);
                rtf.Append(@"\cf1\par").AppendLine();
                break;

            case MarkdownBlockKind.Rule:
                // A row with a line under it, and nothing in it: RichEdit doesn't draw a paragraph's borders
                Append(rtf, $@"\trowd\trgaph0\trleft{indent}\clbrdrb\brdrs\brdrw10\brdrcf5\cellx{width}");
                rtf.Append(@"\pard\intbl\sa0\fs4\cell\row").AppendLine();
                Append(rtf, $@"\pard\sa0\fs{TextSize / 2}\par\fs{TextSize}").AppendLine();
                break;

            default:
                Append(rtf, $@"\pard\li{indent}\sa120 ");
                WriteInlines(rtf, marks, block.Inlines, TextSize);
                rtf.Append(@"\par").AppendLine();
                break;
        }
    }

    private static void WriteHeading(
        StringBuilder rtf,
        List<HelpRtfMarkModel> marks,
        MarkdownBlockModel block,
        bool isFirst)
    {
        (int size, int before) = block.Level switch
        {
            1 => (36, 240),
            2 => (28, 360),
            3 => (24, 280),
            _ => (21, 240),
        };

        Append(rtf, $@"\pard\keepn\sb{(isFirst ? 0 : before)}\sa120\b\fs{size} ");
        marks.Add(new HelpRtfMarkModel(null, block.Anchor));
        rtf.Append(Escape(Mark.ToString()));
        WriteInlines(rtf, marks, block.Inlines, size);
        Append(rtf, $@"\b0\fs{TextSize}\par").AppendLine();
    }

    // In a box of one cell, its background gray: RichEdit shades a cell, not a paragraph
    private static void WriteCode(StringBuilder rtf, MarkdownBlockModel block, int indent, int width)
    {
        if (block.Language.Equals("mermaid", StringComparison.OrdinalIgnoreCase))
        {
            Append(rtf, $@"\pard\li{indent}\sa40\cf4\i\fs{CodeSize} A diagram, written as text (Mermaid):");
            Append(rtf, $@"\i0\cf1\fs{TextSize}\par").AppendLine();
        }

        Append(rtf, $@"\trowd\trgaph120\trleft{indent}\trpaddl120\trpaddr120\trpaddfl3\trpaddfr3");
        Append(rtf, $@"\clcbpat3{Borders()}\cellx{width}");
        Append(rtf, $@"\pard\intbl\sb80\sa80\f1\fs{CodeSize} ");
        rtf.Append(Escape(block.PlainText));
        Append(rtf, $@"\f0\fs{TextSize}\cell\row").AppendLine();
        Spacer(rtf);
    }

    private static void WriteTable(
        StringBuilder rtf,
        List<HelpRtfMarkModel> marks,
        MarkdownBlockModel block,
        int indent,
        int width)
    {
        IReadOnlyList<IReadOnlyList<IReadOnlyList<MarkdownInlineModel>>> rows = block.Rows ?? [];

        if (rows.Count == 0 || rows[0].Count == 0)
        {
            return;
        }

        int[] rights = ColumnRights(rows, indent, width);

        for (int row = 0; row < rows.Count; row++)
        {
            Append(rtf, $@"\trowd\trgaph80\trleft{indent}");

            foreach (int right in rights)
            {
                Append(rtf, $@"{(row == 0 ? @"\clcbpat6" : string.Empty)}{Borders()}\cellx{right}");
            }

            for (int column = 0; column < rights.Length; column++)
            {
                rtf.Append(row == 0 ? @"\pard\intbl\sb40\sa40\b " : @"\pard\intbl\sb40\sa40 ");
                WriteInlines(rtf, marks, column < rows[row].Count ? rows[row][column] : [], TextSize);
                rtf.Append(row == 0 ? @"\b0\cell" : @"\cell");
            }

            rtf.Append(@"\row").AppendLine();
        }

        Spacer(rtf);
    }

    /// <returns>
    ///     Where each column ends, in twips: the table as wide as it can be, each column as wide as its longest text
    ///     wants, within limits, so a column of short names doesn't take as much as one of sentences.
    /// </returns>
    private static int[] ColumnRights(
        IReadOnlyList<IReadOnlyList<IReadOnlyList<MarkdownInlineModel>>> rows,
        int indent,
        int width)
    {
        int columns = rows[0].Count;
        double[] wants = new double[columns];

        foreach (IReadOnlyList<IReadOnlyList<MarkdownInlineModel>> row in rows)
        {
            for (int column = 0; column < Math.Min(columns, row.Count); column++)
            {
                string text = string.Concat(row[column].Select(piece => piece.Text));
                int length = text.Split('\n').Max(line => line.Length);
                wants[column] = Math.Max(wants[column], Math.Clamp(length, 4, 45));
            }
        }

        double total = wants.Sum();
        int available = width - indent;
        int[] rights = new int[columns];
        double right = indent;

        for (int column = 0; column < columns; column++)
        {
            right += available * wants[column] / total;
            rights[column] = (int)right;
        }

        return rights;
    }

    private static string Borders() =>
        @"\clbrdrt\brdrs\brdrw10\brdrcf5\clbrdrl\brdrs\brdrw10\brdrcf5"
        + @"\clbrdrb\brdrs\brdrw10\brdrcf5\clbrdrr\brdrs\brdrw10\brdrcf5";

    // Some space after a table, which its last row's own spacing doesn't give
    private static void Spacer(StringBuilder rtf) =>
        Append(rtf, $@"\pard\sa0\fs{TextSize / 2}\par\fs{TextSize}").AppendLine();

    private static void WriteInlines(
        StringBuilder rtf,
        List<HelpRtfMarkModel> marks,
        IReadOnlyList<MarkdownInlineModel> inlines,
        int size)
    {
        for (int i = 0; i < inlines.Count; i++)
        {
            MarkdownInlineModel piece = inlines[i];

            // A link starts with a mark, once for all its pieces, as long as all of them
            if (piece.Link is not null && (i == 0 || inlines[i - 1].Link != piece.Link))
            {
                int length = inlines.Skip(i).TakeWhile(next => next.Link == piece.Link).Sum(next => next.Text.Length);
                marks.Add(new HelpRtfMarkModel(piece.Link, null, length));
                rtf.Append(Escape(Mark.ToString()));
            }

            var format = new StringBuilder();

            if (piece.Link is not null)
            {
                format.Append(@"\cf2\ul");
            }

            if (piece.Style.HasFlag(MarkdownInlineStyle.Bold))
            {
                format.Append(@"\b");
            }

            if (piece.Style.HasFlag(MarkdownInlineStyle.Italic))
            {
                format.Append(@"\i");
            }

            if (piece.Style.HasFlag(MarkdownInlineStyle.Code))
            {
                Append(format, $@"\f1\fs{size - 2}\highlight3");
            }

            // The space ends the last control word; without one, it would be text
            rtf.Append('{').Append(format).Append(format.Length > 0 ? " " : string.Empty);
            rtf.Append(Escape(piece.Text)).Append('}');
        }
    }

    /// <summary>Text as RTF: its special characters escaped, the others by their Unicode number.</summary>
    public static string Escape(string text)
    {
        var escaped = new StringBuilder(text.Length);

        foreach (char character in text)
        {
            switch (character)
            {
                case '\\' or '{' or '}':
                    escaped.Append('\\').Append(character);
                    break;

                case '\n':
                    escaped.Append(@"\line ");
                    break;

                case '\t':
                    escaped.Append(@"\tab ");
                    break;

                case < (char)128:
                    escaped.Append(character);
                    break;

                default:
                    // RTF has Unicode numbers as 16-bit signed, with a ? for readers that don't know it
                    escaped.Append(CultureInfo.InvariantCulture, $@"\u{(short)character}?");
                    break;
            }
        }

        return escaped.ToString();
    }

    private static StringBuilder Append(StringBuilder rtf, FormattableString text) =>
        rtf.Append(FormattableString.Invariant(text));
}
