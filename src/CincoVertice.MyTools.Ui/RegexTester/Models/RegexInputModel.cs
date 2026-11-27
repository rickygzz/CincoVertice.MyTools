namespace CincoVertice.MyTools.Ui.RegexTester.Models;

/// <summary>What the regex tester runs: the pattern, its options, the text it's tried on, and a replacement.</summary>
/// <param name="IgnoreCase">Letters match in either case.</param>
/// <param name="Multiline">^ and $ match at each line's start and end, not only the text's.</param>
/// <param name="DotAll">. matches a new line too.</param>
/// <param name="Replacement">What each match becomes, with $1 or ${name} for a group; empty for no replacing.</param>
public sealed record RegexInputModel(
    string Pattern,
    string Text,
    string Replacement = "",
    bool IgnoreCase = false,
    bool Multiline = false,
    bool DotAll = false);
