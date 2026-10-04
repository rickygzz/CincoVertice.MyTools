namespace CincoVertice.MyTools.Ui.Calculator.Views;

public interface ICalculatorView
{
    /// <summary>Raised when the user asks to evaluate the expression (e.g. Enter).</summary>
    event EventHandler? EvaluateRequested;

    /// <summary>The expression typed by the user.</summary>
    string Expression { get; }

    /// <summary>Shows the view and gives it the focus.</summary>
    void ShowAndActivate();

    /// <summary>Shows a calculated result.</summary>
    void ShowResult(string result);

    /// <summary>Shows why the expression could not be evaluated and selects the part at fault.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="selectionStart">Where the error is in the expression.</param>
    /// <param name="selectionLength">1 to select the character at fault, 0 for a caret at the end.</param>
    void ShowError(string message, int selectionStart, int selectionLength);
}
