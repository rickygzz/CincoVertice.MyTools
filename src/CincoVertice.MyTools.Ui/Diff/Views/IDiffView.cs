using System.ComponentModel;
using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.Diff.Views;

/// <summary>
///     A window comparing two files, or pasted texts, side by side, both editable. It owns the text being edited, as a
///     text box does, and tells <see cref="Presenters.DiffPresenter"/> of each change; it shows what the presenter
///     gives it.
/// </summary>
public interface IDiffView
{
    /// <summary>Files chosen with a button, or dropped on the window.</summary>
    event EventHandler<DiffFilesModel>? FilesChosen;

    /// <summary>The clipboard's text, pasted as one side.</summary>
    event EventHandler<DiffPasteModel>? TextPasted;

    /// <summary>The user changed a side's text, by typing, pasting, undoing...</summary>
    event EventHandler<DiffEditModel>? TextEdited;

    /// <summary>Save the side the user is editing.</summary>
    event EventHandler<DiffSide>? SaveRequested;

    event EventHandler? SwapRequested;

    event EventHandler? ReloadRequested;

    /// <summary>The window became the active one: a file may have changed meanwhile.</summary>
    event EventHandler? WindowActivated;

    /// <summary>The user is closing the window; cancel to keep it open.</summary>
    event EventHandler<CancelEventArgs>? CloseRequested;

    event EventHandler<DiffOptionsModel>? OptionsChanged;

    /// <summary>With the row the view is at: the one the caret is on, or else the first one showing.</summary>
    event EventHandler<int>? NextChangeRequested;

    /// <inheritdoc cref="NextChangeRequested"/>
    event EventHandler<int>? PreviousChangeRequested;

    void ShowTitle(string title);

    /// <summary>What each side shows: a file's path, or that it's pasted text, or that it's still to choose.</summary>
    void ShowSides(string left, string right);

    /// <summary>Puts a new text on a side to edit, with nothing to undo.</summary>
    void ShowText(DiffSide side, IReadOnlyList<string> lines);

    /// <param name="keepPlace">Whether to stay at the rows showing, as after an edit, rather than at the top.</param>
    void ShowDiff(DiffResultModel result, bool keepPlace);

    /// <summary>Brings a change into view, marked as the current one; null for none.</summary>
    void ShowChange(DiffHunkModel? change);

    /// <param name="summary">What differs, in all.</param>
    /// <param name="position">Which change is the current one, such as "Change 2 of 5"; empty for none.</param>
    void ShowStatus(string summary, string position);

    void ShowCanNavigate(bool canNavigate);

    void ShowProblem(string message);

    /// <returns>Where to save text that isn't a file yet, or null when the user cancels.</returns>
    string? AskSavePath(DiffSide side);

    /// <param name="names">The files changed and not saved.</param>
    DiffSaveChoice AskSaveChanges(IReadOnlyList<string> names);

    /// <returns>Whether to read again a file that changed on disk, losing the changes made to it here.</returns>
    bool AskReload(string name);
}
