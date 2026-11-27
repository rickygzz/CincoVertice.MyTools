namespace CincoVertice.MyTools.Ui.RegexTester.Models;

/// <summary>A group of a match: its number, its name when it has one, and what it matched, where.</summary>
/// <param name="Success">False when the group took no part in the match, as an optional one.</param>
public sealed record RegexGroupModel(int Number, string? Name, int Index, int Length, string Value, bool Success);
