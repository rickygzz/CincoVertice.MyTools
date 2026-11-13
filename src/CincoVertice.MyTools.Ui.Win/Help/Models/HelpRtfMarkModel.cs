namespace CincoVertice.MyTools.Ui.Win.Help.Models;

/// <summary>
///     What a mark in a page's RTF stands for: a link, as long as its text, or a heading's start. The marks are
///     <see cref="Services.MarkdownRtfWriter.Mark"/> characters, in the order of these.
/// </summary>
/// <param name="Link">The link's target, as written; null for a heading.</param>
/// <param name="Anchor">The heading's link name; null for a link.</param>
/// <param name="Length">How many characters of text the link has after the mark.</param>
public sealed record HelpRtfMarkModel(string? Link, string? Anchor, int Length = 0);
