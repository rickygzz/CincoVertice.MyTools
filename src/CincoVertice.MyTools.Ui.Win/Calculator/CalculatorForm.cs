using CincoVertice.Common.Ui.Rtf.Constants;
using CincoVertice.Common.Ui.Rtf.Themes;
using CincoVertice.MyTools.Ui.Calculator.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;

namespace CincoVertice.MyTools.Ui.Win.Calculator;

/// <summary>
///     Calculator view. <see cref="Ui.Calculator.Presenters.CalculatorPresenter"/> evaluates the expression;
///     the form only shows what it is given.
/// </summary>
public partial class CalculatorForm : Form, ICalculatorView
{
    public CalculatorForm()
    {
        InitializeComponent();
        ResultOutputControl.SetTheme(new ModernConsole());

        // Safety net for Alt+F4 (still works with ControlBox = false) or any future Close() call
        FormExtensions.HideOnUserClose(this);
        DragMover.Attach(this, CalculatorTextBox, ResultOutputControl);
    }

    public event EventHandler? EvaluateRequested;

    public string Expression => CalculatorTextBox.Text;

    public void ShowAndActivate()
    {
        Show();
        Activate();
    }

    public void ShowResult(string result)
    {
        ResultOutputControl.SetText(result, RtfConstants.Console, updateContent: true);
        ResultOutputControl.Visible = true;
    }

    public void ShowError(string message, int selectionStart, int selectionLength)
    {
        ResultOutputControl.SetText(message, RtfConstants.Highlight, updateContent: true);
        ResultOutputControl.Visible = true;

        CalculatorTextBox.Select(selectionStart, selectionLength);
    }

    // The form is a singleton reused on every hotkey press, so this runs on each show (Shown only fires once).
    private void CalculatorForm_VisibleChanged(object? sender, EventArgs e)
    {
        if (!Visible)
        {
            return;
        }

        FormExtensions.CenterOnCursorScreen(this);

        // Not Focus(): the form is not active yet, and on activation WinForms restores ActiveControl,
        // which would otherwise be the control that had focus when the form was last hidden.
        ActiveControl = CalculatorTextBox;
        CalculatorTextBox.SelectAll();
    }

    private void CalculatorForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (KeyboardMover.TryMove(this, e))
        {
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            Hide();
        }
        else if (e.KeyCode == Keys.Enter)
        {
            EvaluateRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void CalculatorForm_Deactivate(object sender, EventArgs e)
    {
        Hide();
    }
}
