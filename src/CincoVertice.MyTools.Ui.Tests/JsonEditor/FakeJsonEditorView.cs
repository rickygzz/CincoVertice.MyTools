using System.ComponentModel;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Views;

namespace CincoVertice.MyTools.Ui.Tests.JsonEditor;

internal sealed class FakeJsonEditorView : IJsonEditorView
{
    private readonly List<List<string>> _tabs = [[string.Empty]];

    private int _shown;

    private List<string> _lines
    {
        get => _tabs[_shown];
        set => _tabs[_shown] = value;
    }

    public event EventHandler<int>? TabChosen;

    public event EventHandler<int>? TabCloseRequested;

    public List<string> TabTitles { get; } = [string.Empty];

    public int ShownTab => _shown;

    public IReadOnlyList<string> LinesOf(int tab) => _tabs[tab];

    public void AddTab(string title)
    {
        _tabs.Add([string.Empty]);
        TabTitles.Add(title);
        _shown = _tabs.Count - 1;
    }

    public void SelectTab(int index) => _shown = index;

    public void RemoveTab(int index)
    {
        _tabs.RemoveAt(index);
        TabTitles.RemoveAt(index);
        _shown = Math.Min(_shown, _tabs.Count - 1);
    }

    public void ShowTabTitle(int index, string title) => TabTitles[index] = title;

    public void RaiseTabChosen(int index)
    {
        _shown = index;
        TabChosen?.Invoke(this, index);
    }

    public void RaiseTabClose(int index) => TabCloseRequested?.Invoke(this, index);

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

    public event EventHandler? ThemeToggled;

    public IReadOnlyList<string> Lines => _lines;

    public bool IsDark { get; private set; }

    public List<bool> ThemesShown { get; } = [];

    public string SearchText { get; private set; } = string.Empty;

    public JsonSearchMode SearchMode { get; private set; }

    public JsonDocumentModel? Document { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Position { get; private set; } = string.Empty;

    public string Path { get; private set; } = string.Empty;

    public string Validity { get; private set; } = string.Empty;

    public IReadOnlyList<JsonMatchModel> Matches { get; private set; } = [];

    public JsonMatchModel? WentTo { get; private set; }

    public string SearchMessage { get; private set; } = string.Empty;

    public bool IsSearchProblem { get; private set; }

    public string? Result { get; private set; }

    public IReadOnlyList<JsonSuggestionModel> Suggestions { get; private set; } = [];

    public bool? SaveAnswer { get; set; }

    public int SaveAsked { get; private set; }

    public void ShowText(IReadOnlyList<string> lines) => _lines = [.. lines];

    public void ReplaceText(string text)
    {
        _lines = [.. text.Split('\n')];
        TextEdited?.Invoke(this, EventArgs.Empty);
    }

    public void ShowDocument(JsonDocumentModel document) => Document = document;

    public void ShowTitle(string title) => Title = title;

    public void ShowTheme(bool isDark)
    {
        IsDark = isDark;
        ThemesShown.Add(isDark);
    }

    public void ShowStatus(string position, string path, string validity) =>
        (Position, Path, Validity) = (position, path, validity);

    public void ShowMatches(IReadOnlyList<JsonMatchModel> matches, int current) => Matches = matches;

    public void GoTo(JsonMatchModel match) => WentTo = match;

    public void ShowSearchMessage(string message, bool isProblem) =>
        (SearchMessage, IsSearchProblem) = (message, isProblem);

    public void ShowResult(string? json) => Result = json;

    public void ShowSuggestions(IReadOnlyList<JsonSuggestionModel> suggestions) => Suggestions = suggestions;

    public void SetSearchText(string text)
    {
        SearchText = text;
        SearchChanged?.Invoke(this, EventArgs.Empty);
    }

    public string? OpenPath { get; set; }

    public string? SavePath { get; set; }

    public string? AskOpenPath() => OpenPath;

    public string? AskSavePath(string fileName) => SavePath;

    public bool? AskSaveChanges()
    {
        SaveAsked++;

        return SaveAnswer;
    }

    public void ShowProblem(string message)
    {
        if (!CollectsProblems)
        {
            throw new InvalidOperationException(message);
        }

        Problems.Add(message);
    }

    public void Edit(Action<List<string>> change)
    {
        change(_lines);
        TextEdited?.Invoke(this, EventArgs.Empty);
    }

    public void MoveCaret(DiffPosition caret) => CaretMoved?.Invoke(this, caret);

    public void Search(JsonSearchMode mode, string text)
    {
        SearchMode = mode;
        SetSearchText(text);
    }

    public void RaiseStep(int step) => SearchStepRequested?.Invoke(this, step);

    public void RaisePathRequested(JsonPathRequestModel request) => PathRequested?.Invoke(this, request);

    public void RaiseSuggestionChosen(JsonSuggestionModel suggestion) => SuggestionChosen?.Invoke(this, suggestion);

    public void RaiseFormat() => FormatRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseMinify() => MinifyRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseSave() => SaveRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseClose(CancelEventArgs e) => CloseRequested?.Invoke(this, e);

    public void RaiseDrop(string path) => FileDropped?.Invoke(this, path);

    public void RaiseNew() => NewRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseOpen() => OpenRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseSaveAs() => SaveAsRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseThemeToggled() => ThemeToggled?.Invoke(this, EventArgs.Empty);

    public event EventHandler? TreeToggled;

    public event EventHandler<JsonNodeModel>? NodeChosen;

    public event EventHandler<JsonHoverRequestModel>? HoverRequested;

    public event EventHandler<JsonKeyRequestModel>? KeysRequested;

    public event EventHandler<bool>? SortKeysRequested;

    public event EventHandler<JsonCompareSource>? CompareRequested;

    public event EventHandler? GoToPathRequested;

    public event EventHandler<bool>? CopyResultRequested;

    public event EventHandler? RepairRequested;

    public event EventHandler<JsonConversionKind>? ConvertRequested;

    public event EventHandler<bool>? SchemaRequested;

    public event EventHandler<int>? ProblemStepRequested;

    /// <summary>ShowProblem keeps the message here, instead of failing the test.</summary>
    public bool CollectsProblems { get; set; }

    public List<string> Problems { get; } = [];

    public List<string> Messages { get; } = [];

    public bool IsTreeShown { get; private set; }

    public JsonNodeModel? Tree { get; private set; }

    public JsonNodeModel? SelectedInTree { get; private set; }

    public IReadOnlyList<JsonCrumbModel> Crumbs { get; private set; } = [];

    public (DiffPosition Open, DiffPosition Close)? BracketPair { get; private set; }

    public IReadOnlyList<JsonSchemaProblemModel> SchemaProblems { get; private set; } = [];

    public string? SchemaStatus { get; private set; }

    public (string LeftName, string LeftText, string RightName, string RightText)? Comparison { get; private set; }

    public string? PathAnswer { get; set; }

    public string? PathAsked { get; private set; }

    public string? SchemaPath { get; set; }

    public string? CsvPath { get; set; }

    public string? ExportPath { get; set; }

    public string? Clipboard { get; set; }

    public void ShowMessage(string message) => Messages.Add(message);

    public void ReplaceRange(DiffPosition start, DiffPosition end, string text)
    {
        string joined = _lines[start.Line][..start.Column] + text + _lines[end.Line][end.Column..];
        _lines.RemoveRange(start.Line, end.Line - start.Line + 1);
        _lines.InsertRange(start.Line, joined.Split('\n'));
        RaiseTextEdited();
    }

    public void ShowTreePanel(bool isShown) => IsTreeShown = isShown;

    public void ShowTree(JsonNodeModel? root) => Tree = root;

    public void SelectInTree(JsonNodeModel? node) => SelectedInTree = node;

    public void ShowCrumbs(IReadOnlyList<JsonCrumbModel> crumbs) => Crumbs = crumbs;

    public void ShowBracketPair((DiffPosition Open, DiffPosition Close)? pair) => BracketPair = pair;

    public void ShowSchemaProblems(IReadOnlyList<JsonSchemaProblemModel> problems) => SchemaProblems = problems;

    public void ShowSchemaStatus(string? status) => SchemaStatus = status;

    public void ShowComparison(string leftName, string leftText, string rightName, string rightText) =>
        Comparison = (leftName, leftText, rightName, rightText);

    public string? AskPath(string current)
    {
        PathAsked = current;

        return PathAnswer;
    }

    public string? AskSchemaPath() => SchemaPath;

    public string? AskCsvPath() => CsvPath;

    public string? AskExportPath(string fileName) => ExportPath;

    public string? ReadClipboard() => Clipboard;

    public void WriteClipboard(string text) => Clipboard = text;

    public void RaiseTextEdited() => TextEdited?.Invoke(this, EventArgs.Empty);

    public void RaiseTreeToggled() => TreeToggled?.Invoke(this, EventArgs.Empty);

    public void RaiseNodeChosen(JsonNodeModel node) => NodeChosen?.Invoke(this, node);

    public void RaiseHover(JsonHoverRequestModel request) => HoverRequested?.Invoke(this, request);

    public void RaiseKeys(JsonKeyRequestModel request) => KeysRequested?.Invoke(this, request);

    public void RaiseSortKeys(bool here) => SortKeysRequested?.Invoke(this, here);

    public void RaiseCompare(JsonCompareSource source) => CompareRequested?.Invoke(this, source);

    public void RaiseGoToPath() => GoToPathRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseCopyResult(bool asLines) => CopyResultRequested?.Invoke(this, asLines);

    public void RaiseRepair() => RepairRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseConvert(JsonConversionKind kind) => ConvertRequested?.Invoke(this, kind);

    public void RaiseSchema(bool choose) => SchemaRequested?.Invoke(this, choose);

    public void RaiseProblemStep(int step) => ProblemStepRequested?.Invoke(this, step);
}
