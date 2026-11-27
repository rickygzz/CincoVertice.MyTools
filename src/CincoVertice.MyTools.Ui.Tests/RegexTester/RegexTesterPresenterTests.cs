using CincoVertice.MyTools.Ui.RegexTester.Interfaces;
using CincoVertice.MyTools.Ui.RegexTester.Models;
using CincoVertice.MyTools.Ui.RegexTester.Presenters;
using CincoVertice.MyTools.Ui.RegexTester.Services;
using CincoVertice.MyTools.Ui.RegexTester.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.RegexTester;

public class RegexTesterPresenterTests
{
    private readonly FakeView _view = new();
    private readonly FakeStore _store = new();

    private RegexTesterPresenter Start(RegexInputModel? saved = null)
    {
        _store.Saved = saved;
        var presenter = new RegexTesterPresenter(_view, _store);
        presenter.Start();

        return presenter;
    }

    [Fact]
    public void Start_ShowsWhatWasSavedLast_AndWhatItGives_WithoutSavingIt()
    {
        // Act
        Start(new RegexInputModel(@"(\d)", "a1 b2", "<$1>"));

        // Assert
        Assert.Equal(@"(\d)", _view.Input.Pattern);
        Assert.Equal(("2 matches", false), (_view.Status, _view.IsProblem));
        Assert.Equal("a<1> b<2>", _view.Replaced);
        Assert.Equal(4, _view.Rows.Count);
        Assert.Empty(_store.SaveCalls);
    }

    [Fact]
    public void Start_WithNothingSaved_AsksForAPattern()
    {
        // Act
        Start();

        // Assert
        Assert.Equal("Write a pattern to see what it matches.", _view.Status);
        Assert.Empty(_view.Rows);
        Assert.Null(_view.Replaced);
    }

    [Fact]
    public void InputChanged_RunsItAgain_AndSavesIt()
    {
        // Arrange
        Start();

        // Act
        _view.Input = new RegexInputModel("[", "abc");
        _view.RaiseInputChanged();

        // Assert
        Assert.True(_view.IsProblem);
        Assert.Contains("[", _view.Status);
        Assert.Equal(_view.Input, Assert.Single(_store.SaveCalls));
    }

    [Fact]
    public void Rows_AreEachMatch_ThenItsGroups_OnOneLine()
    {
        // Arrange
        Start();

        // Act
        _view.Input = new RegexInputModel(@"(?<word>\w+)(x)?\n?", "ab\ncd");
        _view.RaiseInputChanged();

        // Assert
        Assert.Equal(
            [
                new RegexRowModel("Match 1", "ab↵", "at 0, 3 long", 0, 3, IsGroup: false),
                new RegexRowModel("  Group 1", "(took no part)", "", -1, 0, IsGroup: true),
                new RegexRowModel("  word", "ab", "at 0, 2 long", 0, 2, IsGroup: true),
                new RegexRowModel("Match 2", "cd", "at 3, 2 long", 3, 2, IsGroup: false),
                new RegexRowModel("  Group 1", "(took no part)", "", -1, 0, IsGroup: true),
                new RegexRowModel("  word", "cd", "at 3, 2 long", 3, 2, IsGroup: true),
            ],
            _view.Rows);
    }

    [Fact]
    public void Highlights_AlternateBetweenMatches_WithTheirGroupsOverThem()
    {
        // Arrange
        Start();

        // Act
        _view.Input = new RegexInputModel(@"(\w)\w", "ab cd ef");
        _view.RaiseInputChanged();

        // Assert
        Assert.Equal(
            [
                new RegexHighlightModel(0, 2, RegexHighlightKind.Match),
                new RegexHighlightModel(0, 1, RegexHighlightKind.Group, 1),
                new RegexHighlightModel(3, 2, RegexHighlightKind.OtherMatch),
                new RegexHighlightModel(3, 1, RegexHighlightKind.Group, 1),
                new RegexHighlightModel(6, 2, RegexHighlightKind.Match),
                new RegexHighlightModel(6, 1, RegexHighlightKind.Group, 1),
            ],
            _view.Highlights);
    }

    [Fact]
    public void RowChosen_SelectsItInTheText_ButNotAGroupThatTookNoPart()
    {
        // Arrange
        Start(new RegexInputModel("b(x)?", "abc"));

        // Act
        _view.RaiseRowChosen(_view.Rows[0]);
        (int, int)? selected = _view.Selected;
        _view.Selected = null;
        _view.RaiseRowChosen(_view.Rows[1]);

        // Assert
        Assert.Equal((1, 1), selected);
        Assert.Null(_view.Selected);
    }

    [Fact]
    public void Status_SaysHowMany_OneOrNone_AndWhenThereAreMore()
    {
        // Act / Assert
        Assert.Equal("No match", RegexTesterPresenter.StatusOf(new("x", ""), new([], false, null, null)));
        Assert.Equal(
            "1000 matches shown; there are more",
            RegexTesterPresenter.StatusOf(
                new("x", ""),
                new([.. Enumerable.Range(1, 1000).Select(i => new RegexMatchModel(i, 0, 0, "", []))], true, null, null)));
    }

    [Fact]
    public void Rows_ALongValue_IsCut()
    {
        // Act
        Start(new RegexInputModel(".+", new string('a', RegexTesterPresenter.MaxShownLength + 10)));

        // Assert
        Assert.Equal(new string('a', RegexTesterPresenter.MaxShownLength) + "…", _view.Rows[0].Value);
    }

    private sealed class FakeStore : IRegexTesterSettingsStore
    {
        public RegexInputModel? Saved { get; set; }

        public List<RegexInputModel> SaveCalls { get; } = [];

        public RegexInputModel? Load() => Saved;

        public void Save(RegexInputModel input) => SaveCalls.Add(input);
    }

    [Fact]
    public void PieceChosen_WritesItIntoThePattern_AndTicksEachLineForALinePiece()
    {
        // Arrange
        Start();
        RegexPieceModel line = RegexPieces.All.Single(piece => piece.Title == "Line containing…");

        // Act: "error" selected in the pattern
        _view.RaisePieceChosen(new RegexPieceRequestModel(line, new RegexEditModel("error", 0, 5), ""));

        // Assert
        Assert.Equal((new RegexEditModel("^.*error.*$", 11, 0), true), _view.ShownPattern);
    }

    private sealed class FakeView : IRegexTesterView
    {
        public event EventHandler? InputChanged;

        public event EventHandler<RegexRowModel>? RowChosen;

        public event EventHandler<RegexPieceRequestModel>? PieceChosen;

        public (RegexEditModel Pattern, bool EachLine)? ShownPattern { get; private set; }

        public RegexInputModel Input { get; set; } = new("", "");

        public IReadOnlyList<RegexHighlightModel> Highlights { get; private set; } = [];

        public IReadOnlyList<RegexRowModel> Rows { get; private set; } = [];

        public string Status { get; private set; } = "";

        public bool IsProblem { get; private set; }

        public string? Replaced { get; private set; }

        public (int Start, int Length)? Selected { get; set; }

        public void RaiseInputChanged() => InputChanged?.Invoke(this, EventArgs.Empty);

        public void RaiseRowChosen(RegexRowModel row) => RowChosen?.Invoke(this, row);

        public void RaisePieceChosen(RegexPieceRequestModel request) => PieceChosen?.Invoke(this, request);

        public void ShowPattern(RegexEditModel pattern, bool eachLine) => ShownPattern = (pattern, eachLine);

        public void ShowInput(RegexInputModel input) => Input = input;

        public void ShowHighlights(IReadOnlyList<RegexHighlightModel> highlights) => Highlights = highlights;

        public void ShowRows(IReadOnlyList<RegexRowModel> rows) => Rows = rows;

        public void ShowStatus(string text, bool isProblem) => (Status, IsProblem) = (text, isProblem);

        public void ShowReplaced(string? text) => Replaced = text;

        public void SelectText(int start, int length) => Selected = (start, length);
    }
}
