using CincoVertice.MyTools.Ui.Calculator.Models;

namespace CincoVertice.MyTools.Ui.Calculator.Views;

public interface ICalculatorView
{
    /// <summary>Raised when the user asks to evaluate the expression (e.g. Enter).</summary>
    event EventHandler? EvaluateRequested;

    /// <summary>Raised when the user asks to show or hide the history (e.g. Ctrl+H).</summary>
    event EventHandler? ToggleHistoryRequested;

    /// <summary>The expression typed by the user.</summary>
    string Expression { get; }

    /// <summary>Shows the view and gives it the focus.</summary>
    void ShowAndActivate();

    /// <summary>Shows a calculated result.</summary>
    void ShowResult(string result);

    /// <summary>
    ///     Makes the output taller if it isn't yet, and shows these operations in it, each with its operation and
    ///     result in different colors, scrolled to the last.
    /// </summary>
    /// <param name="entries">The operations, oldest first.</param>
    /// <param name="emptyText">What to show when there are no operations.</param>
    void ShowHistory(IReadOnlyList<CalculatorHistoryEntryModel> entries, string emptyText);

    /// <summary>Makes the output its normal height again.</summary>
    void HideHistory();

    /// <summary>
    ///     Shows why the expression could not be evaluated and selects the part at fault. While the history is shown,
    ///     the message goes below it.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="selectionStart">Where the error is in the expression.</param>
    /// <param name="selectionLength">1 to select the character at fault, 0 for a caret at the end.</param>
    void ShowError(string message, int selectionStart, int selectionLength);
}
