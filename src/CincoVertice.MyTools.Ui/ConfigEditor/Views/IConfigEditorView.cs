using System.ComponentModel;
using CincoVertice.Common.Utils.Highlighting;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;

namespace CincoVertice.MyTools.Ui.ConfigEditor.Views;

/// <summary>
///     Shows a list of files and the text of one of them, with its colors and problems.
/// </summary>
public interface IConfigEditorView
{
    event EventHandler<EditableFileModel>? FileSelected;

    /// <summary>Raised a moment after the user stops typing, so a long file isn't checked on every key.</summary>
    event EventHandler? TextEdited;

    event EventHandler? SaveRequested;

    event EventHandler? NewScriptRequested;

    /// <summary>Raised with the index of the problem the user picked.</summary>
    event EventHandler<int>? ProblemSelected;

    /// <summary>Raised when the window comes to the front, when the file may have been changed elsewhere.</summary>
    event EventHandler? ViewActivated;

    /// <summary>Raised when the user closes the window; cancel it to keep it open.</summary>
    event EventHandler<CancelEventArgs>? CloseRequested;

    /// <summary>The text being edited, with \n for every new line.</summary>
    string CodeText { get; }

    void ShowFiles(IReadOnlyList<EditableFileModel> files, EditableFileModel? selected);

    void ShowCode(string text);

    void ShowHighlights(IReadOnlyList<CodeSpan> spans);

    void ShowProblems(IReadOnlyList<EditableFileProblemModel> problems);

    void ShowTitle(string title);

    /// <summary>Whether the text can be saved: there's no file open when one couldn't be read.</summary>
    void ShowCanSave(bool canSave);

    void ShowError(string message);

    void GoToLine(int line);

    /// <returns>What to do with the changes to the named file.</returns>
    ConfigEditorSaveChoice AskSaveChanges(string name);

    /// <returns>Whether to replace a file that was changed elsewhere since it was opened.</returns>
    bool AskOverwrite(string name);

    /// <returns>The new script's name, or null when the user cancels.</returns>
    string? AskNewScriptName();
}
