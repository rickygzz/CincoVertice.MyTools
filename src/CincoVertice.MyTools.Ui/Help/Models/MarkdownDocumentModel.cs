namespace CincoVertice.MyTools.Ui.Help.Models;

/// <summary>A Markdown page, as blocks.</summary>
public sealed record MarkdownDocumentModel(IReadOnlyList<MarkdownBlockModel> Blocks)
{
    /// <summary>The text of its first heading; null when it has none.</summary>
    public string? Title => Blocks.FirstOrDefault(block => block.Kind == MarkdownBlockKind.Heading)?.PlainText;
}
