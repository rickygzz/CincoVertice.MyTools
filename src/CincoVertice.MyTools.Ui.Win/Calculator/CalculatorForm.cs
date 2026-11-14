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

    // Rows the list of suggestions shows at once; it scrolls for more
    private const int MaxSuggestionRows = 8;

    // What was added to the output's height; 0 at its normal height. Worked out when it grows, not in the
    // constructor, because the form is only scaled to the screen's DPI when its handle is created.
    private int _historyExtraHeight;

    // What was added to the form's height so the suggestions fit; 0 while they're hidden
    private int _suggestionsExtraHeight;

    // While the presenter writes the expression, which says it changed once the caret is placed
    private bool _isSettingExpression;

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

    public event EventHandler? ExpressionChanged;

    public event EventHandler<int>? SuggestionChosen;

    public event EventHandler<int>? RecallRequested;

    public string Expression => CalculatorTextBox.Text;

    public int Caret => CalculatorTextBox.SelectionStart;

    public void SetExpression(string expression, int caret)
    {
        _isSettingExpression = true;
        CalculatorTextBox.Text = expression;
        CalculatorTextBox.Select(caret, 0);
        _isSettingExpression = false;
        ExpressionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ShowSuggestions(IReadOnlyList<string> items)
    {
        SuggestionsListBox.BeginUpdate();
        SuggestionsListBox.Items.Clear();
        SuggestionsListBox.Items.AddRange([.. items]);
        SuggestionsListBox.EndUpdate();
        SuggestionsListBox.SelectedIndex = -1;

        // Shown first: until its window is there, the list says its rows are shorter than they are
        SuggestionsListBox.Visible = true;
        SuggestionsListBox.BringToFront();

        int border = SuggestionsListBox.Height - SuggestionsListBox.ClientSize.Height;
        int rows = Math.Min(items.Count, MaxSuggestionRows);

        SuggestionsListBox.Location =
            new Point(CalculatorTextBox.Left, CalculatorTextBox.Bottom + LogicalToDeviceUnits(2));
        SuggestionsListBox.Height = (SuggestionsListBox.ItemHeight * rows) + border;

        // Over the result; the window grows when it's too short, as it is without the history
        int needed = SuggestionsListBox.Bottom + LogicalToDeviceUnits(5);
        SetSuggestionsExtraHeight(Math.Max(0, needed - (ClientSize.Height - _suggestionsExtraHeight)));
    }

    public void HideSuggestions()
    {
        SuggestionsListBox.Visible = false;
        SetSuggestionsExtraHeight(0);
    }

    /// <summary>
    ///     While suggestions show: Down and Up go through them, Tab writes the one chosen or else the first, Enter the
    ///     one chosen (else it calculates), and Esc hides them rather than the calculator.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (SuggestionsListBox.Visible)
        {
            int chosen = SuggestionsListBox.SelectedIndex;

            switch (keyData)
            {
                case Keys.Down:
                    SuggestionsListBox.SelectedIndex = Math.Min(chosen + 1, SuggestionsListBox.Items.Count - 1);
                    return true;

                case Keys.Up:
                    SuggestionsListBox.SelectedIndex = Math.Max(chosen - 1, -1);
                    return true;

                case Keys.Tab:
                    SuggestionChosen?.Invoke(this, Math.Max(chosen, 0));
                    return true;

                case Keys.Enter when chosen >= 0:
                    SuggestionChosen?.Invoke(this, chosen);
                    return true;

                case Keys.Escape:
                    HideSuggestions();
                    return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

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
        // Up and Down bring back the lines entered; while suggestions show, they go through them instead
        else if (e.KeyData is Keys.Up or Keys.Down)
        {
            RecallRequested?.Invoke(this, e.KeyData == Keys.Up ? -1 : 1);
            e.Handled = true;
            e.SuppressKeyPress = true;
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

    private void SetSuggestionsExtraHeight(int extraHeight)
    {
        int change = extraHeight - _suggestionsExtraHeight;

        if (change == 0)
        {
            return;
        }

        _suggestionsExtraHeight = extraHeight;
        ClientSize = new Size(ClientSize.Width, ClientSize.Height + change);

        FormExtensions.KeepBottomOnScreen(this);
    }

    private void CalculatorTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (!_isSettingExpression)
        {
            ExpressionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    // Then back to typing
    private void SuggestionsListBox_MouseClick(object? sender, MouseEventArgs e)
    {
        int index = SuggestionsListBox.IndexFromPoint(e.Location);

        CalculatorTextBox.Focus();

        if (index >= 0)
        {
            SuggestionChosen?.Invoke(this, index);
        }
    }

    private void CalculatorForm_Deactivate(object sender, EventArgs e)
    {
        Hide();
    }
}
