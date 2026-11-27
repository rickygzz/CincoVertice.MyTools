namespace CincoVertice.MyTools.Ui.RegexTester.Models;

/// <summary>
///     A part of a pattern the Add menu writes (<see cref="Services.RegexPieces"/>), so a pattern can be made without
///     knowing the syntax.
/// </summary>
/// <param name="Group">The menu it's in; empty for the menu itself.</param>
/// <param name="Title">Its line in the menu.</param>
/// <param name="Description">What it matches, as its tooltip.</param>
/// <param name="Template">
///     What it writes; <see cref="ValueSign"/> stands for the part selected in the pattern, or else for
///     <paramref name="Placeholder"/>.
/// </param>
/// <param name="Placeholder">Written for the value when nothing is selected, and selected, to type over.</param>
/// <param name="Pick">A part of the template selected after it's written, when no placeholder is.</param>
/// <param name="IsQuantifier">Says how many times what's before it, or the part selected, comes.</param>
/// <param name="IsPerLine">Means a whole line, so it ticks ^ and $ at each line.</param>
/// <param name="IsSampleText">Writes the text selected in the sample, its special characters escaped.</param>
public sealed record RegexPieceModel(
    string Group,
    string Title,
    string Description,
    string Template,
    string? Placeholder = null,
    string? Pick = null,
    bool IsQuantifier = false,
    bool IsPerLine = false,
    bool IsSampleText = false)
{
    /// <summary>Where the value goes in <see cref="Template"/>.</summary>
    public const string ValueSign = "…";

    /// <summary>The template as the menu shows it, beside the title: the placeholder in place of the value.</summary>
    public string Example => Template.Replace(ValueSign, Placeholder ?? string.Empty);
}
