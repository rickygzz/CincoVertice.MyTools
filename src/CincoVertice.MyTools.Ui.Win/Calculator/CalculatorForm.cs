using CincoVertice.Common.Ui.Rtf.Constants;
using CincoVertice.Common.Ui.Rtf.Models;
using CincoVertice.Common.Ui.Rtf.Themes;
using CincoVertice.MyTools.Ui.Calculator.Models;
using CincoVertice.MyTools.Ui.Calculator.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;

namespace CincoVertice.MyTools.Ui.Win.Calculator;

/// <summary>
///     Calculator view. <see cref="Ui.Calculator.Presenters.CalculatorPresenter"/> evaluates the expression;
///     the form only shows what it is given.
/// </summary>
public partial class CalculatorForm : Form, ICalculatorView
{
    // How many times its normal height the output is while it shows the history
    private const int HistoryHeightFactor = 8;

    // What was added to the output's height; 0 at its normal height. Worked out when it grows, not in the
    // constructor, because the form is only scaled to the screen's DPI when its handle is created.
    private int _historyExtraHeight;

    // The history's operations, like RtfConstants.Console (its results) but dimmer
    private readonly RtfFormat _operationFormat;

    public CalculatorForm()
    {
        InitializeComponent();

        var theme = new ModernConsole();

        ResultOutputControl.SetTheme(theme);
        _operationFormat = new RtfFormat
        {
            Bold = RtfConstants.Console.Bold,
            Italic = RtfConstants.Console.Italic,
            ForeColorIndex = theme.ColorIndex("Accent1Dark"),
            HighlightColorIndex = RtfConstants.Console.HighlightColorIndex,
            Size = RtfConstants.Console.Size,
            LineHeight = RtfConstants.Console.LineHeight,
        };

        // Safety net for Alt+F4 (still works with ControlBox = false) or any future Close() call
        FormExtensions.HideOnUserClose(this);
        DragMover.Attach(this, CalculatorTextBox, ResultOutputControl);
    }

    public event EventHandler? EvaluateRequested;

    public event EventHandler? ToggleHistoryRequested;

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

    public void ShowHistory(IReadOnlyList<CalculatorHistoryEntryModel> entries, string emptyText)
    {
        if (_historyExtraHeight == 0)
        {
            SetHistoryExtraHeight(ResultOutputControl.Height * (HistoryHeightFactor - 1));
        }

        ResultOutputControl.SetText(entries.Count == 0 ? emptyText : string.Empty, _operationFormat);

        for (int i = 0; i < entries.Count; i++)
        {
            if (i > 0)
            {
                ResultOutputControl.AddNewLine();
            }

            ResultOutputControl.AddText($"{entries[i].Operation} = ", _operationFormat);
            ResultOutputControl.AddText(entries[i].Result, RtfConstants.Console);
        }

        ResultOutputControl.UpdateText(scrollToBottom: true);
        ResultOutputControl.Visible = true;
    }

    public void HideHistory()
    {
        SetHistoryExtraHeight(0);
    }

    public void ShowError(string message, int selectionStart, int selectionLength)
    {
        if (_historyExtraHeight > 0)
        {
            ResultOutputControl.AddNewLine();
            ResultOutputControl.AddText(message, RtfConstants.Highlight, updateContent: true);
        }
        else
        {
            ResultOutputControl.SetText(message, RtfConstants.Highlight, updateContent: true);
        }

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
        else if (e.KeyData == (Keys.Control | Keys.H))
        {
            ToggleHistoryRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;

            // Otherwise a text box takes Ctrl+H as a backspace
            e.SuppressKeyPress = true;
        }
    }

    private void SetHistoryExtraHeight(int extraHeight)
    {
        int change = extraHeight - _historyExtraHeight;

        _historyExtraHeight = extraHeight;
        ResultOutputControl.Height += change;
        ClientSize = new Size(ClientSize.Width, ClientSize.Height + change);

        FormExtensions.KeepBottomOnScreen(this);
    }

    private void CalculatorForm_Deactivate(object sender, EventArgs e)
    {
        Hide();
    }
}
