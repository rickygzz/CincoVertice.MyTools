namespace CincoVertice.MyTools.Ui.RegexTester.Models;

/// <summary>A row of the regex tester's quick reference: a part of a pattern and what it means.</summary>
/// <param name="Group">The heading it's under.</param>
public sealed record RegexReferenceModel(string Group, string Pattern, string Description);
