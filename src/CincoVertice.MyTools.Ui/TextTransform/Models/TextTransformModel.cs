namespace CincoVertice.MyTools.Ui.TextTransform.Models;

/// <summary>A transform as its menu offers it.</summary>
/// <param name="Kind">What it does.</param>
/// <param name="Title">Its menu item, with &amp; before the letter that chooses it: "&amp;UPPER CASE".</param>
/// <param name="Group">The items of a group show together, between lines: case, lines, encodings, JSON.</param>
public sealed record TextTransformModel(TextTransformKind Kind, string Title, int Group);
