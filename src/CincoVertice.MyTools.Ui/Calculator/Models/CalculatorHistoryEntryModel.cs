namespace CincoVertice.MyTools.Ui.Calculator.Models;

/// <summary>
///     An operation in the calculator's history.
/// </summary>
/// <param name="Operation">The expression the user typed.</param>
/// <param name="Result">Its result, formatted to be shown.</param>
public sealed record CalculatorHistoryEntryModel(string Operation, string Result);
