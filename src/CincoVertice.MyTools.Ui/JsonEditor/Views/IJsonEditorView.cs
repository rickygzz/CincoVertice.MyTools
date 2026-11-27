using System.ComponentModel;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Views;

/// <summary>
///     The JSON editor's window: the text, its search box, and what the presenter says about them. It has tabs, each
///     with a text of its own (and its own undo); it starts with one. Everything about the text is about the tab shown.
/// </summary>
public interface IJsonEditorView
{
    /// <summary>The user chose another tab, by its index.</summary>
    event EventHandler<int>? TabChosen;

    /// <summary>The user asked to close a tab, by its index; the presenter asks about its changes first.</summary>
    event EventHandler<int>? TabCloseRequested;

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

    /// <summary>The Dark or Light button: the other of the two, from now on.</summary>
    event EventHandler? ThemeToggled;

    /// <summary>The Tree button (Ctrl+T): the tree panel shown or hidden, from now on.</summary>
    event EventHandler? TreeToggled;

    /// <summary>A value chosen in the tree panel, or a step of the breadcrumb clicked: to go to it.</summary>
    event EventHandler<JsonNodeModel>? NodeChosen;

    /// <summary>The pointer rests on the text: the presenter says what's there.</summary>
    event EventHandler<JsonHoverRequestModel>? HoverRequested;

    /// <summary>Ctrl+Space: the names to offer for a new member there.</summary>
    event EventHandler<JsonKeyRequestModel>? KeysRequested;

    /// <summary>Sort keys: of the object at the caret when true, of the whole document when false.</summary>
    event EventHandler<bool>? SortKeysRequested;

    event EventHandler<JsonCompareSource>? CompareRequested;

    /// <summary>Go to path (Ctrl+G).</summary>
    event EventHandler? GoToPathRequested;

    /// <summary>Copy the result: as plain lines when true, as JSON when false.</summary>
    event EventHandler<bool>? CopyResultRequested;

    event EventHandler? RepairRequested;

    event EventHandler<JsonConversionKind>? ConvertRequested;

    /// <summary>Choose a schema when true; check against none when false.</summary>
    event EventHandler<bool>? SchemaRequested;

    /// <summary>To the next schema problem after the caret (F8), or the one before (Shift+F8, -1).</summary>
    event EventHandler<int>? ProblemStepRequested;

    /// <summary>The text of the tab shown.</summary>
    IReadOnlyList<string> Lines { get; }

    /// <summary>The text of any tab.</summary>
    IReadOnlyList<string> LinesOf(int tab);

    /// <summary>A tab after the others, with an empty text, shown. Raises no <see cref="TabChosen"/>.</summary>
    void AddTab(string title);

    /// <summary>Shows a tab. Raises no <see cref="TabChosen"/>.</summary>
    void SelectTab(int index);

    /// <summary>Removes a tab and its text; the presenter then says which to show.</summary>
    void RemoveTab(int index);

    void ShowTabTitle(int index, string title);

    /// <summary>Whether it's shown dark: as Windows' mode when it opens, until a theme is chosen.</summary>
    bool IsDark { get; }

    string SearchText { get; }

    JsonSearchMode SearchMode { get; }

    /// <summary>Shows a new text, with no undo before it.</summary>
    void ShowText(IReadOnlyList<string> lines);

    /// <summary>Replaces the whole text as an edit, which Undo takes back.</summary>
    void ReplaceText(string text);

    /// <summary>The text's colors and sections.</summary>
    void ShowDocument(JsonDocumentModel document);

    void ShowTitle(string title);

    void ShowTheme(bool isDark);

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

    void ShowMessage(string message);

    /// <summary>Replaces part of the text as one edit, which Undo takes back.</summary>
    void ReplaceRange(DiffPosition start, DiffPosition end, string text);

    /// <summary>The tree panel shown or hidden.</summary>
    void ShowTreePanel(bool isShown);

    /// <summary>The document's values in the tree panel; null for none.</summary>
    void ShowTree(JsonNodeModel? root);

    /// <summary>The value at the caret selected in the tree panel, its sections opened down to it.</summary>
    void SelectInTree(JsonNodeModel? node);

    /// <summary>The steps to the value at the caret, in the status bar.</summary>
    void ShowCrumbs(IReadOnlyList<JsonCrumbModel> crumbs);

    /// <summary>The bracket at the caret and its pair, marked; null for none.</summary>
    void ShowBracketPair((DiffPosition Open, DiffPosition Close)? pair);

    /// <summary>The values the schema finds wrong, underlined.</summary>
    void ShowSchemaProblems(IReadOnlyList<JsonSchemaProblemModel> problems);

    /// <summary>The schema's name and how many problems, in the status bar; null for no schema.</summary>
    void ShowSchemaStatus(string? status);

    /// <summary>Two texts side by side, in Compare files.</summary>
    void ShowComparison(string leftName, string leftText, string rightName, string rightText);

    /// <returns>The path to go to; null when the user cancels.</returns>
    string? AskPath(string current);

    string? AskSchemaPath();

    string? AskCsvPath();

    /// <returns>Where to save a YAML or CSV file, by its extension; null when the user cancels.</returns>
    string? AskExportPath(string fileName);

    /// <returns>The clipboard's text; null when it has none.</returns>
    string? ReadClipboard();

    void WriteClipboard(string text);
}
