using CincoVertice.MyTools.Ui.Diff.Interfaces;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Interfaces;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Services;
using CincoVertice.MyTools.Ui.JsonEditor.Views;

namespace CincoVertice.MyTools.Ui.JsonEditor.Presenters;

/// <summary>
///     The JSON editor: reads the text again at each change (colors, sections, values, the first error), says the path
///     of the value at the caret and at a right click, searches text or JMESPath with suggestions, formats, and opens
///     and saves files as they were written (encoding, line breaks).
/// </summary>
public sealed class JsonEditorPresenter
{
    public const string BaseTitle = "JSON editor";

    private readonly IJsonEditorView _view;
    private readonly IDiffFileReader _reader;
    private readonly IDiffFileWriter _writer;
    private readonly IJsonEditorSettingsStore? _settings;

    private string? _path;
    private DiffTextFormatModel _format = DiffTextFormatModel.Default;
    private IReadOnlyList<string> _savedLines = [string.Empty];
    private JsonDocumentModel _document = JsonDocumentReader.Read([string.Empty]);
    private IReadOnlyList<JsonMatchModel> _matches = [];
    private int _current = -1;
    private DiffPosition _caret;

    /// <param name="settings">Where dark or light is kept; none keeps it for the window only.</param>
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
        _view.SearchChanged += (_, _) => SearchChanged();
        _view.SearchStepRequested += (_, step) => Step(step);
        _view.SuggestionChosen += (_, suggestion) => Choose(suggestion);
        _view.PathRequested += (_, request) => AnswerPath(request);
        _view.CloseRequested += (_, e) => e.Cancel = !MayDiscard();
        _view.ThemeToggled += (_, _) => ToggleTheme();
    }

    public bool IsModified => !_view.Lines.SequenceEqual(_savedLines);

    /// <summary>Shows a file, or an empty text for null.</summary>
    public void Start(string? path)
    {
        if (_settings?.Load()?.IsDark is bool isDark)
        {
            _view.ShowTheme(isDark);
        }

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
        try
        {
            DiffFileModel file = _reader.Read(path);
            DiffTextFormatModel format = DiffTextFormatModel.Of(file.Text, file.Encoding);
            List<string> lines = [.. file.Text.ReplaceLineEndings("\n").Split('\n')];

            if (format.EndsWithLineBreak && lines.Count > 1)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            Show(path, lines, format);

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowProblem($"Could not open {path}: {ex.Message}");

            return false;
        }
    }

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
        if (_document.Root is null)
        {
            _view.ShowProblem("There's no JSON to format.");

            return;
        }

        if (_document.Error is string error)
        {
            _view.ShowProblem($"The JSON isn't valid: {Where(_document.ErrorAt)}, {error}.");

            return;
        }

        string text = JsonTextWriter.Write(_document.Root, indented);

        if (string.Join("\n", _view.Lines) != text)
        {
            _view.ReplaceText(text);
        }
    }

    private void Changed()
    {
        _document = JsonDocumentReader.Read(_view.Lines);
        _view.ShowDocument(_document);
        ShowTitle();
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
        string path = JsonPathWriter.NodeAt(_document.Root, caret) is { } node
            ? JsonPathWriter.PathOf(node)
            : string.Empty;
        string validity = _document.Root is null && _document.Error is null ? "Empty"
            : _document.Error is string error ? $"L{Where(_document.ErrorAt)[1..]}: {error}"
            : "Valid JSON";

        _view.ShowStatus($"Ln {caret.Line + 1}, Col {caret.Column + 1}", path, validity);
    }

    private void ToggleTheme()
    {
        bool isDark = !_view.IsDark;
        _view.ShowTheme(isDark);
        _settings?.Save(new JsonEditorSettingsModel(isDark));
    }

    // The values the selection holds, or the one clicked when it holds none
    private void AnswerPath(JsonPathRequestModel request)
    {
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

    private void SearchChanged()
    {
        Search(suggest: true);
    }

    private void Search(bool suggest)
    {
        string query = _view.SearchText;

        if (suggest)
        {
            _view.ShowSuggestions(JsonSuggester.Suggest(_view.SearchMode, query, _document.Root));
        }

        _current = -1;

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
    }

    private static string Where(DiffPosition at) => $"line {at.Line + 1}, character {at.Column + 1}";

    private static string Count(int count, string one, string many) =>
        count == 0 ? $"No {many}" : $"{count} {(count == 1 ? one : many)}";
}
