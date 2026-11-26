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
///     were written (encoding, line breaks).
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

    private string? _path;
    private DiffTextFormatModel _format = DiffTextFormatModel.Default;
    private IReadOnlyList<string> _savedLines = [string.Empty];
    private JsonDocumentModel _document = JsonDocumentReader.Read([string.Empty]);
    private IReadOnlyList<JsonMatchModel> _matches = [];
    private JsonNodeModel? _result;
    private int _current = -1;
    private DiffPosition _caret;
    private JsonEditorSettingsModel _shownSettings = new(IsDark: null);

    // The schema: one chosen, or else the one the document names in $schema, unless turned off
    private string? _chosenSchema;
    private bool _isSchemaOff;
    private string? _schemaPath;
    private JsonSchemaValidator? _validator;
    private string? _schemaProblem;
    private IReadOnlyList<JsonSchemaProblemModel> _problems = [];

    // A large text: read on another thread, once more if it changed meanwhile; _document is behind it until then
    private bool _isStale;
    private bool _isReading;
    private bool _readAgain;
    private Task _reading = Task.CompletedTask;

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
        _view.CloseRequested += (_, e) => e.Cancel = !MayDiscard();
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

    public bool IsModified => !_view.Lines.SequenceEqual(_savedLines);

    /// <summary>The read of a large text going on, for tests: done once the document is shown.</summary>
    public Task Reading => _reading;

    /// <summary>Shows a file, or an empty text for null.</summary>
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

    /// <summary>Opens a file, asking first about changes not saved.</summary>
    public void Open(string path)
    {
        if (MayDiscard())
        {
            Load(path);
        }
    }

    private void New()
    {
        if (MayDiscard())
        {
            Show(null, [string.Empty], DiffTextFormatModel.Default);
        }
    }

    private void OpenAsked()
    {
        if (MayDiscard() && _view.AskOpenPath() is string path)
        {
            Load(path);
        }
    }

    private bool Load(string path)
    {
        if (ReadFile(path) is not DiffFileModel file)
        {
            return false;
        }

        DiffTextFormatModel format = DiffTextFormatModel.Of(file.Text, file.Encoding);
        List<string> lines = LinesOf(file.Text);

        if (format.EndsWithLineBreak && lines.Count > 1)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        Show(path, lines, format);

        return true;
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
        _path = path;
        _format = format;
        _savedLines = [.. lines];
        _view.ShowText(lines);
        Changed();
    }

    private bool Save() => _path is null ? SaveAs() : SaveTo(_path);

    private bool SaveAs() =>
        _view.AskSavePath(_path is null ? "document.json" : Path.GetFileName(_path)) is string path && SaveTo(path);

    private bool SaveTo(string path)
    {
        string text = _format.Join(_view.Lines) + (_format.EndsWithLineBreak ? _format.LineBreak : string.Empty);

        try
        {
            _writer.Write(path, text, _format.Encoding);
            _path = path;
            _savedLines = [.. _view.Lines];
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

        if (_document.Root is null)
        {
            _view.ShowProblem("There's no JSON here.");

            return null;
        }

        if (_document.Error is string error)
        {
            _view.ShowProblem($"The JSON isn't valid: {Where(_document.ErrorAt)}, {error}.");

            return null;
        }

        return _document.Root;
    }

    private void Changed()
    {
        ShowTitle();

        if (_view.Lines.Sum(line => line.Length + 1) > LargeTextLength)
        {
            _isStale = true;
            ReadInBackground();
            ShowCaret(_caret);
        }
        else
        {
            Apply(Analyze(_view.Lines));
        }
    }

    private void ReadInBackground()
    {
        if (_isReading)
        {
            _readAgain = true;

            return;
        }

        _isReading = true;
        _reading = ReadUntilCurrentAsync();
    }

    // Back on the window's thread after each read; a read the text has gone past is read again, not shown
    private async Task ReadUntilCurrentAsync()
    {
        try
        {
            do
            {
                _readAgain = false;
                string[] lines = [.. _view.Lines];
                (JsonDocumentModel, IReadOnlyList<JsonSchemaProblemModel>) read = await Task.Run(() => Analyze(lines));

                if (!_readAgain && _isStale)
                {
                    Apply(read);
                }
            }
            while (_readAgain);
        }
        finally
        {
            _isReading = false;
        }
    }

    // Before what needs the document as the text is now: a large one is read here and now
    private void EnsureCurrent()
    {
        if (_isStale)
        {
            Apply(Analyze(_view.Lines));
        }
    }

    private (JsonDocumentModel Document, IReadOnlyList<JsonSchemaProblemModel> Problems) Analyze(
        IReadOnlyList<string> lines)
    {
        JsonDocumentModel document = JsonDocumentReader.Read(lines);
        JsonSchemaValidator? validator = _validator;

        // A broken document's values stop at the error, so what's missing after it isn't missing
        IReadOnlyList<JsonSchemaProblemModel> problems = validator is not null && document.Root is not null
            && document.Error is null
                ? validator.Validate(document.Root)
                : [];

        return (document, problems);
    }

    private void Apply((JsonDocumentModel Document, IReadOnlyList<JsonSchemaProblemModel> Problems) read)
    {
        _isStale = false;
        _document = read.Document;
        _view.ShowDocument(_document);

        // The document may name another schema now
        if (UpdateSchema())
        {
            read = Analyze(_view.Lines);
            _document = read.Document;
        }

        _problems = read.Problems;
        _view.ShowSchemaProblems(_problems);
        ShowSchemaStatus();

        if (_shownSettings.IsTreeShown)
        {
            _view.ShowTree(_document.Root);
        }

        ShowCaret(_caret);
        Search(suggest: false);
    }

    private void ShowTitle()
    {
        string name = _path is null ? "Untitled" : Path.GetFileName(_path);
        _view.ShowTitle($"{name}{(IsModified ? " *" : string.Empty)} - {BaseTitle}");
    }

    private void ShowCaret(DiffPosition caret)
    {
        _caret = caret;
        JsonNodeModel? node = _isStale ? null : JsonPathWriter.NodeAt(_document.Root, caret);
        string validity = _isStale ? "Reading…"
            : _document.Root is null && _document.Error is null ? "Empty"
            : _document.Error is string error ? $"L{Where(_document.ErrorAt)[1..]}: {error}"
            : "Valid JSON";

        _view.ShowStatus(
            $"Ln {caret.Line + 1}, Col {caret.Column + 1}",
            node is null ? string.Empty : JsonPathWriter.PathOf(node),
            validity);
        _view.ShowCrumbs(node is null ? [] : JsonPathWriter.CrumbsOf(node));
        _view.ShowBracketPair(_isStale ? null : JsonBrackets.PairAt(_document.Tokens, _view.Lines, caret));

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
            _view.ShowTree(_document.Root);
            ShowCaret(_caret);
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
        JsonNodeModel? clicked = JsonPathWriter.NodeAt(_document.Root, request.Position);
        IReadOnlyList<JsonNodeModel> values = request.Selection is var (start, end)
            ? JsonPathWriter.NodesIn(_document.Root, start, end)
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
        if (_isStale || JsonPathWriter.NodeAt(_document.Root, request.Position) is not JsonNodeModel node)
        {
            return;
        }

        var text = new StringBuilder();
        text.Append(JsonPathWriter.PathOf(node)).Append('\n').Append(JsonPathWriter.KindOf(node));

        foreach (JsonSchemaProblemModel problem in _problems.Where(
            problem => problem.Start <= request.Position && request.Position < problem.End))
        {
            text.Append('\n').Append(problem.Message);
        }

        request.Text = text.ToString();
    }

    private void AnswerKeys(JsonKeyRequestModel request)
    {
        EnsureCurrent();

        if (_document.Root is JsonNodeModel root
            && JsonKeySuggester.ObjectAt(root, request.Position) is JsonNodeModel target)
        {
            request.Names = JsonKeySuggester.NamesFor(target, root, _validator);
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

        if (JsonKeySuggester.ObjectAt(_document.Root, _caret) is not
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

        string name = _path is null ? "Untitled" : Path.GetFileName(_path);
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
        JsonNodeModel? here = JsonPathWriter.NodeAt(_document.Root, _caret);

        if (_view.AskPath(here is null ? string.Empty : JsonPathWriter.PathOf(here)) is not string path
            || path.Trim().Length == 0)
        {
            return;
        }

        (JsonNodeModel? result, string? error, int errorAt) = JsonSearch.Run(path, _document.Root);

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
        string still = _document.Error is string error
            ? $" It still isn't valid JSON: {Where(_document.ErrorAt)}, {error}."
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

        string baseName = _path is null ? "document" : Path.GetFileNameWithoutExtension(_path);
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

        if (MayDiscard())
        {
            Show(null, LinesOf(JsonTextWriter.Write(table, indented: true)), DiffTextFormatModel.Default);

            // Not saved anywhere yet: closing asks
            _savedLines = [string.Empty];
            ShowTitle();
        }
    }

    private void ChooseSchema(bool choose)
    {
        if (!choose)
        {
            _isSchemaOff = true;
            _chosenSchema = null;
        }
        else if (_view.AskSchemaPath() is string path)
        {
            _isSchemaOff = false;
            _chosenSchema = path;
            _schemaPath = null;
        }
        else
        {
            return;
        }

        UpdateSchema();
        Apply(Analyze(_view.Lines));

        if (_schemaProblem is string problem && choose)
        {
            _view.ShowProblem(problem);
        }
    }

    // The schema the document should be checked against now; true when it changed
    private bool UpdateSchema()
    {
        string? path = _isSchemaOff ? null : _chosenSchema ?? NamedSchema();

        if (path == _schemaPath)
        {
            return false;
        }

        _schemaPath = path;
        _validator = null;
        _schemaProblem = null;

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
                _schemaProblem = $"The schema {Path.GetFileName(path)} isn't valid JSON: {schema.Error}.";
            }
            else
            {
                _validator = new JsonSchemaValidator(schema.Root);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _schemaProblem = $"Could not read the schema {path}: {ex.Message}";
        }

        return true;
    }

    // The document's $schema, when it's a file: beside the document when its path is relative. One on the web
    // isn't fetched: the app goes on the internet only to look for updates
    private string? NamedSchema()
    {
        if (_document.Root?.Member("$schema") is not { Kind: JsonNodeKind.String } named
            || named.Text.Contains("://", StringComparison.Ordinal)
            || named.Text.StartsWith("urn:", StringComparison.Ordinal))
        {
            return null;
        }

        string location = named.Text.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(named.Text, UriKind.Absolute, out Uri? uri)
                ? uri.LocalPath
                : named.Text;

        return Path.IsPathRooted(location) || _path is null
            ? location
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_path)!, location));
    }

    private void ShowSchemaStatus()
    {
        string? web = _document.Root?.Member("$schema") is { Kind: JsonNodeKind.String } named
            && named.Text.Contains("://", StringComparison.Ordinal) && _chosenSchema is null && !_isSchemaOff
                ? "Schema on the web: not checked"
                : null;
        string? status = _schemaPath is null ? web
            : _schemaProblem is not null ? $"Schema {Path.GetFileName(_schemaPath)}: can't be read"
            : _problems.Count == 0 ? $"Schema {Path.GetFileName(_schemaPath)}: valid"
            : $"Schema {Path.GetFileName(_schemaPath)}: {Count(_problems.Count, "problem", "problems")}";
        _view.ShowSchemaStatus(status);
    }

    private void StepProblem(int step)
    {
        if (_problems.Count == 0)
        {
            return;
        }

        int index = step > 0
            ? _problems.ToList().FindIndex(problem => problem.Start > _caret)
            : _problems.ToList().FindLastIndex(problem => problem.Start < _caret);

        if (index < 0)
        {
            index = step > 0 ? 0 : _problems.Count - 1;
        }

        JsonSchemaProblemModel found = _problems[index];
        _view.GoTo(new JsonMatchModel(found.Start, found.End));
        _view.ShowSchemaStatus($"{index + 1} of {_problems.Count}: {found.Message}");
    }

    private void Search(bool suggest)
    {
        string query = _view.SearchText;

        if (suggest)
        {
            EnsureCurrent();
            _view.ShowSuggestions(JsonSuggester.Suggest(_view.SearchMode, query, _document.Root));
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
            (JsonNodeModel? result, string? error, int errorAt) = JsonSearch.Run(query, _document.Root);

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
            int after = _matches.ToList().FindIndex(match => match.Start >= _caret);
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
