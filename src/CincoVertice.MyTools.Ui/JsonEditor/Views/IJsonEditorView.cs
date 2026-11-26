using System.ComponentModel;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Views;

/// <summary>The JSON editor's window: the text, its search box, and what the presenter says about them.</summary>
public interface IJsonEditorView
{
    /// <summary>The user changed the text; <see cref="Lines"/> has it.</summary>
    event EventHandler? TextEdited;

    event EventHandler<DiffPosition>? CaretMoved;

    event EventHandler? NewRequested;

    event EventHandler? OpenRequested;

    event EventHandler? SaveRequested;

    event EventHandler? SaveAsRequested;

    event EventHandler? FormatRequested;

    event EventHandler? MinifyRequested;

    /// <summary>A file dropped on the window, to open.</summary>
    event EventHandler<string>? FileDropped;

    /// <summary>The search's text or mode changed: <see cref="SearchText"/>, <see cref="SearchMode"/>.</summary>
    event EventHandler? SearchChanged;

    /// <summary>To the next match, 1, or the one before, -1.</summary>
    event EventHandler<int>? SearchStepRequested;

    event EventHandler<JsonSuggestionModel>? SuggestionChosen;

    /// <summary>A right click: the presenter says the path and the value there.</summary>
    event EventHandler<JsonPathRequestModel>? PathRequested;

    /// <summary>The window is closing; cancel to keep it open.</summary>
    event EventHandler<CancelEventArgs>? CloseRequested;

    IReadOnlyList<string> Lines { get; }

    string SearchText { get; }

    JsonSearchMode SearchMode { get; }

    /// <summary>Shows a new text, with no undo before it.</summary>
    void ShowText(IReadOnlyList<string> lines);

    /// <summary>Replaces the whole text as an edit, which Undo takes back.</summary>
    void ReplaceText(string text);

    /// <summary>The text's colors and sections.</summary>
    void ShowDocument(JsonDocumentModel document);

    void ShowTitle(string title);

    /// <summary>Where the caret is, the path of the value there, and whether the JSON is valid.</summary>
    void ShowStatus(string position, string path, string validity);

    /// <summary>The search's matches, the current one apart; -1 for none current.</summary>
    void ShowMatches(IReadOnlyList<JsonMatchModel> matches, int current);

    /// <summary>Brings a match into view and selects it.</summary>
    void GoTo(JsonMatchModel match);

    /// <summary>Beside the search box: how many matches, or what's wrong with the expression.</summary>
    void ShowSearchMessage(string message, bool isProblem);

    /// <summary>A JMESPath result as JSON, under the text; null hides it.</summary>
    void ShowResult(string? json);

    void ShowSuggestions(IReadOnlyList<JsonSuggestionModel> suggestions);

    /// <summary>Writes the search text, as after a suggestion is chosen, the caret at its end.</summary>
    void SetSearchText(string text);

    string? AskOpenPath();

    string? AskSavePath(string fileName);

    /// <summary>Save the changes first? Yes, no, or null to cancel.</summary>
    bool? AskSaveChanges();

    void ShowProblem(string message);
}
