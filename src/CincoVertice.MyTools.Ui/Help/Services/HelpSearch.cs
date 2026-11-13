using System.Text;
using CincoVertice.MyTools.Ui.Help.Models;

namespace CincoVertice.MyTools.Ui.Help.Services;

/// <summary>
///     Finds text in the pages of the user guide, as they read: headings, paragraphs, lists, code and tables, without
///     the Markdown around them; case doesn't count.
/// </summary>
public static class HelpSearch
{
    // Of the text around the first match, before it and after it
    private const int SnippetBefore = 30;
    private const int SnippetAfter = 70;

    /// <returns>The pages that have the text, those with the most first, then in the guide's order.</returns>
    public static IReadOnlyList<HelpSearchResultModel> Find(
        IEnumerable<(HelpPageModel Page, MarkdownDocumentModel Document)> pages,
        string text)
    {
        string query = text.Trim();

        if (query.Length == 0)
        {
            return [];
        }

        List<HelpSearchResultModel> results = [];

        foreach ((HelpPageModel page, MarkdownDocumentModel document) in pages)
        {
            int count = 0;
            string? snippet = null;

            foreach (string block in document.Blocks.Select(TextOf))
            {
                int first = IndexOf(block, query, 0);

                if (first < 0)
                {
                    continue;
                }

                snippet ??= SnippetOf(block, first, query.Length);

                for (int at = first; at >= 0; at = IndexOf(block, query, at + query.Length))
                {
                    count++;
                }
            }

            if (count > 0)
            {
                results.Add(new HelpSearchResultModel(page.Name, page.Title, snippet!, count));
            }
        }

        // OrderByDescending is stable: pages with as many keep the guide's order
        return [.. results.OrderByDescending(result => result.Count)];
    }

    private static int IndexOf(string text, string query, int start) =>
        text.IndexOf(query, start, StringComparison.CurrentCultureIgnoreCase);

    // A table's text is its cells'
    private static string TextOf(MarkdownBlockModel block) =>
        block.Rows is null
            ? block.PlainText
            : string.Join(
                " ",
                block.Rows.SelectMany(row => row).Select(cell => string.Concat(cell.Select(piece => piece.Text))));

    // Whole words around the match, on one line, with "…" where it's cut
    private static string SnippetOf(string text, int match, int length)
    {
        int start = Math.Max(0, match - SnippetBefore);
        int end = Math.Min(text.Length, match + length + SnippetAfter);

        while (start > 0 && start < match && !char.IsWhiteSpace(text[start - 1]))
        {
            start++;
        }

        while (end < text.Length && end > match + length && !char.IsWhiteSpace(text[end]))
        {
            end--;
        }

        var snippet = new StringBuilder();
        snippet.Append(start > 0 ? "…" : string.Empty);
        snippet.AppendJoin(' ', text[start..end].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        snippet.Append(end < text.Length ? "…" : string.Empty);

        return snippet.ToString();
    }
}
