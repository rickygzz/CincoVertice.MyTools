using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.Diff.Views;

/// <summary>
///     A window comparing two files, or pasted texts, side by side. It only shows what
///     <see cref="Presenters.DiffPresenter"/> gives it, and tells it what the user did.
/// </summary>
public interface IDiffView
{
    /// <summary>Files chosen with a button, or dropped on the window.</summary>
    event EventHandler<DiffFilesModel>? FilesChosen;

    /// <summary>The clipboard's text, pasted as one side.</summary>
    event EventHandler<DiffPasteModel>? TextPasted;

    event EventHandler? SwapRequested;

    event EventHandler? ReloadRequested;

    /// <summary>The window became the active one: a file may have changed meanwhile.</summary>
    event EventHandler? WindowActivated;

    event EventHandler<DiffOptionsModel>? OptionsChanged;

    /// <summary>With the row the view is at: the one the user clicked, or else the first one showing.</summary>
    event EventHandler<int>? NextChangeRequested;

    /// <inheritdoc cref="NextChangeRequested"/>
    event EventHandler<int>? PreviousChangeRequested;

    void ShowTitle(string title);

    /// <summary>What each side shows: a file's path, or that it's pasted text, or that it's still to choose.</summary>
    void ShowSides(string left, string right);

    /// <param name="keepPlace">Whether to stay at the rows showing, as after a reload, rather than at the top.</param>
    void ShowDiff(DiffResultModel result, bool keepPlace);

    /// <summary>Brings a change into view, marked as the current one; null for none.</summary>
    void ShowChange(DiffHunkModel? change);

    /// <param name="summary">What differs, in all.</param>
    /// <param name="position">Which change is the current one, such as "Change 2 of 5"; empty for none.</param>
    void ShowStatus(string summary, string position);

    void ShowCanNavigate(bool canNavigate);

    void ShowProblem(string message);
}
