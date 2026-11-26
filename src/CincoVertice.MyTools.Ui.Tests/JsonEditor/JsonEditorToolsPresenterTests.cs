using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Presenters;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.JsonEditor;

public class JsonEditorToolsPresenterTests
{
    private const string Path = @"C:\data\people.json";
    private const string Json = "{\n  \"people\": [\n    { \"name\": \"Ann\" },\n    { \"name\": \"Bob\" }\n  ]\n}\n";

    private readonly FakeJsonEditorView _view = new();
    private readonly FakeJsonFiles _files = new();
    private readonly FakeJsonSettings _settings = new();
    private readonly JsonEditorPresenter _presenter;

    public JsonEditorToolsPresenterTests()
    {
        _files.Texts[Path] = Json;
        _presenter = new JsonEditorPresenter(_view, _files, _files, _settings);
        _presenter.Start(Path);
    }

    private void SetText(params string[] lines) => _view.Edit(all =>
    {
        all.Clear();
        all.AddRange(lines);
    });

    [Fact]
    public void Tree_Toggled_ShowsTheDocument_FollowsTheCaret_AndIsKept()
    {
        // Act
        _view.RaiseTreeToggled();
        _view.MoveCaret(new DiffPosition(3, 15));

        // Assert
        Assert.True(_view.IsTreeShown);
        Assert.Equal("{1}", Ui.JsonEditor.Services.JsonTree.LabelOf(_view.Tree!));
        Assert.Equal("name: \"Bob\"", Ui.JsonEditor.Services.JsonTree.LabelOf(_view.SelectedInTree!));
        Assert.True(_settings.Settings!.IsTreeShown);
    }

    [Fact]
    public void Start_ShowsTheTreePanel_WhenItWasShownLastTime()
    {
        // Arrange
        var view = new FakeJsonEditorView();
        var settings = new FakeJsonSettings { Settings = new JsonEditorSettingsModel(null, IsTreeShown: true) };

        // Act
        new JsonEditorPresenter(view, _files, _files, settings).Start(Path);

        // Assert
        Assert.True(view.IsTreeShown);
        Assert.NotNull(view.Tree);
    }

    [Fact]
    public void NodeChosen_GoesToTheValue()
    {
        // Arrange
        _view.RaiseTreeToggled();

        // Act
        _view.RaiseNodeChosen(_view.Tree!.Member("people")!.Items[1]);

        // Assert
        Assert.Equal(new JsonMatchModel(new DiffPosition(3, 4), new DiffPosition(3, 21)), _view.WentTo);
    }

    [Fact]
    public void CaretMoved_ShowsTheCrumbs_AndTheBracketPair()
    {
        // Act
        _view.MoveCaret(new DiffPosition(3, 15));
        IReadOnlyList<string> crumbs = [.. _view.Crumbs.Select(crumb => crumb.Text)];
        _view.MoveCaret(new DiffPosition(1, 12));

        // Assert
        Assert.Equal(["people", "[1]", "name"], crumbs);
        Assert.Equal((new DiffPosition(1, 12), new DiffPosition(4, 2)), _view.BracketPair);
    }

    [Fact]
    public void Hover_SaysThePathAndWhatTheValueIs()
    {
        // Act
        var request = new JsonHoverRequestModel(new DiffPosition(2, 16));
        _view.RaiseHover(request);

        // Assert
        Assert.Equal("people[0].name\nstring, 3 characters", request.Text);
    }

    [Fact]
    public void SortKeys_Here_SortsOnlyTheObjectAtTheCaret_InItsPlace()
    {
        // Arrange
        SetText("{", "  \"b\": 1,", "  \"x\": { \"d\": 1, \"c\": 2 },", "  \"a\": 0", "}");
        _view.MoveCaret(new DiffPosition(2, 12));

        // Act
        _view.RaiseSortKeys(here: true);

        // Assert
        Assert.Equal(
            ["{", "  \"b\": 1,", "  \"x\": {", "    \"c\": 2,", "    \"d\": 1", "  },", "  \"a\": 0", "}"],
            _view.Lines);
    }

    [Fact]
    public void SortKeys_All_SortsTheWholeDocument()
    {
        // Arrange
        SetText("{\"b\": 1, \"a\": {\"d\": 1, \"c\": 2}}");

        // Act
        _view.RaiseSortKeys(here: false);

        // Assert
        Assert.Equal(["{", "  \"a\": {", "    \"c\": 2,", "    \"d\": 1", "  },", "  \"b\": 1", "}"], _view.Lines);
    }

    [Fact]
    public void Compare_WithTheClipboard_FormatsBothSides()
    {
        // Arrange
        _view.Clipboard = "{\"people\":[]}";

        // Act
        _view.RaiseCompare(JsonCompareSource.Clipboard);

        // Assert
        Assert.Equal(("people.json", "Clipboard"), (_view.Comparison?.LeftName, _view.Comparison?.RightName));
        Assert.Equal("{\n  \"people\": []\n}", _view.Comparison?.RightText);
        Assert.StartsWith("{\n  \"people\": [\n    {\n      \"name\": \"Ann\"", _view.Comparison?.LeftText);
    }

    [Fact]
    public void Compare_WithAnEmptyClipboard_SaysSo()
    {
        // Arrange
        _view.CollectsProblems = true;

        // Act
        _view.RaiseCompare(JsonCompareSource.Clipboard);

        // Assert
        Assert.Null(_view.Comparison);
        Assert.Equal("The clipboard has no text to compare.", Assert.Single(_view.Problems));
    }

    [Fact]
    public void GoToPath_OffersThePathAtTheCaret_AndGoesToTheOneGiven()
    {
        // Arrange
        _view.MoveCaret(new DiffPosition(2, 16));
        _view.PathAnswer = "people[1].name";

        // Act
        _view.RaiseGoToPath();

        // Assert
        Assert.Equal("people[0].name", _view.PathAsked);
        Assert.Equal(new DiffPosition(3, 14), _view.WentTo?.Start);
    }

    [Fact]
    public void GoToPath_ToNothing_SaysSo()
    {
        // Arrange
        _view.CollectsProblems = true;
        _view.PathAnswer = "people[5]";

        // Act
        _view.RaiseGoToPath();

        // Assert
        Assert.Equal("people[5] isn't in the text.", Assert.Single(_view.Problems));
    }

    [Fact]
    public void CopyResult_AsLines_GivesTheValues_WithoutQuotes()
    {
        // Arrange
        _view.Search(JsonSearchMode.JmesPath, "people[*].name");

        // Act
        _view.RaiseCopyResult(asLines: true);
        string lines = _view.Clipboard!;
        _view.RaiseCopyResult(asLines: false);

        // Assert
        Assert.Equal($"Ann{Environment.NewLine}Bob", lines);
        Assert.Equal("[\n  \"Ann\",\n  \"Bob\"\n]", _view.Clipboard);
    }

    [Fact]
    public void Repair_RewritesTheText_AndSaysWhatItDid()
    {
        // Arrange
        SetText("{name: 'Ann',}");

        // Act
        _view.RaiseRepair();

        // Assert
        Assert.Equal(["{\"name\": \"Ann\"}"], _view.Lines);
        Assert.Equal(
            "Repaired: 1 name put in quotes, 1 string put in double quotes, 1 comma before a closing bracket removed.",
            Assert.Single(_view.Messages));
        Assert.Equal("Valid JSON", _view.Validity);
    }

    [Fact]
    public void Repair_OfWhatItCantFix_SaysItIsStillNotValid()
    {
        // Arrange
        SetText("{\"a\": 1");

        // Act
        _view.RaiseRepair();

        // Assert
        Assert.StartsWith("Nothing to repair. It still isn't valid JSON: line 1", Assert.Single(_view.Messages));
    }

    [Fact]
    public void Convert_CopiesTheSearchResult_OrElseTheDocument()
    {
        // Act
        _view.RaiseConvert(JsonConversionKind.CopyYaml);
        string document = _view.Clipboard!;
        _view.Search(JsonSearchMode.JmesPath, "people[*].name");
        _view.RaiseConvert(JsonConversionKind.CopyCsv);

        // Assert
        Assert.Equal("people:\n  - name: Ann\n  - name: Bob\n", document);
        Assert.Equal("value\r\nAnn\r\nBob\r\n", _view.Clipboard);
    }

    [Fact]
    public void Convert_SaveCsv_WritesTheFile()
    {
        // Arrange
        _view.Search(JsonSearchMode.JmesPath, "people");
        _view.ExportPath = @"C:\data\people.csv";

        // Act
        _view.RaiseConvert(JsonConversionKind.SaveCsv);

        // Assert
        Assert.Equal("name\r\nAnn\r\nBob\r\n", _files.Texts[@"C:\data\people.csv"]);
    }

    [Fact]
    public void Convert_PasteCsv_OpensItAsANewDocument()
    {
        // Arrange
        _view.Clipboard = "a,b\n1,x";

        // Act
        _view.RaiseConvert(JsonConversionKind.PasteCsv);

        // Assert
        Assert.Equal(["[", "  {", "    \"a\": 1,", "    \"b\": \"x\"", "  }", "]"], _view.Lines);
        Assert.Equal("Untitled * - JSON editor", _view.Title);
    }

    [Fact]
    public void Schema_NamedByTheDocument_IsReadBesideIt_AndChecked()
    {
        // Arrange
        _files.Texts[@"C:\data\person.schema.json"] =
            "{\"properties\":{\"age\":{\"type\":\"number\"}},\"required\":[\"name\"]}";

        // Act
        SetText("{", "  \"$schema\": \"person.schema.json\",", "  \"age\": \"old\"", "}");

        // Assert
        Assert.Equal(
            ["Missing \"name\"", "Expected a number, not a string"],
            _view.SchemaProblems.Select(problem => problem.Message));
        Assert.Equal("Schema person.schema.json: 2 problems", _view.SchemaStatus);
    }

    [Fact]
    public void Schema_Chosen_IsChecked_AndNone_StopsIt()
    {
        // Arrange
        _files.Texts[@"C:\schemas\people.json"] = "{\"required\":[\"count\"]}";
        _view.SchemaPath = @"C:\schemas\people.json";

        // Act
        _view.RaiseSchema(choose: true);
        string? chosen = _view.SchemaStatus;
        _view.RaiseSchema(choose: false);

        // Assert
        Assert.Equal("Schema people.json: 1 problem", chosen);
        Assert.Null(_view.SchemaStatus);
        Assert.Empty(_view.SchemaProblems);
    }

    [Fact]
    public void Schema_OnTheWeb_IsntFetched()
    {
        // Act
        SetText("{\"$schema\": \"https://json.schemastore.org/package.json\"}");

        // Assert
        Assert.Equal("Schema on the web: not checked", _view.SchemaStatus);
    }

    [Fact]
    public void ProblemStep_GoesToTheNextProblem_AndSaysIt()
    {
        // Arrange
        _files.Texts[@"C:\schemas\s.json"] = "{\"properties\":{\"people\":{\"items\":{\"required\":[\"age\"]}}}}";
        _view.SchemaPath = @"C:\schemas\s.json";
        _view.RaiseSchema(choose: true);

        // Act
        _view.RaiseProblemStep(1);
        _view.MoveCaret(_view.WentTo!.Value.Start);
        _view.RaiseProblemStep(1);

        // Assert
        Assert.Equal(new DiffPosition(3, 4), _view.WentTo?.Start);
        Assert.Equal("2 of 2: Missing \"age\"", _view.SchemaStatus);
    }

    [Fact]
    public void Keys_OfferTheNamesOfTheObjectsAlike()
    {
        // Arrange
        SetText("[", "  {\"name\": \"A\", \"age\": 1},", "  { }", "]");

        // Act
        var request = new JsonKeyRequestModel(new DiffPosition(2, 4));
        _view.RaiseKeys(request);

        // Assert
        Assert.Equal(["name", "age"], request.Names);
    }

    [Fact]
    public async Task LargeText_IsReadOnAnotherThread_ThenShown()
    {
        // Arrange: more than the limit
        List<string> lines = ["["];
        lines.AddRange(Enumerable.Range(0, 40_000).Select(i => $"  {{ \"id\": {i}, \"name\": \"Person {i}\" }},"));
        lines.Add("  null");
        lines.Add("]");

        // Act
        SetText([.. lines]);
        string whileReading = _view.Validity;
        await _presenter.Reading;

        // Assert
        Assert.Equal("Reading…", whileReading);
        Assert.Equal("Valid JSON", _view.Validity);
        Assert.Equal(40_001, _view.Document!.Root!.Items.Count);
    }
}
