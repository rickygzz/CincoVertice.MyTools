using System.ComponentModel;
using System.Text;
using CincoVertice.MyTools.Ui.Diff.Interfaces;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Presenters;
using CincoVertice.MyTools.Ui.Diff.Services;
using CincoVertice.MyTools.Ui.Diff.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diff;

public class DiffPresenterTests
{
    private const string Left = @"C:\work\old.txt";
    private const string Right = @"C:\work\new.txt";

    private readonly FakeFiles _files = new();
    private readonly FakeDiffView _view = new();
    private readonly DiffPresenter _presenter;

    public DiffPresenterTests()
    {
        _files.Write(Left, "a\nb\nc\nd\ne\nf");
        _files.Write(Right, "a\nB\nc\nd\nE\nf\ng");
        _presenter = new DiffPresenter(_view, _files, _files);
    }

    [Fact]
    public void Open_TwoFiles_ShowsThemToEdit_ComparesThem_AndGoesToTheFirstChange()
    {
        // Act
        _presenter.Open(Left, Right);

        // Assert
        Assert.Equal((Left, Right), _view.Sides);
        Assert.Equal(["a", "b", "c", "d", "e", "f"], _view.Texts[DiffSide.Left]);
        Assert.Equal("old.txt vs new.txt - Compare files", _view.Title);
        Assert.Equal(3, _view.Result!.Hunks.Count);
        Assert.Equal(new DiffHunkModel(1, 1), _view.Change);
        Assert.Equal(("3 changes: 1 line added, 2 changed", "Change 1 of 3"), _view.Status);
        Assert.True(_view.CanNavigate);
    }

    [Fact]
    public void Open_WithNothing_AsksForTheFiles()
    {
        // Act
        _presenter.Open();

        // Assert
        Assert.Equal((DiffPresenter.NoSource, DiffPresenter.NoSource), _view.Sides);
        Assert.Equal(DiffPresenter.Title, _view.Title);
        Assert.Empty(_view.Result!.Rows);
        Assert.False(_view.CanNavigate);
    }

    [Fact]
    public void OneSide_ShowsItsLinesAlone_AndAsksForTheOther()
    {
        // Arrange
        _presenter.Open();

        // Act
        _view.ChooseFiles(new DiffFilesModel([Right], DiffSide.Right));

        // Assert
        Assert.Equal((DiffPresenter.NoSource, Right), _view.Sides);
        Assert.Equal("Now choose the left side, or paste or type it.", _view.Status.Summary);
        Assert.Equal(7, _view.Result!.Rows.Count);
        Assert.All(_view.Result.Rows, row => Assert.Null(row.Left));
    }

    [Fact]
    public void TwoFilesDropped_GoOnBothSides_TheFirstOnTheLeft()
    {
        // Arrange
        _presenter.Open();

        // Act
        _view.ChooseFiles(new DiffFilesModel([Left, Right], DiffSide.Right));

        // Assert
        Assert.Equal((Left, Right), _view.Sides);
    }

    [Fact]
    public void NextAndPrevious_GoFromChangeToChange_FromTheRowTheViewIsAt()
    {
        // Arrange
        _presenter.Open(Left, Right);

        // Act
        _view.Next(1);
        DiffHunkModel? second = _view.Change;
        _view.Next(4);
        DiffHunkModel? third = _view.Change;
        _view.Next(6);
        DiffHunkModel? stillThird = _view.Change;
        _view.Previous(6);
        DiffHunkModel? back = _view.Change;

        // Assert
        Assert.Equal(new DiffHunkModel(4, 1), second);
        Assert.Equal(new DiffHunkModel(6, 1), third);
        Assert.Equal(third, stillThird);
        Assert.Equal(second, back);
        Assert.Equal("Change 2 of 3", _view.Status.Position);
    }

    [Fact]
    public void AnEdit_ComparesAgain_InPlace_AndMarksTheFileUnsaved()
    {
        // Arrange
        _presenter.Open(Left, Right);

        // Act
        _view.Edit(DiffSide.Left, ["a", "B", "c", "d", "E", "f", "g"]);

        // Assert
        Assert.False(_view.Result!.HasChanges);
        Assert.True(_view.KeptPlace);
        Assert.Equal(("C:\\work\\old.txt *", Right), _view.Sides);
        Assert.Equal("old.txt* vs new.txt - Compare files", _view.Title);
        Assert.Equal("They're the same.", _view.Status.Summary);
    }

    [Fact]
    public void Typing_OnAnEmptySide_MakesItAText()
    {
        // Arrange
        _presenter.Open(Left, null);

        // Act
        _view.Edit(DiffSide.Right, ["a", "b"]);

        // Assert
        Assert.Equal((Left, DiffPresenter.PastedText), _view.Sides);
        Assert.Equal(4, _view.Result!.Removed);
    }

    [Fact]
    public void Save_WritesTheFile_AsItWasRead()
    {
        // Arrange
        _files.Write(Left, "one\r\ntwo\r\n", Encoding.UTF8);
        _presenter.Open(Left, Right);
        _view.Edit(DiffSide.Left, ["one", "2"]);

        // Act
        _view.Save(DiffSide.Left);

        // Assert
        Assert.Equal("one\r\n2\r\n", _files.TextOf(Left));
        Assert.Same(Encoding.UTF8, _files.EncodingOf(Left));
        Assert.Equal((Left, Right), _view.Sides);
    }

    [Fact]
    public void Save_PastedText_AsksWhere()
    {
        // Arrange
        _presenter.Open(Left, null);
        _view.Paste(new DiffPasteModel(DiffSide.Right, "x\ny"));
        _view.SavePath = @"C:\work\pasted.txt";

        // Act
        _view.Save(DiffSide.Right);

        // Assert
        Assert.Equal("x\ny", _files.TextOf(@"C:\work\pasted.txt"));
        Assert.Equal((Left, @"C:\work\pasted.txt"), _view.Sides);
    }

    [Fact]
    public void PastedText_TakesASidesPlace()
    {
        // Arrange
        _presenter.Open(Left, null);

        // Act
        _view.Paste(new DiffPasteModel(DiffSide.Right, "a\nb\nc\nd\ne\nf"));

        // Assert
        Assert.Equal((Left, DiffPresenter.PastedText), _view.Sides);
        Assert.Equal(["a", "b", "c", "d", "e", "f"], _view.Texts[DiffSide.Right]);
        Assert.Equal("They're the same.", _view.Status.Summary);
        Assert.Equal("old.txt vs Pasted text - Compare files", _view.Title);
    }

    [Fact]
    public void EmptyPaste_IsAProblem()
    {
        // Act
        _view.Paste(new DiffPasteModel(DiffSide.Left, string.Empty));

        // Assert
        Assert.Equal(["The clipboard has no text."], _view.Problems);
    }

    [Fact]
    public void Swap_PutsEachSideOnTheOther()
    {
        // Arrange
        _presenter.Open(Left, Right);

        // Act
        _view.Swap();

        // Assert
        Assert.Equal((Right, Left), _view.Sides);
        Assert.Equal(7, _view.Texts[DiffSide.Left].Count);
        Assert.Equal(DiffRowKind.Removed, _view.Result!.Rows[^1].Kind);
    }

    [Fact]
    public void Options_CompareAgain_InPlace()
    {
        // Arrange
        _presenter.Open(Left, Right);

        // Act
        _view.SetOptions(new DiffOptionsModel(IgnoreCase: true));

        // Assert
        Assert.Single(_view.Result!.Hunks);
        Assert.True(_view.KeptPlace);
        Assert.Null(_view.Change);
        Assert.Equal(("1 change: 1 line added", string.Empty), _view.Status);
    }

    [Fact]
    public void AFileWrittenMeanwhile_IsReadAgain_WhenTheWindowIsActive()
    {
        // Arrange
        _presenter.Open(Left, Right);
        _files.Write(Right, "a\nb\nc\nd\ne\nf");

        // Act
        _view.Activate();

        // Assert
        Assert.False(_view.Result!.HasChanges);
        Assert.Equal(6, _view.Texts[DiffSide.Right].Count);
        Assert.Empty(_view.ReloadsAsked);
    }

    [Fact]
    public void AFileEditedHere_AndWrittenMeanwhile_IsReadAgainOnlyIfTheUserAgrees()
    {
        // Arrange
        _presenter.Open(Left, Right);
        _view.Edit(DiffSide.Right, ["mine"]);
        _files.Write(Right, "theirs");
        _view.ReloadAnswer = false;

        // Act
        _view.Activate();
        _view.Activate();

        // Assert: asked once, and the edit stays
        Assert.Equal(["new.txt"], _view.ReloadsAsked);
        Assert.Equal(["mine"], _view.Texts[DiffSide.Right]);
    }

    [Fact]
    public void Activating_WithNoFileChanged_ComparesNothingAgain()
    {
        // Arrange
        _presenter.Open(Left, Right);
        DiffResultModel? before = _view.Result;

        // Act
        _view.Activate();

        // Assert
        Assert.Same(before, _view.Result);
    }

    [Fact]
    public void Reload_ReadsTheFilesAgain()
    {
        // Arrange
        _presenter.Open(Left, Right);
        _files.Write(Left, "a\nB\nc\nd\nE\nf\ng");

        // Act
        _view.Reload();

        // Assert
        Assert.False(_view.Result!.HasChanges);
    }

    [Fact]
    public void Closing_WithUnsavedChanges_Asks_AndCancelKeepsTheWindow()
    {
        // Arrange
        _presenter.Open(Left, Right);
        _view.Edit(DiffSide.Left, ["changed"]);
        _view.SaveAnswer = DiffSaveChoice.Cancel;

        // Act
        bool cancelled = _view.Close();

        // Assert
        Assert.True(cancelled);
        Assert.Equal(["old.txt"], _view.SavesAsked.Single());
    }

    [Fact]
    public void Closing_AndSaving_WritesTheChanges()
    {
        // Arrange
        _presenter.Open(Left, Right);
        _view.Edit(DiffSide.Left, ["changed"]);
        _view.SaveAnswer = DiffSaveChoice.Save;

        // Act
        bool cancelled = _view.Close();

        // Assert
        Assert.False(cancelled);
        Assert.Equal("changed", _files.TextOf(Left));
    }

    [Fact]
    public void Closing_WithOnlyPastedTextChanged_DoesNotAsk()
    {
        // Arrange
        _presenter.Open(Left, null);
        _view.Edit(DiffSide.Right, ["typed"]);

        // Act
        bool cancelled = _view.Close();

        // Assert
        Assert.False(cancelled);
        Assert.Empty(_view.SavesAsked);
    }

    [Fact]
    public void AFileThatCantBeRead_IsAProblem_AndTheSideStays()
    {
        // Arrange
        _presenter.Open(Left, Right);

        // Act
        _view.ChooseFiles(new DiffFilesModel([@"C:\missing.txt"], DiffSide.Left));

        // Assert
        Assert.Equal([@"Could not read C:\missing.txt: Not found."], _view.Problems);
        Assert.Equal((Left, Right), _view.Sides);
    }

    [Fact]
    public void ABinaryFile_IsAProblem()
    {
        // Arrange
        _files.Write(@"C:\work\app.exe", "MZ\0\0");

        // Act
        _presenter.Open(@"C:\work\app.exe", Right);

        // Assert
        Assert.Equal(["app.exe isn't a text file, so it can't be compared."], _view.Problems);
        Assert.Equal((DiffPresenter.NoSource, Right), _view.Sides);
    }

    [Theory]
    [InlineData("a\r\nb", "a\nb", false, "The lines are the same: only the line endings differ.")]
    [InlineData("a b", "ab", true, "No differences, ignoring whitespace.")]
    [InlineData("a\nb", "a\nb", false, "They're the same.")]
    public void Summary_SaysWhyTheSameDiffers(string left, string right, bool ignoreWhitespace, string summary)
    {
        // Arrange
        var options = new DiffOptionsModel(IgnoreWhitespace: ignoreWhitespace);
        DiffResultModel result = TextDiffer.Compare(left, right, options);

        // Act
        string actual = DiffPresenter.Summary(result, FileOf(left), FileOf(right), options);

        // Assert
        Assert.Equal(summary, actual);
    }

    private static DiffSourceModel FileOf(string text) => new(
        TextDiffer.SplitLines(text),
        @"C:\a.txt",
        null,
        DiffTextFormatModel.Of(text, Encoding.UTF8));

    private sealed class FakeFiles : IDiffFileReader, IDiffFileWriter
    {
        private readonly Dictionary<string, DiffFileModel> _files = [];
        private DateTime _now = new(2026, 11, 15, 9, 0, 0, DateTimeKind.Utc);

        public void Write(string path, string text, Encoding? encoding = null)
        {
            _now = _now.AddSeconds(1);
            _files[path] = new DiffFileModel(text, _now, encoding ?? new UTF8Encoding(false));
        }

        public string TextOf(string path) => _files[path].Text;

        public Encoding EncodingOf(string path) => _files[path].Encoding;

        public DiffFileModel Read(string path) =>
            _files.TryGetValue(path, out DiffFileModel? file) ? file : throw new IOException("Not found.");

        public DateTime? WrittenUtc(string path) =>
            _files.TryGetValue(path, out DiffFileModel? file) ? file.WrittenUtc : null;

        DateTime IDiffFileWriter.Write(string path, string text, Encoding encoding)
        {
            Write(path, text, encoding);

            return _now;
        }
    }

    private sealed class FakeDiffView : IDiffView
    {
        public event EventHandler<DiffFilesModel>? FilesChosen;

        public event EventHandler<DiffPasteModel>? TextPasted;

        public event EventHandler<DiffEditModel>? TextEdited;

        public event EventHandler<DiffSide>? SaveRequested;

        public event EventHandler? SwapRequested;

        public event EventHandler? ReloadRequested;

        public event EventHandler? WindowActivated;

        public event EventHandler<CancelEventArgs>? CloseRequested;

        public event EventHandler<DiffOptionsModel>? OptionsChanged;

        public event EventHandler<int>? NextChangeRequested;

        public event EventHandler<int>? PreviousChangeRequested;

        public string? Title { get; private set; }

        public (string Left, string Right) Sides { get; private set; }

        public Dictionary<DiffSide, IReadOnlyList<string>> Texts { get; } = [];

        public DiffResultModel? Result { get; private set; }

        public bool KeptPlace { get; private set; }

        public DiffHunkModel? Change { get; private set; }

        public (string Summary, string Position) Status { get; private set; }

        public bool CanNavigate { get; private set; }

        public List<string> Problems { get; } = [];

        public string? SavePath { get; set; }

        public DiffSaveChoice SaveAnswer { get; set; } = DiffSaveChoice.DontSave;

        public List<IReadOnlyList<string>> SavesAsked { get; } = [];

        public bool ReloadAnswer { get; set; } = true;

        public List<string> ReloadsAsked { get; } = [];

        public void ShowTitle(string title) => Title = title;

        public void ShowSides(string left, string right) => Sides = (left, right);

        public void ShowText(DiffSide side, IReadOnlyList<string> lines) => Texts[side] = lines;

        public void ShowDiff(DiffResultModel result, bool keepPlace)
        {
            Result = result;
            KeptPlace = keepPlace;
        }

        public void ShowChange(DiffHunkModel? change) => Change = change;

        public void ShowStatus(string summary, string position) => Status = (summary, position);

        public void ShowCanNavigate(bool canNavigate) => CanNavigate = canNavigate;

        public void ShowProblem(string message) => Problems.Add(message);

        public string? AskSavePath(DiffSide side) => SavePath;

        public DiffSaveChoice AskSaveChanges(IReadOnlyList<string> names)
        {
            SavesAsked.Add(names);

            return SaveAnswer;
        }

        public bool AskReload(string name)
        {
            ReloadsAsked.Add(name);

            return ReloadAnswer;
        }

        public void ChooseFiles(DiffFilesModel files) => FilesChosen?.Invoke(this, files);

        public void Paste(DiffPasteModel paste) => TextPasted?.Invoke(this, paste);

        public void Edit(DiffSide side, IReadOnlyList<string> lines)
        {
            Texts[side] = lines;
            TextEdited?.Invoke(this, new DiffEditModel(side, lines));
        }

        public void Save(DiffSide side) => SaveRequested?.Invoke(this, side);

        public void Swap() => SwapRequested?.Invoke(this, EventArgs.Empty);

        public void Reload() => ReloadRequested?.Invoke(this, EventArgs.Empty);

        public void Activate() => WindowActivated?.Invoke(this, EventArgs.Empty);

        public bool Close()
        {
            var e = new CancelEventArgs();
            CloseRequested?.Invoke(this, e);

            return e.Cancel;
        }

        public void SetOptions(DiffOptionsModel options) => OptionsChanged?.Invoke(this, options);

        public void Next(int fromRow) => NextChangeRequested?.Invoke(this, fromRow);

        public void Previous(int fromRow) => PreviousChangeRequested?.Invoke(this, fromRow);
    }
}
