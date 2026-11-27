using System.Text;
using CincoVertice.MyTools.Ui.Diff.Interfaces;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Interfaces;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Schema;
using CincoVertice.MyTools.Ui.JsonEditor.Services;
using CincoVertice.MyTools.Ui.JsonEditor.Views;

namespace CincoVertice.MyTools.Ui.JsonEditor.Presenters;

/// <summary>
///     The JSON editor: reads the text again at each change (colors, sections, values, the first error), on another
///     thread for a large one; says the path of the value at the caret, at a right click and under the pointer; keeps
///     the tree panel, the breadcrumb and the bracket pair in step with the caret; searches text or JMESPath with
///     suggestions; checks a schema; formats, sorts, repairs, compares and converts; and opens and saves files as they
///     were written (encoding, line breaks). Each document is a tab (<see cref="JsonEditorTab"/>): New, Open, a drop
///     and a CSV opened go to a tab of their own, and all of it is about the tab shown.
/// </summary>
public sealed class JsonEditorPresenter
{
    public const string BaseTitle = "JSON editor";

    /// <summary>A text longer than this, in characters, is read on another thread, so typing never waits.</summary>
    public const int LargeTextLength = 500_000;

    private readonly IJsonEditorView _view;
    private readonly IDiffFileReader _reader;
    private readonly IDiffFileWriter _writer;
    private readonly IJsonEditorSettingsStore? _settings;

    // The tabs, in the order the view shows them, and the one shown; the search and the result are of that one
    private readonly List<JsonEditorTab> _tabs = [new()];
    private JsonEditorTab _tab;
    private IReadOnlyList<JsonMatchModel> _matches = [];
    private JsonNodeModel? _result;
    private int _current = -1;
    private JsonEditorSettingsModel _shownSettings = new(IsDark: null);

    /// <param name="settings">Where dark or light and the tree panel are kept; none keeps them for the window.</param>
    public JsonEditorPresenter(
        IJsonEditorView view,
        IDiffFileReader reader,
        IDiffFileWriter writer,
        IJsonEditorSettingsStore? settings = null)
    {
        _view = view;
        _reader = reader;
        _writer = writer;
        _settings = settings;
        _tab = _tabs[0];

        _view.TabChosen += (_, index) => Activate(index);
        _view.TabCloseRequested += (_, index) => CloseTab(index);
        _view.TextEdited += (_, _) => Changed();
        _view.CaretMoved += (_, caret) => ShowCaret(caret);
        _view.NewRequested += (_, _) => New();
        _view.OpenRequested += (_, _) => OpenAsked();
        _view.SaveRequested += (_, _) => Save();
        _view.SaveAsRequested += (_, _) => SaveAs();
        _view.FormatRequested += (_, _) => Rewrite(indented: true);
        _view.MinifyRequested += (_, _) => Rewrite(indented: false);
        _view.FileDropped += (_, path) => Open(path);
        _view.SearchChanged += (_, _) => Search(suggest: true);
        _view.SearchStepRequested += (_, step) => Step(step);
        _view.SuggestionChosen += (_, suggestion) => Choose(suggestion);
        _view.PathRequested += (_, request) => AnswerPath(request);
        _view.CloseRequested += (_, e) => e.Cancel = !MayDiscardAll();
        _view.ThemeToggled += (_, _) => ToggleTheme();
        _view.TreeToggled += (_, _) => ToggleTree();
        _view.NodeChosen += (_, node) => GoToNode(node);
        _view.HoverRequested += (_, request) => AnswerHover(request);
        _view.KeysRequested += (_, request) => AnswerKeys(request);
        _view.SortKeysRequested += (_, here) => SortKeys(here);
        _view.CompareRequested += (_, source) => Compare(source);
        _view.GoToPathRequested += (_, _) => GoToPath();
        _view.CopyResultRequested += (_, asLines) => CopyResult(asLines);
        _view.RepairRequested += (_, _) => Repair();
        _view.ConvertRequested += (_, kind) => Convert(kind);
        _view.SchemaRequested += (_, choose) => ChooseSchema(choose);
        _view.ProblemStepRequested += (_, step) => StepProblem(step);
    }

    /// <summary>Whether the tab shown has changes not saved.</summary>
    public bool IsModified => !_view.Lines.SequenceEqual(_tab.SavedLines);

    /// <summary>The read of a large text going on in the tab shown, for tests: done once it's shown.</summary>
    public Task Reading => _tab.Reading;

    /// <summary>How many tabs there are, for tests.</summary>
    public int TabCount => _tabs.Count;

    /// <summary>Shows a file in the window's first tab, or an empty text for null.</summary>
    public void Start(string? path)
    {
        _shownSettings = _settings?.Load() ?? _shownSettings;

        if (_shownSettings.IsDark is bool isDark)
        {
            _view.ShowTheme(isDark);
        }

        _view.ShowTreePanel(_shownSettings.IsTreeShown);

        if (path is null || !Load(path))
        {
            Show(null, [string.Empty], DiffTextFormatModel.Default);
        }
    }

    /// <summary>
    ///     Opens a file in a tab: the tab it's open in already, the tab shown when that one is an empty Untitled, or a
    ///     new tab.
    /// </summary>
    public void Open(string path)
    {
        int open = _tabs.FindIndex(tab => tab.Path is string other && SamePath(other, path));

        if (open >= 0)
        {
            Activate(open);
        }
        else if (ReadFile(path) is DiffFileModel file)
        {
            ToFreeTab();
            ShowFile(path, file);
        }
    }

    private static bool SamePath(string one, string other) =>
        string.Equals(Path.GetFullPath(one), Path.GetFullPath(other), StringComparison.OrdinalIgnoreCase);

    private void New()
    {
        AddTab();
        Show(null, [string.Empty], DiffTextFormatModel.Default);
    }

    private void OpenAsked()
    {
        if (_view.AskOpenPath() is string path)
        {
            Open(path);
        }
    }

    // The tab shown when it's an empty Untitled with nothing to lose, or else a new one
    private void ToFreeTab()
    {
        bool isFree = _tab.Path is null && !IsModified && _view.Lines is [""];

        if (!isFree)
        {
            AddTab();
        }
    }

    private void AddTab()
    {
        _tab = new JsonEditorTab();
        _tabs.Add(_tab);
        _view.AddTab(_tab.Name);
    }

    /// <summary>Shows a tab and what's known of its document.</summary>
    private void Activate(int index)
    {
        if (index < 0 || index >= _tabs.Count)
        {
            return;
        }

        _tab = _tabs[index];
        _view.SelectTab(index);
        ShowTitle();

        // A large text left while it was read, or before: read again now that it shows
        if (_tab.IsStale)
        {
            ReadInBackground();
            ShowCaret(_tab.Caret);
            Search(suggest: false);
        }
        else
        {
            ShowRead();
        }
    }

    // Asks about its changes first; the last tab closed leaves an empty Untitled, so the window keeps a tab
    private void CloseTab(int index)
    {
        if (index < 0 || index >= _tabs.Count)
        {
            return;
        }

        Activate(index);

        if (!MayDiscard())
        {
            return;
        }

        if (_tabs.Count == 1)
        {
            New();
        }

        _tabs.RemoveAt(index);
        _view.RemoveTab(index);
        Activate(Math.Min(index, _tabs.Count - 1));
    }

    // Each tab with changes, shown in turn to ask about it; one cancelled keeps the window open
    private bool MayDiscardAll()
    {
        int shown = _tabs.IndexOf(_tab);

        for (int i = 0; i < _tabs.Count; i++)
        {
            int index = (shown + i) % _tabs.Count;

            if (!_view.LinesOf(index).SequenceEqual(_tabs[index].SavedLines))
            {
                Activate(index);

                if (!MayDiscard())
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool Load(string path)
    {
        if (ReadFile(path) is not DiffFileModel file)
        {
            return false;
        }

        ShowFile(path, file);

        return true;
    }

    private void ShowFile(string path, DiffFileModel file)
    {
        DiffTextFormatModel format = DiffTextFormatModel.Of(file.Text, file.Encoding);
        List<string> lines = LinesOf(file.Text);

        if (format.EndsWithLineBreak && lines.Count > 1)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        Show(path, lines, format);
    }

    private DiffFileModel? ReadFile(string path)
    {
        try
        {
            return _reader.Read(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowProblem($"Could not open {path}: {ex.Message}");

            return null;
        }
    }

    private static List<string> LinesOf(string text) => [.. text.ReplaceLineEndings("\n").Split('\n')];

    private void Show(string? path, IReadOnlyList<string> lines, DiffTextFormatModel format)
    {
        _tab.Path = path;
        _tab.Format = format;
        _tab.SavedLines = [.. lines];
        _view.ShowText(lines);
        Changed();
    }

    private bool Save() => _tab.Path is null ? SaveAs() : SaveTo(_tab.Path);

    private bool SaveAs() =>
        _view.AskSavePath(_tab.Path is null ? "document.json" : _tab.Name) is string path && SaveTo(path);

    private bool SaveTo(string path)
    {
        DiffTextFormatModel format = _tab.Format;
        string text = format.Join(_view.Lines) + (format.EndsWithLineBreak ? format.LineBreak : string.Empty);

        try
        {
            _writer.Write(path, text, _tab.Format.Encoding);
            _tab.Path = path;
            _tab.SavedLines = [.. _view.Lines];
            ShowTitle();

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowProblem($"Could not save {path}: {ex.Message}");

            return false;
        }
    }

    // Changes not saved: save them, drop them, or stay
    private bool MayDiscard()
    {
        if (!IsModified)
        {
            return true;
        }

        return _view.AskSaveChanges() switch
        {
            true => Save(),
            false => true,
            _ => false,
        };
    }

    private void Rewrite(bool indented)
    {
        if (ValidRoot() is not JsonNodeModel root)
        {
            return;
        }

        string text = JsonTextWriter.Write(root, indented);

        if (string.Join("\n", _view.Lines) != text)
        {
            _view.ReplaceText(text);
        }
    }

    // The document's value when the JSON is valid; says why not otherwise
    private JsonNodeModel? ValidRoot()
    {
        EnsureCurrent();

        if (_tab.Document.Root is null)
        {
            _view.ShowProblem("There's no JSON here.");

            return null;
        }

        if (_tab.Document.Error is string error)
        {
            _view.ShowProblem($"The JSON isn't valid: {Where(_tab.Document.ErrorAt)}, {error}.");

            return null;
        }

        return _tab.Document.Root;
    }

    private void Changed()
    {
        ShowTitle();

        if (_view.Lines.Sum(line => line.Length + 1) > LargeTextLength)
        {
            _tab.IsStale = true;
            ReadInBackground();
            ShowCaret(_tab.Caret);
        }
        else
        {
            Apply(Analyze(_view.Lines));
        }
    }

    private void ReadInBackground()
    {
        if (_tab.IsReading)
        {
            _tab.ReadAgain = true;

            return;
        }

        _tab.IsReading = true;
        _tab.Reading = ReadUntilCurrentAsync(_tab);
    }

    // Back on the window's thread after each read; a read the text has gone past is read again, not shown. The
    // view's text is the tab shown: once another tab is, this one stays stale and is read when it shows again
    private async Task ReadUntilCurrentAsync(JsonEditorTab tab)
    {
        try
        {
            do
            {
                tab.ReadAgain = false;

                if (tab != _tab)
                {
                    return;
                }

                string[] lines = [.. _view.Lines];
                JsonSchemaValidator? validator = tab.Validator;
                (JsonDocumentModel, IReadOnlyList<JsonSchemaProblemModel>) read =
                    await Task.Run(() => Analyze(lines, validator));

                if (!tab.ReadAgain && tab.IsStale && tab == _tab)
                {
                    Apply(read);
                }
            }
            while (tab.ReadAgain);
        }
        finally
        {
            tab.IsReading = false;
        }
    }

    // Before what needs the document as the text is now: a large one is read here and now
    private void EnsureCurrent()
    {
        if (_tab.IsStale)
        {
            Apply(Analyze(_view.Lines));
        }
    }

    private (JsonDocumentModel Document, IReadOnlyList<JsonSchemaProblemModel> Problems) Analyze(
        IReadOnlyList<string> lines) =>
        Analyze(lines, _tab.Validator);

    private static (JsonDocumentModel Document, IReadOnlyList<JsonSchemaProblemModel> Problems) Analyze(
        IReadOnlyList<string> lines,
        JsonSchemaValidator? validator)
    {
        JsonDocumentModel document = JsonDocumentReader.Read(lines);

        // A broken document's values stop at the error, so what's missing after it isn't missing
        IReadOnlyList<JsonSchemaProblemModel> problems = validator is not null && document.Root is not null
            && document.Error is null
                ? validator.Validate(document.Root)
                : [];

        return (document, problems);
    }

    private void Apply((JsonDocumentModel Document, IReadOnlyList<JsonSchemaProblemModel> Problems) read)
    {
        _tab.IsStale = false;
        _tab.Document = read.Document;

        // The document may name another schema now
        if (UpdateSchema())
        {
            read = Analyze(_view.Lines);
            _tab.Document = read.Document;
        }

        _tab.Problems = read.Problems;
        ShowRead();
    }

    // The tab's document as last read: its colors, schema problems, tree, caret and the search in it
    private void ShowRead()
    {
        _view.ShowDocument(_tab.Document);
        _view.ShowSchemaProblems(_tab.Problems);
        ShowSchemaStatus();

        if (_shownSettings.IsTreeShown)
        {
            _view.ShowTree(_tab.Document.Root);
        }

        ShowCaret(_tab.Caret);
        Search(suggest: false);
    }

    private void ShowTitle()
    {
        string name = _tab.Name + (IsModified ? " *" : string.Empty);
        _view.ShowTitle($"{name} - {BaseTitle}");
        _view.ShowTabTitle(_tabs.IndexOf(_tab), name);
    }

    private void ShowCaret(DiffPosition caret)
    {
        _tab.Caret = caret;
        JsonNodeModel? node = _tab.IsStale ? null : JsonPathWriter.NodeAt(_tab.Document.Root, caret);
        string validity = _tab.IsStale ? "Reading…"
            : _tab.Document.Root is null && _tab.Document.Error is null ? "Empty"
            : _tab.Document.Error is string error ? $"L{Where(_tab.Document.ErrorAt)[1..]}: {error}"
            : "Valid JSON";

        _view.ShowStatus(
            $"Ln {caret.Line + 1}, Col {caret.Column + 1}",
            node is null ? string.Empty : JsonPathWriter.PathOf(node),
            validity);
        _view.ShowCrumbs(node is null ? [] : JsonPathWriter.CrumbsOf(node));
        _view.ShowBracketPair(_tab.IsStale ? null : JsonBrackets.PairAt(_tab.Document.Tokens, _view.Lines, caret));

        if (_shownSettings.IsTreeShown)
        {
            _view.SelectInTree(node);
        }
    }

    private void ToggleTheme()
    {
        bool isDark = !_view.IsDark;
        _view.ShowTheme(isDark);
        SaveSettings(_shownSettings with { IsDark = isDark });
    }

    private void ToggleTree()
    {
        SaveSettings(_shownSettings with { IsTreeShown = !_shownSettings.IsTreeShown });
        _view.ShowTreePanel(_shownSettings.IsTreeShown);

        if (_shownSettings.IsTreeShown)
        {
            EnsureCurrent();
            _view.ShowTree(_tab.Document.Root);
            ShowCaret(_tab.Caret);
        }
    }

    private void SaveSettings(JsonEditorSettingsModel settings)
    {
        _shownSettings = settings;
        _settings?.Save(settings);
    }

    private void GoToNode(JsonNodeModel node)
    {
        if (node.Start is DiffPosition start && node.End is DiffPosition end)
        {
            _view.GoTo(new JsonMatchModel(start, end));
        }
    }

    // The values the selection holds, or the one clicked when it holds none
    private void AnswerPath(JsonPathRequestModel request)
    {
        EnsureCurrent();
        JsonNodeModel? clicked = JsonPathWriter.NodeAt(_tab.Document.Root, request.Position);
        IReadOnlyList<JsonNodeModel> values = request.Selection is var (start, end)
            ? JsonPathWriter.NodesIn(_tab.Document.Root, start, end)
            : [];

        if (values.Count == 0 && clicked is not null)
        {
            values = [clicked];
        }

        List<JsonNodeModel> named = [.. values.Where(value => value.Key is not null)];
        string separator = "," + Environment.NewLine;

        request.Path = clicked is null ? null : JsonPathWriter.PathOf(clicked);
        request.ValueCount = values.Count;
        request.Value = values.Count == 0
            ? null
            : string.Join(separator, values.Select(value => JsonTextWriter.Write(value, indented: false)));
        request.NameAndValueCount = named.Count;
        request.NameAndValue = named.Count == 0 ? null : string.Join(separator, named.Select(NameAndValueOf));
    }

    private static string NameAndValueOf(JsonNodeModel value) =>
        $"{JsonTextWriter.Quoted(value.Key!)}: {JsonTextWriter.Write(value, indented: false)}";

    // What's under the pointer: its path, what it is, and what the schema finds wrong with it
    private void AnswerHover(JsonHoverRequestModel request)
    {
        if (_tab.IsStale || JsonPathWriter.NodeAt(_tab.Document.Root, request.Position) is not JsonNodeModel node)
        {
            return;
        }

        var text = new StringBuilder();
        text.Append(JsonPathWriter.PathOf(node)).Append('\n').Append(JsonPathWriter.KindOf(node));

        foreach (JsonSchemaProblemModel problem in _tab.Problems.Where(
            problem => problem.Start <= request.Position && request.Position < problem.End))
        {
            text.Append('\n').Append(problem.Message);
        }

        request.Text = text.ToString();
    }

    private void AnswerKeys(JsonKeyRequestModel request)
    {
        EnsureCurrent();

        if (_tab.Document.Root is JsonNodeModel root
            && JsonKeySuggester.ObjectAt(root, request.Position) is JsonNodeModel target)
        {
            request.Names = JsonKeySuggester.NamesFor(target, root, _tab.Validator);
        }
    }

    private void SortKeys(bool here)
    {
        if (!here)
        {
            if (ValidRoot() is JsonNodeModel root)
            {
                string all = JsonTextWriter.Write(JsonTransforms.SortedKeys(root), indented: true);
                ReplaceIfChanged(string.Join("\n", _view.Lines), all);
            }

            return;
        }

        EnsureCurrent();

        if (JsonKeySuggester.ObjectAt(_tab.Document.Root, _tab.Caret) is not
            { Start: DiffPosition start, End: DiffPosition end } target)
        {
            _view.ShowProblem("The caret isn't in an object to sort.");

            return;
        }

        string line = _view.Lines[start.Line];
        string indentation = line[..(line.Length - line.TrimStart().Length)];
        string sorted = JsonTransforms.Indented(
            JsonTextWriter.Write(JsonTransforms.SortedKeys(target), indented: true),
            indentation);

        if (TextBetween(start, end) != sorted)
        {
            _view.ReplaceRange(start, end, sorted);
        }
    }

    private void ReplaceIfChanged(string now, string text)
    {
        if (now != text)
        {
            _view.ReplaceText(text);
        }
    }

    private string TextBetween(DiffPosition start, DiffPosition end)
    {
        IReadOnlyList<string> lines = _view.Lines;

        if (start.Line == end.Line)
        {
            return lines[start.Line][start.Column..end.Column];
        }

        IEnumerable<string> parts = [lines[start.Line][start.Column..]];
        parts = parts.Concat(lines.Skip(start.Line + 1).Take(end.Line - start.Line - 1));

        return string.Join("\n", parts.Append(lines[end.Line][..end.Column]));
    }

    // Both laid out the same way first, when they're valid JSON, so only what differs shows
    private void Compare(JsonCompareSource source)
    {
        string? otherName;
        string? otherText;

        if (source == JsonCompareSource.Clipboard)
        {
            otherName = "Clipboard";
            otherText = _view.ReadClipboard();

            if (otherText is null)
            {
                _view.ShowProblem("The clipboard has no text to compare.");

                return;
            }
        }
        else
        {
            if (_view.AskOpenPath() is not string path || ReadFile(path) is not DiffFileModel file)
            {
                return;
            }

            otherName = Path.GetFileName(path);
            otherText = file.Text;
        }

        string name = _tab.Name;
        _view.ShowComparison(name, Formatted(_view.Lines), otherName, Formatted(LinesOf(otherText)));
    }

    private static string Formatted(IReadOnlyList<string> lines)
    {
        JsonDocumentModel document = JsonDocumentReader.Read(lines);

        return document.Root is not null && document.Error is null
            ? JsonTextWriter.Write(document.Root, indented: true)
            : string.Join("\n", lines);
    }

    private void GoToPath()
    {
        EnsureCurrent();
        JsonNodeModel? here = JsonPathWriter.NodeAt(_tab.Document.Root, _tab.Caret);

        if (_view.AskPath(here is null ? string.Empty : JsonPathWriter.PathOf(here)) is not string path
            || path.Trim().Length == 0)
        {
            return;
        }

        (JsonNodeModel? result, string? error, int errorAt) = JsonSearch.Run(path, _tab.Document.Root);

        if (result is null)
        {
            _view.ShowProblem($"{error} (column {errorAt + 1})");

            return;
        }

        IReadOnlyList<JsonMatchModel> places = JsonSearch.PlacesOf(result);

        if (places.Count == 0)
        {
            _view.ShowProblem($"{path} isn't in the text.");

            return;
        }

        _view.GoTo(places[0]);
    }

    private void CopyResult(bool asLines)
    {
        if (_result is not null)
        {
            _view.WriteClipboard(
                asLines ? JsonTransforms.AsLines(_result) : JsonTextWriter.Write(_result, indented: true));
        }
    }

    private void Repair()
    {
        string text = string.Join("\n", _view.Lines);
        (string repaired, IReadOnlyList<string> fixes) = JsonRepair.Repair(text);

        if (fixes.Count > 0)
        {
            _view.ReplaceText(repaired);
        }

        EnsureCurrent();
        string done = fixes.Count == 0 ? "Nothing to repair." : $"Repaired: {string.Join(", ", fixes)}.";
        string still = _tab.Document.Error is string error
            ? $" It still isn't valid JSON: {Where(_tab.Document.ErrorAt)}, {error}."
            : string.Empty;
        _view.ShowMessage(done + still);
    }

    private void Convert(JsonConversionKind kind)
    {
        if (kind is JsonConversionKind.OpenCsv or JsonConversionKind.PasteCsv)
        {
            OpenCsv(kind == JsonConversionKind.PasteCsv);

            return;
        }

        // The search's result when it has one, as people[*].name; the document otherwise
        if ((_result ?? ValidRoot()) is not JsonNodeModel node)
        {
            return;
        }

        bool isCsv = kind is JsonConversionKind.CopyCsv or JsonConversionKind.SaveCsv;
        string text;

        try
        {
            text = isCsv ? JsonCsv.Write(node) : JsonYamlWriter.Write(node);
        }
        catch (FormatException ex)
        {
            _view.ShowProblem(ex.Message);

            return;
        }

        if (kind is JsonConversionKind.CopyCsv or JsonConversionKind.CopyYaml)
        {
            _view.WriteClipboard(text);

            return;
        }

        string baseName = _tab.Path is null ? "document" : Path.GetFileNameWithoutExtension(_tab.Path);
        string name = $"{baseName}.{(isCsv ? "csv" : "yaml")}";

        if (_view.AskExportPath(name) is not string path)
        {
            return;
        }

        try
        {
            // With a BOM, Excel reads a CSV's accents right
            _writer.Write(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: isCsv));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowProblem($"Could not save {path}: {ex.Message}");
        }
    }

    private void OpenCsv(bool fromClipboard)
    {
        string? text = fromClipboard
            ? _view.ReadClipboard()
            : _view.AskCsvPath() is string path ? ReadFile(path)?.Text : null;

        if (text is null)
        {
            if (fromClipboard)
            {
                _view.ShowProblem("The clipboard has no text.");
            }

            return;
        }

        JsonNodeModel table;

        try
        {
            table = JsonCsv.Read(text);
        }
        catch (FormatException ex)
        {
            _view.ShowProblem(ex.Message);

            return;
        }

        ToFreeTab();
        Show(null, LinesOf(JsonTextWriter.Write(table, indented: true)), DiffTextFormatModel.Default);

        // Not saved anywhere yet: closing asks
        _tab.SavedLines = [string.Empty];
        ShowTitle();
    }

    private void ChooseSchema(bool choose)
    {
        if (!choose)
        {
            _tab.IsSchemaOff = true;
            _tab.ChosenSchema = null;
        }
        else if (_view.AskSchemaPath() is string path)
        {
            _tab.IsSchemaOff = false;
            _tab.ChosenSchema = path;
            _tab.SchemaPath = null;
        }
        else
        {
            return;
        }

        UpdateSchema();
        Apply(Analyze(_view.Lines));

        if (_tab.SchemaProblem is string problem && choose)
        {
            _view.ShowProblem(problem);
        }
    }

    // The schema the document should be checked against now; true when it changed
    private bool UpdateSchema()
    {
        string? path = _tab.IsSchemaOff ? null : _tab.ChosenSchema ?? NamedSchema();

        if (path == _tab.SchemaPath)
        {
            return false;
        }

        _tab.SchemaPath = path;
        _tab.Validator = null;
        _tab.SchemaProblem = null;

        if (path is null)
        {
            return true;
        }

        try
        {
            DiffFileModel file = _reader.Read(path);
            JsonDocumentModel schema = JsonDocumentReader.Read(LinesOf(file.Text));

            if (schema.Root is null || schema.Error is not null)
            {
                _tab.SchemaProblem = $"The schema {Path.GetFileName(path)} isn't valid JSON: {schema.Error}.";
            }
            else
            {
                _tab.Validator = new JsonSchemaValidator(schema.Root);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _tab.SchemaProblem = $"Could not read the schema {path}: {ex.Message}";
        }

        return true;
    }

    // The document's $schema, when it's a file: beside the document when its path is relative. One on the web
    // isn't fetched: the app goes on the internet only to look for updates
    private string? NamedSchema()
    {
        if (_tab.Document.Root?.Member("$schema") is not { Kind: JsonNodeKind.String } named
            || named.Text.Contains("://", StringComparison.Ordinal)
            || named.Text.StartsWith("urn:", StringComparison.Ordinal))
        {
            return null;
        }

        string location = named.Text.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(named.Text, UriKind.Absolute, out Uri? uri)
                ? uri.LocalPath
                : named.Text;

        return Path.IsPathRooted(location) || _tab.Path is null
            ? location
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_tab.Path)!, location));
    }

    private void ShowSchemaStatus()
    {
        string? web = _tab.Document.Root?.Member("$schema") is { Kind: JsonNodeKind.String } named
            && named.Text.Contains("://", StringComparison.Ordinal) && _tab.ChosenSchema is null && !_tab.IsSchemaOff
                ? "Schema on the web: not checked"
                : null;
        string? status = _tab.SchemaPath is null ? web
            : _tab.SchemaProblem is not null ? $"Schema {Path.GetFileName(_tab.SchemaPath)}: can't be read"
            : _tab.Problems.Count == 0 ? $"Schema {Path.GetFileName(_tab.SchemaPath)}: valid"
            : $"Schema {Path.GetFileName(_tab.SchemaPath)}: {Count(_tab.Problems.Count, "problem", "problems")}";
        _view.ShowSchemaStatus(status);
    }

    private void StepProblem(int step)
    {
        if (_tab.Problems.Count == 0)
        {
            return;
        }

        int index = step > 0
            ? _tab.Problems.ToList().FindIndex(problem => problem.Start > _tab.Caret)
            : _tab.Problems.ToList().FindLastIndex(problem => problem.Start < _tab.Caret);

        if (index < 0)
        {
            index = step > 0 ? 0 : _tab.Problems.Count - 1;
        }

        JsonSchemaProblemModel found = _tab.Problems[index];
        _view.GoTo(new JsonMatchModel(found.Start, found.End));
        _view.ShowSchemaStatus($"{index + 1} of {_tab.Problems.Count}: {found.Message}");
    }

    private void Search(bool suggest)
    {
        string query = _view.SearchText;

        if (suggest)
        {
            EnsureCurrent();
            _view.ShowSuggestions(JsonSuggester.Suggest(_view.SearchMode, query, _tab.Document.Root));
        }

        _current = -1;
        _result = null;

        if (query.Trim().Length == 0)
        {
            _matches = [];
            _view.ShowResult(null);
            _view.ShowSearchMessage(string.Empty, isProblem: false);
        }
        else if (_view.SearchMode == JsonSearchMode.Text)
        {
            _matches = JsonSearch.FindText(_view.Lines, query);
            _view.ShowResult(null);
            _view.ShowSearchMessage(Count(_matches.Count, "match", "matches"), isProblem: false);
        }
        else
        {
            (JsonNodeModel? result, string? error, int errorAt) = JsonSearch.Run(query, _tab.Document.Root);

            if (result is null)
            {
                _matches = [];
                _view.ShowResult(null);
                _view.ShowSearchMessage($"{error} (column {errorAt + 1})", isProblem: true);
            }
            else
            {
                _result = result;
                _matches = JsonSearch.PlacesOf(result);
                _view.ShowResult(JsonTextWriter.Write(result, indented: true));
                _view.ShowSearchMessage(
                    _matches.Count == 0 ? "The result isn't in the text" : Count(_matches.Count, "place", "places"),
                    isProblem: false);
            }
        }

        _view.ShowMatches(_matches, _current);
    }

    // To the next match after the caret, or before it; around at the ends
    private void Step(int step)
    {
        if (_matches.Count == 0)
        {
            return;
        }

        if (_current < 0)
        {
            int after = _matches.ToList().FindIndex(match => match.Start >= _tab.Caret);
            _current = step > 0 ? (after < 0 ? 0 : after) : (after <= 0 ? _matches.Count - 1 : after - 1);
        }
        else
        {
            _current = ((_current + step) % _matches.Count + _matches.Count) % _matches.Count;
        }

        _view.ShowMatches(_matches, _current);
        _view.GoTo(_matches[_current]);
        _view.ShowSearchMessage($"{_current + 1} of {_matches.Count}", isProblem: false);
    }

    private void Choose(JsonSuggestionModel suggestion)
    {
        string query = _view.SearchText;
        string text = query[..Math.Min(suggestion.Start, query.Length)] + suggestion.Insert;
        _view.SetSearchText(text);

        // A text chosen is what's looked for: offering the longer ones that hold it would keep Enter choosing, since
        // it chooses while the list shows, and it would never go to the matches
        if (_view.SearchMode == JsonSearchMode.Text)
        {
            _view.ShowSuggestions([]);
        }
    }

    private static string Where(DiffPosition at) => $"line {at.Line + 1}, character {at.Column + 1}";

    private static string Count(int count, string one, string many) =>
        count == 0 ? $"No {many}" : $"{count} {(count == 1 ? one : many)}";
}
