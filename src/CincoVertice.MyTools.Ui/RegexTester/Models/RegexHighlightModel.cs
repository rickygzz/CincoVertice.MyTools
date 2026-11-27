namespace CincoVertice.MyTools.Ui.RegexTester.Models;

/// <summary>A part of the text to color.</summary>
/// <param name="Group">For a group, its number, so each has its color; 0 for a match.</param>
public sealed record RegexHighlightModel(int Start, int Length, RegexHighlightKind Kind, int Group = 0);
