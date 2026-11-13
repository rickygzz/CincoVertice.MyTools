namespace CincoVertice.MyTools.Ui.Help.Models;

/// <summary>A piece of a Markdown line, all of one style: plain, bold, code, a link. A line break is "\n".</summary>
/// <param name="Link">Where it goes when it's a link, as written: "02-hotkeys.md#default-hotkeys", "#usage".</param>
public sealed record MarkdownInlineModel(
    string Text,
    MarkdownInlineStyle Style = MarkdownInlineStyle.None,
    string? Link = null);
