namespace CincoVertice.MyTools.Ui.RegexTester.Models;

/// <summary>What a pattern gives on the text.</summary>
/// <param name="Matches">The matches, at most <see cref="Services.RegexRunner.MaxMatches"/>.</param>
/// <param name="HasMore">There are more matches than those.</param>
/// <param name="Error">Why the pattern can't run, or took too long; null when it ran.</param>
/// <param name="Replaced">The text with every match replaced; null without a replacement or with an error.</param>
public sealed record RegexResultModel(
    IReadOnlyList<RegexMatchModel> Matches,
    bool HasMore,
    string? Error,
    string? Replaced);
