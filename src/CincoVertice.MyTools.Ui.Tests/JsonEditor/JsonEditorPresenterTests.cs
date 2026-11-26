using System.ComponentModel;
using System.Text;
using CincoVertice.MyTools.Ui.Diff.Interfaces;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Interfaces;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Presenters;
using CincoVertice.MyTools.Ui.JsonEditor.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.JsonEditor;

public class JsonEditorPresenterTests
{
    private const string Path = @"C:\data\people.json";
    private const string Json = "{\n  \"people\": [\n    { \"name\": \"Ann\" },\n    { \"name\": \"Bob\" }\n  ]\n}\n";

    private readonly FakeJsonEditorView _view = new();
    private readonly FakeJsonFiles _files = new();
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
        Assert.Equal(1, request.ValueCount);
    }

    [Fact]
    public void RightClick_OnAMember_GivesItsNameAndValue_ButAnItemInAnArrayHasNoName()
    {
        // Act: on "name" in Ann's object, then on Bob's object in the array
        var member = new JsonPathRequestModel(new DiffPosition(2, 8));
        _view.RaisePathRequested(member);
        var item = new JsonPathRequestModel(new DiffPosition(3, 4));
        _view.RaisePathRequested(item);

        // Assert
        Assert.Equal(
            ("\"Ann\"", "\"name\": \"Ann\"", 1),
            (member.Value, member.NameAndValue, member.NameAndValueCount));
        Assert.Equal((null, 0), (item.NameAndValue, item.NameAndValueCount));
    }

    [Fact]
    public void RightClick_InASelectionOfSeveralLines_GivesEachValueInIt()
    {
        // Act: lines 3 and 4 selected whole, the two people
        var request = new JsonPathRequestModel(
            new DiffPosition(2, 8),
            (new DiffPosition(2, 0), new DiffPosition(3, 21)));
        _view.RaisePathRequested(request);

        // Assert: as in an array; the path is still the one clicked
        Assert.Equal(2, request.ValueCount);
        Assert.Equal($"{{\"name\":\"Ann\"}},{Environment.NewLine}{{\"name\":\"Bob\"}}", request.Value);
        Assert.Equal("people[0].name", request.Path);
        Assert.Null(request.NameAndValue);
    }

    [Fact]
    public void RightClick_InASelectionOfMembers_GivesTheirNamesAndValues()
    {
        // Arrange
        _view.Edit(lines =>
        {
            lines.Clear();
            lines.AddRange(["{", "  \"a\": 1,", "  \"b\": [true],", "  \"c\": null", "}"]);
        });

        // Act: "a" and "b", and the start of "c" but not its value
        var request = new JsonPathRequestModel(
            new DiffPosition(1, 3),
            (new DiffPosition(1, 0), new DiffPosition(3, 5)));
        _view.RaisePathRequested(request);

        // Assert
        Assert.Equal(2, request.NameAndValueCount);
        Assert.Equal($"\"a\": 1,{Environment.NewLine}\"b\": [true]", request.NameAndValue);
        Assert.Equal($"1,{Environment.NewLine}[true]", request.Value);
    }

    [Fact]
    public void RightClick_InASelectionInsideOneValue_GivesTheValueClicked()
    {
        // Act: part of "Ann" selected
        var request = new JsonPathRequestModel(
            new DiffPosition(2, 16),
            (new DiffPosition(2, 15), new DiffPosition(2, 17)));
        _view.RaisePathRequested(request);

        // Assert
        Assert.Equal(("\"Ann\"", 1), (request.Value, request.ValueCount));
    }

    [Fact]
    public void Start_ShowsTheThemeChosenBefore_AndToggling_KeepsTheOther()
    {
        // Arrange
        var store = new FakeJsonSettings { Settings = new JsonEditorSettingsModel(IsDark: true) };
        var view = new FakeJsonEditorView();
        var presenter = new JsonEditorPresenter(view, _files, _files, store);

        // Act
        presenter.Start(Path);
        view.RaiseThemeToggled();

        // Assert
        Assert.Equal([true, false], view.ThemesShown);
        Assert.Equal(new JsonEditorSettingsModel(IsDark: false), store.Settings);
    }

    [Fact]
    public void Start_WithNoThemeChosen_LeavesWindowsMode()
    {
        // Arrange
        var view = new FakeJsonEditorView();

        // Act
        new JsonEditorPresenter(view, _files, _files, new FakeJsonSettings()).Start(Path);

        // Assert
        Assert.Empty(view.ThemesShown);
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
    public void TextSuggestion_Chosen_IsSearched_AndNothingMoreIsOffered()
    {
        // Arrange
        _view.Edit(lines =>
        {
            lines.Clear();
            lines.Add("{\"people\":[{\"name\":\"Ann\"},{\"name\":\"Anna\"}]}");
        });

        // Act
        _view.Search(JsonSearchMode.Text, "An");
        JsonSuggestionModel ann = Assert.Single(_view.Suggestions, suggestion => suggestion.Text == "Ann");
        _view.RaiseSuggestionChosen(ann);

        // Assert
        Assert.Equal("Ann", _view.SearchText);
        Assert.Empty(_view.Suggestions);
        Assert.Equal("2 matches", _view.SearchMessage);
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
}
