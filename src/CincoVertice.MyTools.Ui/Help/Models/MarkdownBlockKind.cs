namespace CincoVertice.MyTools.Ui.Help.Models;

/// <summary>What a block is; a quote isn't one of its own, but any of these in a quote.</summary>
public enum MarkdownBlockKind
{
    Heading,
    Paragraph,
    ListItem,
    CodeBlock,
    Table,
    Rule,
}
