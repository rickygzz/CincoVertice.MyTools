namespace CincoVertice.MyTools.Ui.RegexTester.Models;

/// <summary>A ready-made pattern of the Examples menu (<see cref="Services.RegexExamples"/>), to use or start from.</summary>
/// <param name="Group">The submenu it's in.</param>
/// <param name="Title">Its line in the menu.</param>
/// <param name="Description">What it matches, and its groups, as its tooltip.</param>
/// <param name="Sample">A text it matches in, shown when the user's text is empty.</param>
/// <param name="Replacement">What a match becomes, when the example is about replacing; empty otherwise.</param>
public sealed record RegexExampleModel(
    string Group,
    string Title,
    string Description,
    string Pattern,
    string Sample,
    string Replacement = "",
    bool IgnoreCase = false,
    bool Multiline = false,
    bool DotAll = false);
