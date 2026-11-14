namespace CincoVertice.MyTools.Ui.Calculator.Models;

/// <summary>
///     What the calculator offers for the text before the caret, and the part of it a choice replaces.
/// </summary>
/// <param name="Start">Where the part being typed starts in the expression.</param>
/// <param name="Length">How long it is; 0 when nothing of it is typed yet.</param>
/// <param name="Items">The choices, in order; never empty.</param>
public sealed record CalculatorSuggestionsModel(int Start, int Length, IReadOnlyList<CalculatorSuggestionModel> Items);
