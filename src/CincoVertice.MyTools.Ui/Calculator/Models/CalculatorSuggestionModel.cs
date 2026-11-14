namespace CincoVertice.MyTools.Ui.Calculator.Models;

/// <summary>
///     Something the calculator offers to write in place of what's being typed.
/// </summary>
/// <param name="Text">What the list shows, e.g. "Mexico City (UTC-6)" or "km (kilometer, length)".</param>
/// <param name="Insert">What goes in the expression, e.g. "Mexico City to " or "mi".</param>
public sealed record CalculatorSuggestionModel(string Text, string Insert);
