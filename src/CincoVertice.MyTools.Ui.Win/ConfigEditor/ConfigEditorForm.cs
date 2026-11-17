using System.ComponentModel;
using CincoVertice.Common.Ui.Win.Controls.Rtf;
using CincoVertice.Common.Utils.Highlighting;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.ConfigEditor.Presenters;
using CincoVertice.MyTools.Ui.ConfigEditor.Views;
using CincoVertice.MyTools.Ui.Win.Scripting;

namespace CincoVertice.MyTools.Ui.Win.ConfigEditor;

/// <summary>
///     Passive view of <see cref="ConfigEditorPresenter"/>: the files on the left, the code on the right, and the
///     problems under it while there are any.
/// </summary>
public partial class ConfigEditorForm : Form, IConfigEditorView
{
    private const int MaxSuggestionRows = 10;
    private const int SuggestionsWidth = 560;
    private const int SuggestionNameWidth = 220;

    // The list and the text are being set from the presenter, not by the user
    private bool _isShowing;

    // A key that changes the word being typed, or starts a line, went in: the text asks once it changed
    private SuggestionTrigger? _pendingTrigger;
    private IReadOnlyList<CodeSuggestionModel> _suggestions = [];
    private int _suggestionNameWidth;

    public ConfigEditorForm()
    {
        InitializeComponent();
        SuggestionsList.ItemHeight = SuggestionsList.Font.Height + LogicalToDeviceUnits(4);

        // The editor asks what to show where the pointer rests, which the presenter knows
        CodeEditor.HoverText = position =>
        {
            var hover = new CodeHoverModel(position);
            HoverRequested?.Invoke(this, hover);

            return hover.Text;
        };
    }

    public event EventHandler<EditableFileModel>? FileSelected;

    public event EventHandler? TextEdited;

    public event EventHandler? SaveRequested;

    public event EventHandler? NewScriptRequested;

    public event EventHandler<int>? ProblemSelected;

    public event EventHandler? ViewActivated;

    public event EventHandler<CancelEventArgs>? CloseRequested;

    public event EventHandler<SuggestionTrigger>? SuggestionsRequested;

    public event EventHandler<int>? SuggestionChosen;

    public event EventHandler<CodeHoverModel>? HoverRequested;

    public string CodeText => CodeEditor.Text;

    public int CaretPosition => CodeEditor.SelectionStart;

    public void ShowFiles(IReadOnlyList<EditableFileModel> files, EditableFileModel? selected)
    {
        _isShowing = true;

        try
        {
            FilesList.BeginUpdate();
            FilesList.Items.Clear();
            FilesList.Items.AddRange([.. files]);
            FilesList.EndUpdate();
            FilesList.SelectedItem = selected;
        }
        finally
        {
            _isShowing = false;
        }
    }

    public void ShowCode(string text)
    {
        _isShowing = true;

        try
        {
            EditedTimer.Stop();
            HideSuggestions();
            CodeEditor.SetCode(text);
        }
        finally
        {
            _isShowing = false;
        }
    }

    public void ShowHighlights(IReadOnlyList<CodeSpan> spans) => CodeEditor.SetHighlights(spans);

    public void ShowProblems(IReadOnlyList<EditableFileProblemModel> problems)
    {
        CodeEditor.SetMarks(problems.Select(problem => new CodeMark(problem.Line, problem.Message, problem.Column)));

        _isShowing = true;

        try
        {
            ProblemsList.BeginUpdate();
            ProblemsList.Items.Clear();
            ProblemsList.Items.AddRange([.. problems.Select(problem => $"Line {problem.Line}: {problem.Message}")]);
            ProblemsList.EndUpdate();
            ProblemsList.Visible = problems.Count > 0;
        }
        finally
        {
            _isShowing = false;
        }
    }

    public void ShowTitle(string title)
    {
        Text = title;
    }

    public void ShowCanSave(bool canSave)
    {
        SaveButton.Enabled = canSave;
        CodeEditor.ReadOnly = !canSave;
    }

    public void ShowError(string message)
    {
        MessageBox.Show(this, message, ConfigEditorPresenter.WindowTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public void GoToLine(int line) => CodeEditor.GoToLine(line);

    public void ShowSuggestions(IReadOnlyList<CodeSuggestionModel> suggestions, bool selectFirst)
    {
        _suggestions = suggestions;

        using (Graphics graphics = SuggestionsList.CreateGraphics())
        {
            int widest = suggestions.Max(item =>
                TextRenderer.MeasureText(graphics, item.Text, SuggestionsList.Font).Width);
            _suggestionNameWidth = Math.Min(LogicalToDeviceUnits(SuggestionNameWidth), widest)
                + LogicalToDeviceUnits(12);
        }

        SuggestionsList.BeginUpdate();
        SuggestionsList.Items.Clear();
        SuggestionsList.Items.AddRange([.. suggestions.Select(item => item.Text)]);
        SuggestionsList.EndUpdate();
        SuggestionsList.SelectedIndex = selectFirst ? 0 : -1;
        PlaceSuggestions();
        SuggestionsList.Visible = true;
        SuggestionsList.BringToFront();
    }

    public void HideSuggestions()
    {
        SuggestionsList.Visible = false;
        _suggestions = [];
    }

    public void ReplaceText(int start, int length, string text)
    {
        CodeEditor.Select(start, length);
        CodeEditor.SelectedText = text;
        CodeEditor.Focus();
    }

    public ConfigEditorSaveChoice AskSaveChanges(string name)
    {
        DialogResult answer = MessageBox.Show(
            this,
            $"Save the changes to {name}?",
            ConfigEditorPresenter.WindowTitle,
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        return answer switch
        {
            DialogResult.Yes => ConfigEditorSaveChoice.Save,
            DialogResult.No => ConfigEditorSaveChoice.Discard,
            _ => ConfigEditorSaveChoice.Cancel,
        };
    }

    public bool AskOverwrite(string name)
    {
        DialogResult answer = MessageBox.Show(
            this,
            $"{name} was changed elsewhere since it was opened. Replace it with this text?",
            ConfigEditorPresenter.WindowTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        return answer == DialogResult.Yes;
    }

    public string? AskNewScriptName() => InputDialog.Show("New script", "Script name:", string.Empty);

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Space))
        {
            SuggestionsRequested?.Invoke(this, SuggestionTrigger.Asked);

            return true;
        }

        if (SuggestionsList.Visible && HandleSuggestionKey(keyData))
        {
            return true;
        }

        if (keyData == (Keys.Control | Keys.S))
        {
            if (SaveButton.Enabled)
            {
                SaveRequested?.Invoke(this, EventArgs.Empty);
            }

            return true;
        }

        if (keyData is (Keys.Control | Keys.D0) or (Keys.Control | Keys.NumPad0))
        {
            CodeEditor.ResetZoom();

            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    // While the suggestions show, the keys that choose among them go to them, not to the text
    private bool HandleSuggestionKey(Keys keyData)
    {
        int page = Math.Max(1, SuggestionsList.ClientSize.Height / SuggestionsList.ItemHeight);

        switch (keyData)
        {
            case Keys.Up:
                MoveSuggestion(-1);
                return true;
            case Keys.Down:
                MoveSuggestion(1);
                return true;
            case Keys.PageUp:
                MoveSuggestion(-page);
                return true;
            case Keys.PageDown:
                MoveSuggestion(page);
                return true;
            case Keys.Enter:
            case Keys.Tab:
                // None chosen yet: the key goes to the text, as if the list weren't there
                if (SuggestionsList.SelectedIndex < 0)
                {
                    HideSuggestions();

                    return false;
                }

                SuggestionChosen?.Invoke(this, SuggestionsList.SelectedIndex);
                return true;
            case Keys.Escape:
                HideSuggestions();
                return true;
            default:
                return false;
        }
    }

    private void MoveSuggestion(int by)
    {
        int last = SuggestionsList.Items.Count - 1;
        SuggestionsList.SelectedIndex = Math.Clamp(SuggestionsList.SelectedIndex + by, 0, last);
    }

    // Under the caret's line, or above it when there's no room below, and inside the window
    private void PlaceSuggestions()
    {
        int caret = CodeEditor.SelectionStart;
        string text = CodeEditor.Text;
        int lineHeight = (int)Math.Ceiling(CodeEditor.Font.GetHeight() * CodeEditor.ZoomPercent / 100f) + 2;
        Point at = CodeEditor.GetPositionFromCharIndex(caret);

        // RichTextBox has no place for the end of its text: the last character's, one further
        if (caret > 0 && caret >= text.Length)
        {
            Point last = CodeEditor.GetPositionFromCharIndex(caret - 1);
            int lastWidth = TextRenderer.MeasureText(text[(caret - 1)..], CodeEditor.Font).Width;
            at = text[caret - 1] == '\n'
                ? new Point(0, last.Y + lineHeight)
                : new Point(last.X + lastWidth, last.Y);
        }

        Point below = PointToClient(CodeEditor.PointToScreen(new Point(at.X, at.Y + lineHeight)));
        int rows = Math.Min(SuggestionsList.Items.Count, MaxSuggestionRows);
        int border = SuggestionsList.Height - SuggestionsList.ClientSize.Height;
        int height = (rows * SuggestionsList.ItemHeight) + border;
        int width = Math.Min(LogicalToDeviceUnits(SuggestionsWidth), ClientSize.Width);
        int x = Math.Clamp(below.X, 0, Math.Max(0, ClientSize.Width - width));
        int y = below.Y + height > ClientSize.Height ? below.Y - lineHeight - height : below.Y;
        SuggestionsList.Bounds = new Rectangle(x, Math.Max(0, y), width, height);
    }

    private void FilesList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowing && FilesList.SelectedItem is EditableFileModel file)
        {
            FileSelected?.Invoke(this, file);
        }
    }

    private void ProblemsList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowing && ProblemsList.SelectedIndex >= 0)
        {
            ProblemSelected?.Invoke(this, ProblemsList.SelectedIndex);
        }
    }

    private void CodeEditor_TextChanged(object? sender, EventArgs e)
    {
        if (!_isShowing)
        {
            EditedTimer.Stop();
            EditedTimer.Start();
        }

        if (_pendingTrigger is { } trigger)
        {
            _pendingTrigger = null;
            SuggestionsRequested?.Invoke(this, trigger);
        }
    }

    // A letter, a digit, or what starts the next part: a module's ".", a shortcut's "=" or "+", a value's ","
    private void CodeEditor_KeyPress(object? sender, KeyPressEventArgs e)
    {
        char c = e.KeyChar;
        bool isPart = char.IsLetterOrDigit(c) || c is '_' or '.' or '=' or '+' or '"' or ','
            || (c == ' ' && SuggestionsList.Visible);

        // Enter's character comes after its key, which asked for the new line's suggestions
        if (c != '\r')
        {
            _pendingTrigger = isPart ? SuggestionTrigger.Typing : null;
        }
    }

    private void CodeEditor_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Back:
            case Keys.Delete:
                _pendingTrigger = SuggestionsList.Visible ? SuggestionTrigger.Typing : null;
                break;
            case Keys.Left:
            case Keys.Right:
            case Keys.Home:
            case Keys.End:
                HideSuggestions();
                break;
            case Keys.Enter:
                HideSuggestions();
                _pendingTrigger = SuggestionTrigger.NewLine;
                break;
        }
    }

    private void CodeEditor_MouseDown(object? sender, MouseEventArgs e) => HideSuggestions();

    // Unless the focus went to the list, a click on which chooses
    private void CodeEditor_Leave(object? sender, EventArgs e)
    {
        BeginInvoke(() =>
        {
            if (!SuggestionsList.Focused)
            {
                HideSuggestions();
            }
        });
    }

    private void SuggestionsList_MouseClick(object? sender, MouseEventArgs e)
    {
        int index = SuggestionsList.IndexFromPoint(e.Location);

        if (index >= 0)
        {
            SuggestionChosen?.Invoke(this, index);
        }

        CodeEditor.Focus();
    }

    // Each suggestion's name, then what it is or does, dimmed
    private void SuggestionsList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        e.DrawBackground();

        if (e.Index < 0 || e.Index >= _suggestions.Count)
        {
            return;
        }

        CodeSuggestionModel item = _suggestions[e.Index];
        bool isSelected = (e.State & DrawItemState.Selected) != 0;
        Color text = isSelected ? SystemColors.HighlightText : SystemColors.WindowText;
        Color detail = isSelected ? SystemColors.HighlightText : SystemColors.GrayText;
        const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
            | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        int padding = LogicalToDeviceUnits(4);
        var name = new Rectangle(e.Bounds.X + padding, e.Bounds.Y, _suggestionNameWidth, e.Bounds.Height);
        var rest = Rectangle.FromLTRB(name.Right, e.Bounds.Y, e.Bounds.Right - padding, e.Bounds.Bottom);
        TextRenderer.DrawText(e.Graphics, item.Text, e.Font, name, text, flags);
        TextRenderer.DrawText(e.Graphics, item.Detail, e.Font, rest, detail, flags);
    }

    private void ConfigEditorForm_Deactivate(object? sender, EventArgs e) => HideSuggestions();

    private void EditedTimer_Tick(object? sender, EventArgs e)
    {
        EditedTimer.Stop();
        TextEdited?.Invoke(this, EventArgs.Empty);
    }

    private void CodeEditor_ZoomChanged(object? sender, EventArgs e)
    {
        ZoomButton.Text = $"{CodeEditor.ZoomPercent}%";
    }

    private void ZoomButton_Click(object? sender, EventArgs e)
    {
        CodeEditor.ResetZoom();
        CodeEditor.Focus();
    }

    private void SaveButton_Click(object? sender, EventArgs e) => SaveRequested?.Invoke(this, EventArgs.Empty);

    private void NewScriptButton_Click(object? sender, EventArgs e) => NewScriptRequested?.Invoke(this, EventArgs.Empty);

    private void ConfigEditorForm_Activated(object? sender, EventArgs e) => ViewActivated?.Invoke(this, EventArgs.Empty);

    private void ConfigEditorForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        var closing = new CancelEventArgs();

        CloseRequested?.Invoke(this, closing);
        e.Cancel = closing.Cancel;
    }
}
