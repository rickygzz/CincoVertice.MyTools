namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>What a comparison doesn't count as a difference.</summary>
/// <param name="IgnoreWhitespace">Spaces and tabs anywhere in a line, as <c>git diff -w</c>.</param>
/// <param name="IgnoreCase">Upper and lower case letters.</param>
public sealed record DiffOptionsModel(bool IgnoreWhitespace = false, bool IgnoreCase = false);
