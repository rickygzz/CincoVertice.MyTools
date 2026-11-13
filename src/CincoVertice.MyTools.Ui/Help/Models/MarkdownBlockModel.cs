namespace CincoVertice.MyTools.Ui.Help.Models;

/// <summary>A block of a Markdown page: a heading, a paragraph, a list item, a code block, a table...</summary>
/// <param name="Inlines">Its text; for a code block, one piece with all of its lines.</param>
/// <param name="Level">
///     A heading's level, 1 to 6; how deep a list item is, from 0; for a paragraph or code block, how far it's indented
///     under a list item, from 0.
/// </param>
/// <param name="Marker">A list item's bullet, "•", number, "3.", or box, "☐" or "☑" for a task.</param>
/// <param name="Anchor">A heading's link name, as GitHub makes it: "Quick Start" is "quick-start".</param>
/// <param name="Language">A code block's language, e.g. "mermaid"; empty when it says none.</param>
/// <param name="Rows">A table's rows of cells, the header row first.</param>
/// <param name="IsQuoted">Whether it's in a quote, the lines starting with "&gt;".</param>
/// <param name="Alignments">How each column of a table lines up.</param>
public sealed record MarkdownBlockModel(
    MarkdownBlockKind Kind,
    IReadOnlyList<MarkdownInlineModel> Inlines,
    int Level = 0,
    string? Marker = null,
    string? Anchor = null,
    string Language = "",
    IReadOnlyList<IReadOnlyList<IReadOnlyList<MarkdownInlineModel>>>? Rows = null,
    bool IsQuoted = false,
    IReadOnlyList<MarkdownAlignment>? Alignments = null)
{
    /// <summary>Its text without any style, e.g. a heading's.</summary>
    public string PlainText => string.Concat(Inlines.Select(inline => inline.Text));
}
