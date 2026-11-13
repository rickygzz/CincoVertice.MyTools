namespace CincoVertice.MyTools.Ui.Help.Models;

/// <summary>How a piece of a Markdown line looks; several can go together, as bold italic.</summary>
[Flags]
public enum MarkdownInlineStyle
{
    None = 0,
    Bold = 1,
    Italic = 2,
    Code = 4,
}
