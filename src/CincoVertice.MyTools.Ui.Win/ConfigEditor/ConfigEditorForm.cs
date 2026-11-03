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
    // The list and the text are being set from the presenter, not by the user
    private bool _isShowing;

    public ConfigEditorForm()
    {
        InitializeComponent();
    }

    public event EventHandler<EditableFileModel>? FileSelected;

    public event EventHandler? TextEdited;

    public event EventHandler? SaveRequested;

    public event EventHandler? NewScriptRequested;

    public event EventHandler<int>? ProblemSelected;

    public event EventHandler? ViewActivated;

    public event EventHandler<CancelEventArgs>? CloseRequested;

    public string CodeText => CodeEditor.Text;

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
        if (keyData == (Keys.Control | Keys.S))
        {
            if (SaveButton.Enabled)
            {
                SaveRequested?.Invoke(this, EventArgs.Empty);
            }

            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
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
    }

    private void EditedTimer_Tick(object? sender, EventArgs e)
    {
        EditedTimer.Stop();
        TextEdited?.Invoke(this, EventArgs.Empty);
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
