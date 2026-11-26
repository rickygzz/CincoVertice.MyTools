using System.ComponentModel;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;

namespace CincoVertice.MyTools.Ui.Win.JsonEditor;

/// <summary>
///     JSON editor view: the text (<see cref="JsonTextControl"/>), a search box for text or JMESPath with its
///     suggestions, the result of an expression under the text, and the path at the caret in the status bar.
///     <see cref="Ui.JsonEditor.Presenters.JsonEditorPresenter"/> decides everything.
/// </summary>
public partial class JsonEditorForm : Form, IJsonEditorView
{
    private const int MaxSuggestionsShown = 10;

    private IReadOnlyList<JsonSuggestionModel> _suggestions = [];

    // While the search text or mode is set here, not by the user
    private bool _isSettingSearch;

    public JsonEditorForm()
    {
        InitializeComponent();
        _isSettingSearch = true;
        ModeComboBox.SelectedIndex = 0;
        _isSettingSearch = false;
        DpiFollower.Attach(
            this,
            dpi => SuggestionsList.ItemHeight = SuggestionsList.Font.Height + DpiFollower.Scaled(4, dpi));
    }

    public event EventHandler? TextEdited;

    public event EventHandler<DiffPosition>? CaretMoved;

    public event EventHandler? NewRequested;

    public event EventHandler? OpenRequested;

    public event EventHandler? SaveRequested;

    public event EventHandler? SaveAsRequested;

    public event EventHandler? FormatRequested;

    public event EventHandler? MinifyRequested;

    public event EventHandler<string>? FileDropped;

    public event EventHandler? SearchChanged;

    public event EventHandler<int>? SearchStepRequested;

    public event EventHandler<JsonSuggestionModel>? SuggestionChosen;

    public event EventHandler<JsonPathRequestModel>? PathRequested;

    public event EventHandler<CancelEventArgs>? CloseRequested;

    public IReadOnlyList<string> Lines => Editor.Lines;

    public string SearchText => SearchBox.Text;

    public JsonSearchMode SearchMode => ModeComboBox.SelectedIndex == 1 ? JsonSearchMode.JmesPath : JsonSearchMode.Text;

    internal JsonTextControl TextControl => Editor;

    internal IReadOnlyList<JsonSuggestionModel> SuggestionsShown => SuggestionsList.Visible ? _suggestions : [];

    public void ShowText(IReadOnlyList<string> lines) => Editor.SetText(lines);

    public void ReplaceText(string text) => Editor.ReplaceAll(text);

    public void ShowDocument(JsonDocumentModel document) => Editor.ShowDocument(document);

    public void ShowTitle(string title) => Text = title;

    public void ShowStatus(string position, string path, string validity)
    {
        PositionLabel.Text = position;
        PathLabel.Text = path;
        ValidityLabel.Text = validity;
        ValidityLabel.ForeColor = validity is "Valid JSON" or "Empty" ? SystemColors.ControlText : Color.Firebrick;
    }

    public void ShowMatches(IReadOnlyList<JsonMatchModel> matches, int current) => Editor.ShowMatches(matches, current);

    public void GoTo(JsonMatchModel match) => Editor.Select(match);

    public void ShowSearchMessage(string message, bool isProblem)
    {
        SearchMessageLabel.Text = message;
        SearchMessageLabel.ForeColor = isProblem ? Color.Firebrick : SystemColors.ControlText;
        Tips.SetToolTip(SearchMessageLabel, message);
    }

    public void ShowResult(string? json)
    {
        Split.Panel2Collapsed = json is null;
        ResultBox.Text = json?.ReplaceLineEndings("\r\n") ?? string.Empty;
    }

    public void ShowSuggestions(IReadOnlyList<JsonSuggestionModel> suggestions)
    {
        _suggestions = suggestions;

        if (suggestions.Count == 0 || !SearchBox.Focused)
        {
            HideSuggestions();

            return;
        }

        SuggestionsList.BeginUpdate();
        SuggestionsList.Items.Clear();
        SuggestionsList.Items.AddRange([.. suggestions.Select(suggestion => (object)suggestion.Text)]);
        SuggestionsList.SelectedIndex = 0;
        SuggestionsList.EndUpdate();

        Point below = PointToClient(SearchBox.PointToScreen(new Point(0, SearchBox.Height)));
        int rows = Math.Min(suggestions.Count, MaxSuggestionsShown);
        SuggestionsList.Bounds = new Rectangle(
            below.X,
            below.Y,
            Math.Max(SearchBox.Width, DpiFollower.Scaled(360, DeviceDpi)),
            rows * SuggestionsList.ItemHeight + 4);
        SuggestionsList.Visible = true;
        SuggestionsList.BringToFront();
    }

    public void SetSearchText(string text)
    {
        SearchBox.Text = text;
        SearchBox.SelectionStart = text.Length;
        SearchBox.Focus();
    }

    public string? AskOpenPath()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open JSON",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public string? AskSavePath(string fileName)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Save JSON",
            FileName = fileName,
            DefaultExt = ".json",
            AddExtension = true,
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public bool? AskSaveChanges()
    {
        DialogResult answer = MessageBox.Show(
            this,
            "Save the changes first?",
            "JSON editor",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        return answer switch
        {
            DialogResult.Yes => true,
            DialogResult.No => false,
            _ => null,
        };
    }

    public void ShowProblem(string message) =>
        MessageBox.Show(this, message, "JSON editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.N:
                NewRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.O:
                OpenRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.S:
                SaveRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.Shift | Keys.S:
                SaveAsRequested?.Invoke(this, EventArgs.Empty);
                return true;
            case Keys.Control | Keys.F:
                FocusSearch(JsonSearchMode.Text);
                return true;
            case Keys.Control | Keys.J:
                FocusSearch(JsonSearchMode.JmesPath);
                return true;
            case Keys.F3:
                SearchStepRequested?.Invoke(this, 1);
                return true;
            case Keys.Shift | Keys.F3:
                SearchStepRequested?.Invoke(this, -1);
                return true;
            case Keys.Shift | Keys.Alt | Keys.F:
                FormatRequested?.Invoke(this, EventArgs.Empty);
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    private void FocusSearch(JsonSearchMode mode)
    {
        int index = mode == JsonSearchMode.JmesPath ? 1 : 0;

        if (ModeComboBox.SelectedIndex != index)
        {
            ModeComboBox.SelectedIndex = index;
        }

        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void HideSuggestions() => SuggestionsList.Visible = false;

    private void Choose(int index)
    {
        if (index >= 0 && index < _suggestions.Count)
        {
            JsonSuggestionModel suggestion = _suggestions[index];
            HideSuggestions();
            SuggestionChosen?.Invoke(this, suggestion);
        }
    }

    private void NewButton_Click(object? sender, EventArgs e) => NewRequested?.Invoke(this, EventArgs.Empty);

    private void OpenButton_Click(object? sender, EventArgs e) => OpenRequested?.Invoke(this, EventArgs.Empty);

    private void SaveButton_Click(object? sender, EventArgs e) => SaveRequested?.Invoke(this, EventArgs.Empty);

    private void SaveAsButton_Click(object? sender, EventArgs e) => SaveAsRequested?.Invoke(this, EventArgs.Empty);

    private void FormatButton_Click(object? sender, EventArgs e) => FormatRequested?.Invoke(this, EventArgs.Empty);

    private void MinifyButton_Click(object? sender, EventArgs e) => MinifyRequested?.Invoke(this, EventArgs.Empty);

    private void CollapseAllButton_Click(object? sender, EventArgs e) => Editor.CollapseAll();

    private void ExpandAllButton_Click(object? sender, EventArgs e) => Editor.ExpandAll();

    private void PreviousButton_Click(object? sender, EventArgs e) => SearchStepRequested?.Invoke(this, -1);

    private void NextButton_Click(object? sender, EventArgs e) => SearchStepRequested?.Invoke(this, 1);

    private void ModeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isSettingSearch)
        {
            SearchChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SearchBox_TextChanged(object? sender, EventArgs e) => SearchChanged?.Invoke(this, EventArgs.Empty);

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        bool listed = SuggestionsList.Visible && SuggestionsList.Items.Count > 0;

        switch (e.KeyCode)
        {
            case Keys.Down when listed:
                SuggestionsList.SelectedIndex =
                    Math.Min(SuggestionsList.SelectedIndex + 1, SuggestionsList.Items.Count - 1);
                break;
            case Keys.Up when listed:
                SuggestionsList.SelectedIndex = Math.Max(SuggestionsList.SelectedIndex - 1, 0);
                break;
            case Keys.Tab when listed && !e.Shift:
                Choose(SuggestionsList.SelectedIndex);
                break;
            case Keys.Enter when listed && SearchMode == JsonSearchMode.JmesPath:
                Choose(SuggestionsList.SelectedIndex);
                break;
            case Keys.Enter:
                HideSuggestions();
                SearchStepRequested?.Invoke(this, e.Shift ? -1 : 1);
                break;
            case Keys.Escape when listed:
                HideSuggestions();
                break;
            case Keys.Escape:
                Editor.Focus();
                break;
            default:
                return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void SearchBox_Leave(object? sender, EventArgs e)
    {
        // After the focus has gone where it goes: to the list for a click on it, which then chooses
        BeginInvoke(() =>
        {
            if (!SuggestionsList.Focused)
            {
                HideSuggestions();
            }
        });
    }

    private void SuggestionsList_MouseClick(object? sender, MouseEventArgs e) =>
        Choose(SuggestionsList.IndexFromPoint(e.Location));

    private void SuggestionsList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _suggestions.Count)
        {
            return;
        }

        e.DrawBackground();
        JsonSuggestionModel suggestion = _suggestions[e.Index];
        bool selected = (e.State & DrawItemState.Selected) != 0;
        Color text = selected ? SystemColors.HighlightText : SystemColors.ControlText;
        Color detail = selected ? SystemColors.HighlightText : SystemColors.GrayText;
        int padding = DpiFollower.Scaled(4, DeviceDpi);
        Rectangle bounds = Rectangle.Inflate(e.Bounds, -padding, 0);
        const TextFormatFlags Flags =
            TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
        Font font = SuggestionsList.Font;

        TextRenderer.DrawText(e.Graphics, suggestion.Text, font, bounds, text, Flags);
        TextRenderer.DrawText(e.Graphics, suggestion.Detail, font, bounds, detail, Flags | TextFormatFlags.Right);
    }

    private void Editor_TextEdited(object? sender, EventArgs e) => TextEdited?.Invoke(this, EventArgs.Empty);

    private void Editor_CaretMoved(object? sender, DiffPosition caret) => CaretMoved?.Invoke(this, caret);

    private void Editor_PathRequested(object? sender, JsonPathRequestModel request) =>
        PathRequested?.Invoke(this, request);

    private void JsonEditorForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            var closing = new CancelEventArgs();
            CloseRequested?.Invoke(this, closing);
            e.Cancel = closing.Cancel;
        }
    }

    private void JsonEditorForm_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void JsonEditorForm_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths)
        {
            FileDropped?.Invoke(this, paths[0]);
        }
    }
}
