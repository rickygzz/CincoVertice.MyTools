using System.ComponentModel;
using System.Text;
using CincoVertice.MyTools.Ui.Diff.Interfaces;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Presenters;
using CincoVertice.MyTools.Ui.JsonEditor.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.JsonEditor;

public class JsonEditorPresenterTests
{
    private const string Path = @"C:\data\people.json";
    private const string Json = "{\n  \"people\": [\n    { \"name\": \"Ann\" },\n    { \"name\": \"Bob\" }\n  ]\n}\n";

    private readonly FakeView _view = new();
    private readonly FakeFiles _files = new();
    private readonly JsonEditorPresenter _presenter;

    public JsonEditorPresenterTests()
    {
        _files.Texts[Path] = Json;
        _presenter = new JsonEditorPresenter(_view, _files, _files);
        _presenter.Start(Path);
    }

    [Fact]
    public void Start_ShowsTheFile_ItsSectionsAndThatItsValid()
    {
        // Assert: the line break at its end isn't a line of its own
        Assert.Equal(6, _view.Lines.Count);
        Assert.Equal("people.json - JSON editor", _view.Title);
        Assert.Equal(2, _view.Document!.Folds.Count);
        Assert.Equal("Valid JSON", _view.Validity);
    }

    [Fact]
    public void CaretMoved_SaysThePathOfTheValueThere()
    {
        // Act
        _view.MoveCaret(new DiffPosition(3, 15));

        // Assert
        Assert.Equal(("Ln 4, Col 16", "people[1].name"), (_view.Position, _view.Path));
    }

    [Fact]
    public void RightClick_GivesThePathAndTheValue()
    {
        // Act
        var request = new JsonPathRequestModel(new DiffPosition(2, 4));
        _view.RaisePathRequested(request);

        // Assert
        Assert.Equal(("people[0]", "{\"name\":\"Ann\"}"), (request.Path, request.Value));
    }

    [Fact]
    public void Edit_ThatBreaksIt_SaysWhere_AndMarksTheTitle()
    {
        // Act: the comma between the people is gone
        _view.Edit(lines => lines[2] = "    { \"name\": \"Ann\" }");

        // Assert
        Assert.Equal("Line 4, character 5: Expected , or ], not {", _view.Validity);
        Assert.Equal("people.json * - JSON editor", _view.Title);
    }

    [Fact]
    public void Search_Text_FindsEachPlace_AndStepsThroughThem()
    {
        // Act
        _view.Search(JsonSearchMode.Text, "name");
        string count = _view.SearchMessage;
        _view.RaiseStep(1);
        _view.RaiseStep(1);

        // Assert
        Assert.Equal("2 matches", count);
        Assert.Equal(new DiffPosition(3, 7), _view.WentTo!.Value.Start);
        Assert.Equal("2 of 2", _view.SearchMessage);
    }

    [Fact]
    public void Search_JmesPath_ShowsTheResult_AndWhereItsValuesAre()
    {
        // Act
        _view.Search(JsonSearchMode.JmesPath, "people[*].name");

        // Assert
        Assert.Equal("[\n  \"Ann\",\n  \"Bob\"\n]", _view.Result);
        Assert.Equal("2 places", _view.SearchMessage);
        Assert.Equal(2, _view.Matches.Count);
    }

    [Fact]
    public void Search_JmesPath_ThatCantRun_SaysWhyAndWhere()
    {
        // Act
        _view.Search(JsonSearchMode.JmesPath, "people[?name = 'Ann']");

        // Assert
        Assert.Equal(("Write == to compare (column 14)", true), (_view.SearchMessage, _view.IsSearchProblem));
        Assert.Null(_view.Result);
    }

    [Fact]
    public void Suggestions_FollowTheTyping_AndAChoiceWritesIt()
    {
        // Act
        _view.Search(JsonSearchMode.JmesPath, "people[0].n");
        JsonSuggestionModel suggestion = Assert.Single(_view.Suggestions);
        _view.RaiseSuggestionChosen(suggestion);

        // Assert
        Assert.Equal("people[0].name", _view.SearchText);
        Assert.Equal("\"Ann\"", _view.Result);
    }

    [Fact]
    public void Format_LaysTheJsonOut_AsAnEdit_AndMinifyPutsItOnOneLine()
    {
        // Act
        _view.RaiseMinify();
        string minified = string.Join("\n", _view.Lines);
        _view.RaiseFormat();

        // Assert
        Assert.Equal("{\"people\":[{\"name\":\"Ann\"},{\"name\":\"Bob\"}]}", minified);
        Assert.Equal(
            "{\n  \"people\": [\n    {\n      \"name\": \"Ann\"\n    },\n    {\n      \"name\": \"Bob\"\n    }\n  ]\n}",
            string.Join("\n", _view.Lines));
    }

    [Fact]
    public void Save_WritesTheFileAsItWas_ItsLineBreakAtTheEndToo()
    {
        // Arrange
        _view.Edit(lines => lines[2] = lines[2].Replace("Ann", "Ana", StringComparison.Ordinal));

        // Act
        _view.RaiseSave();

        // Assert
        Assert.Equal(Json.Replace("Ann", "Ana", StringComparison.Ordinal), _files.Texts[Path]);
        Assert.Equal("people.json - JSON editor", _view.Title);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(false, false)]
    public void Close_WithChanges_Asks_AndCancelKeepsItOpen(bool? answer, bool isCancelled)
    {
        // Arrange
        _view.Edit(lines => lines[0] = "{ ");
        _view.SaveAnswer = answer;

        // Act
        var closing = new CancelEventArgs();
        _view.RaiseClose(closing);

        // Assert
        Assert.Equal(isCancelled, closing.Cancel);
        Assert.Equal(1, _view.SaveAsked);
    }

    [Fact]
    public void DropOpenAndNew_ShowAnotherText_WithNoChangesToAskAbout()
    {
        // Arrange
        _files.Texts[@"C:\data\other.json"] = "[1, 2]";
        _files.Texts[@"C:\data\third.json"] = "true";
        _view.OpenPath = @"C:\data\third.json";

        // Act / Assert
        _view.RaiseDrop(@"C:\data\other.json");
        Assert.Equal(("other.json - JSON editor", "[1, 2]"), (_view.Title, _view.Lines[0]));
        _view.RaiseOpen();
        Assert.Equal(("third.json - JSON editor", "true"), (_view.Title, _view.Lines[0]));
        _view.RaiseNew();
        Assert.Equal(("Untitled - JSON editor", "Empty"), (_view.Title, _view.Validity));
        Assert.Equal(0, _view.SaveAsked);
    }

    [Fact]
    public void SaveAs_WritesWhereChosen_AndTheTitleSaysIt()
    {
        // Arrange
        _view.SavePath = @"C:\data\copy.json";

        // Act
        _view.RaiseSaveAs();

        // Assert
        Assert.Equal(Json, _files.Texts[@"C:\data\copy.json"]);
        Assert.Equal("copy.json - JSON editor", _view.Title);
    }

    private sealed class FakeFiles : IDiffFileReader, IDiffFileWriter
    {
        public Dictionary<string, string> Texts { get; } = [];

        public DiffFileModel Read(string path) =>
            Texts.TryGetValue(path, out string? text)
                ? new DiffFileModel(text, DateTime.UtcNow, new UTF8Encoding(false))
                : throw new FileNotFoundException("Not found", path);

        public DateTime? WrittenUtc(string path) => null;

        public DateTime Write(string path, string text, Encoding encoding)
        {
            Texts[path] = text;

            return DateTime.UtcNow;
        }
    }

    private sealed class FakeView : IJsonEditorView
    {
        private List<string> _lines = [string.Empty];

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

        public IReadOnlyList<string> Lines => _lines;

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

        public void ShowProblem(string message) => throw new InvalidOperationException(message);

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
    }
}
