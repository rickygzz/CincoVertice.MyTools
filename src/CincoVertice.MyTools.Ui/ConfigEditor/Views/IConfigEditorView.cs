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

    /// <summary>Raised when the user asks to read the list of files again, as after adding a script elsewhere.</summary>
    event EventHandler? RefreshFilesRequested;

    /// <summary>Raised with the index of the problem the user picked.</summary>
    event EventHandler<int>? ProblemSelected;

    /// <summary>Raised when the window comes to the front, when the file may have been changed elsewhere.</summary>
    event EventHandler? ViewActivated;

    /// <summary>Raised when the user closes the window; cancel it to keep it open.</summary>
    event EventHandler<CancelEventArgs>? CloseRequested;

    /// <summary>Raised as the user types a word, a "." or an "=", starts a line, or presses Ctrl+Space.</summary>
    event EventHandler<SuggestionTrigger>? SuggestionsRequested;

    /// <summary>Raised with the index of the suggestion the user chose.</summary>
    event EventHandler<int>? SuggestionChosen;

    /// <summary>Raised when the pointer rests on a character, for what to show about the name there.</summary>
    event EventHandler<CodeHoverModel>? HoverRequested;

    /// <summary>The text being edited, with \n for every new line.</summary>
    string CodeText { get; }

    /// <summary>Where the caret is in <see cref="CodeText"/>.</summary>
    int CaretPosition { get; }

    void ShowFiles(IReadOnlyList<EditableFileModel> files, EditableFileModel? selected);

    void ShowCode(string text);

    void ShowHighlights(IReadOnlyList<CodeSpan> spans);

    void ShowProblems(IReadOnlyList<EditableFileProblemModel> problems);

    void ShowTitle(string title);

    /// <summary>Whether the text can be saved: there's no file open when one couldn't be read.</summary>
    void ShowCanSave(bool canSave);

    void ShowError(string message);

    void GoToLine(int line);

    /// <summary>Shows the suggestions at the caret.</summary>
    /// <param name="selectFirst">Whether Enter chooses the first; otherwise only one the user moved to.</param>
    void ShowSuggestions(IReadOnlyList<CodeSuggestionModel> suggestions, bool selectFirst);

    void HideSuggestions();

    /// <summary>Writes text in place of a part of the code, as typing would, the caret after it.</summary>
    void ReplaceText(int start, int length, string text);

    /// <returns>What to do with the changes to the named file.</returns>
    ConfigEditorSaveChoice AskSaveChanges(string name);

    /// <returns>Whether to replace a file that was changed elsewhere since it was opened.</returns>
    bool AskOverwrite(string name);

    /// <returns>The new script's name, or null when the user cancels.</returns>
    string? AskNewScriptName();
}
