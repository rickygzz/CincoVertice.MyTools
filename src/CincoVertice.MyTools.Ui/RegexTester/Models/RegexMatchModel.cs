namespace CincoVertice.MyTools.Ui.RegexTester.Models;

/// <summary>A match: its number from 1, where it is in the text, what it is, and its groups (not the whole match).</summary>
public sealed record RegexMatchModel(
    int Number,
    int Index,
    int Length,
    string Value,
    IReadOnlyList<RegexGroupModel> Groups);
