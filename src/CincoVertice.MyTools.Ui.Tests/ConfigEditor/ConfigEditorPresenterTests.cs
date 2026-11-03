using System.ComponentModel;
using CincoVertice.Common.Utils.Highlighting;
using CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.ConfigEditor.Presenters;
using CincoVertice.MyTools.Ui.ConfigEditor.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.ConfigEditor;

public class ConfigEditorPresenterTests
{
    private static readonly EditableFileModel _hotkeys = new("hotkeys.cfg", "hotkeys.cfg", EditableFileKind.Hotkeys);
    private static readonly EditableFileModel _script = new("a.mts", "a.mts", EditableFileKind.Script);

    private readonly FakeView _view = new();
    private readonly FakeCatalog _catalog = new();
    private readonly FakeStore _store = new();
    private readonly FakeLanguage _language = new();
    private readonly ConfigEditorPresenter _presenter;

    public ConfigEditorPresenterTests()
    {
        _store.Files[_hotkeys.Path] = "A = 1";
        _store.Files[_script.Path] = "x = 1";
        _presenter = new ConfigEditorPresenter(_view, _catalog, _store, _language);
    }

    [Fact]
    public void Open_ShowsTheFile_WithItsColorsAndProblems()
    {
        // Arrange
        _language.Problems = [new EditableFileProblemModel(1, "bad")];

        // Act
        _presenter.Open(_script.Path);

        // Assert
        Assert.Equal("x = 1", _view.CodeText);
        Assert.Equal(_script, _view.SelectedFile);
        Assert.Equal(EditableFileKind.Script, _language.LastKind);
        Assert.Single(_view.Spans);
        Assert.Equal("bad", Assert.Single(_view.Problems).Message);
        Assert.Equal("a.mts - " + ConfigEditorPresenter.WindowTitle, _view.Title);
        Assert.True(_view.CanSave);
    }

    [Fact]
    public void Open_WithoutAPath_ShowsTheFirst()
    {
        _presenter.Open(null);

        Assert.Equal(_hotkeys, _view.SelectedFile);
    }

    [Fact]
    public void Open_TheSameFile_KeepsItsChanges()
    {
        // Arrange
        _presenter.Open(_script.Path);
        _view.CodeText = "changed";

        // Act
        _presenter.Open(_script.Path);

        // Assert
        Assert.Equal("changed", _view.CodeText);
        Assert.Equal(0, _view.SaveQuestions);
    }

    [Fact]
    public void Open_AFileThatCantBeRead_ReportsIt_AndCantBeSaved()
    {
        // Arrange
        _store.ReadError = new IOException("no way");

        // Act
        _presenter.Open(_script.Path);

        // Assert
        Assert.Contains("no way", _view.Error);
        Assert.False(_view.CanSave);
        Assert.Empty(_view.Spans);

        // Act: nothing to overwrite the file with
        _store.ReadError = null;
        _view.RaiseSave();

        // Assert
        Assert.Equal("x = 1", _store.Files[_script.Path]);
    }

    [Fact]
    public void TextEdited_MarksTheTitle_UntilSaved()
    {
        // Arrange
        _presenter.Open(_script.Path);
        _view.CodeText = "x = 2";

        // Act
        _view.RaiseEdited();

        // Assert
        Assert.StartsWith("a.mts*", _view.Title);

        // Act
        _view.RaiseSave();

        // Assert
        Assert.Equal("x = 2", _store.Files[_script.Path]);
        Assert.StartsWith("a.mts -", _view.Title);
    }

    [Fact]
    public void TextEdited_BackToTheSavedText_IsNotModified()
    {
        // Arrange: as with undo
        _presenter.Open(_script.Path);
        _view.CodeText = "x = 2";
        _view.RaiseEdited();
        _view.CodeText = "x = 1";

        // Act
        _view.RaiseEdited();

        // Assert
        Assert.StartsWith("a.mts -", _view.Title);
    }

    [Fact]
    public void Save_KeepsHowTheFileWasWritten()
    {
        // Arrange
        _store.Formats[_script.Path] = ("\r\n", true);
        _presenter.Open(_script.Path);
        _view.CodeText = "x = 2";

        // Act
        _view.RaiseSave();

        // Assert
        Assert.Equal(("\r\n", true), _store.Formats[_script.Path]);
    }

    [Fact]
    public void Save_WhenItFails_ReportsIt_AndStaysModified()
    {
        // Arrange
        _presenter.Open(_script.Path);
        _view.CodeText = "x = 2";
        _store.WriteError = new UnauthorizedAccessException("denied");

        // Act
        _view.RaiseSave();
        _view.RaiseEdited();

        // Assert
        Assert.Contains("denied", _view.Error);
        Assert.StartsWith("a.mts*", _view.Title);
    }

    [Theory]
    [InlineData(true, "mine")]
    [InlineData(false, "theirs")]
    public void Save_AFileChangedElsewhere_AsksFirst(bool overwrite, string expected)
    {
        // Arrange
        _presenter.Open(_script.Path);
        _view.CodeText = "mine";
        _store.Files[_script.Path] = "theirs";
        _view.OverwriteAnswer = overwrite;

        // Act
        _view.RaiseSave();

        // Assert
        Assert.Equal(1, _view.OverwriteQuestions);
        Assert.Equal(expected, _store.Files[_script.Path]);
    }

    [Fact]
    public void Save_AFileRemovedElsewhere_AsksFirst_AndBringsItBack()
    {
        // Arrange
        _presenter.Open(_script.Path);
        _view.CodeText = "mine";
        _store.Files.Remove(_script.Path);

        // Act
        _view.RaiseSave();

        // Assert
        Assert.Equal(1, _view.OverwriteQuestions);
        Assert.Equal("mine", _store.Files[_script.Path]);
    }

    [Theory]
    [InlineData(ConfigEditorSaveChoice.Save, "changed", "A = 1")]
    [InlineData(ConfigEditorSaveChoice.Discard, "x = 1", "A = 1")]
    [InlineData(ConfigEditorSaveChoice.Cancel, "x = 1", "changed")]
    public void FileSelected_WithChanges_AsksFirst(ConfigEditorSaveChoice choice, string savedScript, string shown)
    {
        // Arrange
        _presenter.Open(_script.Path);
        _view.CodeText = "changed";
        _view.SaveAnswer = choice;

        // Act
        _view.RaiseFileSelected(_hotkeys);

        // Assert
        Assert.Equal(1, _view.SaveQuestions);
        Assert.Equal(savedScript, _store.Files[_script.Path]);
        Assert.Equal(shown, _view.CodeText);
        Assert.Equal(choice == ConfigEditorSaveChoice.Cancel ? _script : _hotkeys, _view.SelectedFile);
    }

    [Fact]
    public void FileSelected_WhenSavingFails_StaysOnTheFile()
    {
        // Arrange
        _presenter.Open(_script.Path);
        _view.CodeText = "changed";
        _view.SaveAnswer = ConfigEditorSaveChoice.Save;
        _store.WriteError = new IOException("full");

        // Act
        _view.RaiseFileSelected(_hotkeys);

        // Assert
        Assert.Equal("changed", _view.CodeText);
        Assert.Equal(_script, _view.SelectedFile);
    }

    [Theory]
    [InlineData(ConfigEditorSaveChoice.Save, false)]
    [InlineData(ConfigEditorSaveChoice.Discard, false)]
    [InlineData(ConfigEditorSaveChoice.Cancel, true)]
    public void Closing_WithChanges_AsksFirst(ConfigEditorSaveChoice choice, bool staysOpen)
    {
        // Arrange
        _presenter.Open(_script.Path);
        _view.CodeText = "changed";
        _view.SaveAnswer = choice;

        // Act & Assert
        Assert.Equal(staysOpen, _view.RaiseClosing());
    }

    [Fact]
    public void Closing_WithoutChanges_DoesntAsk()
    {
        // Arrange
        _presenter.Open(_script.Path);

        // Act & Assert
        Assert.False(_view.RaiseClosing());
        Assert.Equal(0, _view.SaveQuestions);
    }

    [Fact]
    public void NewScript_OpensTheNewFile()
    {
        // Arrange
        _presenter.Open(_hotkeys.Path);
        _view.NewScriptName = "b";

        // Act
        _view.RaiseNewScript();

        // Assert
        Assert.Equal("b.mts", _view.SelectedFile?.Name);
        Assert.Contains(_view.ShownFiles, file => file.Name == "b.mts");
    }

    [Fact]
    public void NewScript_WithABadName_ReportsIt_AndStays()
    {
        // Arrange
        _presenter.Open(_hotkeys.Path);
        _view.NewScriptName = "b";
        _catalog.CreateError = "bad name";

        // Act
        _view.RaiseNewScript();

        // Assert
        Assert.Equal("bad name", _view.Error);
        Assert.Equal(_hotkeys, _view.SelectedFile);
    }

    [Fact]
    public void NewScript_Cancelled_DoesntAskAboutChanges()
    {
        // Arrange
        _presenter.Open(_hotkeys.Path);
        _view.CodeText = "changed";
        _view.NewScriptName = null;

        // Act
        _view.RaiseNewScript();

        // Assert
        Assert.Equal(0, _view.SaveQuestions);
        Assert.Equal("changed", _view.CodeText);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 7)]
    public void ProblemSelected_GoesToItsLine(int index, int line)
    {
        // Arrange
        _language.Problems = [new EditableFileProblemModel(3, "a"), new EditableFileProblemModel(7, "b")];
        _presenter.Open(_script.Path);

        // Act
        _view.RaiseProblemSelected(index);

        // Assert
        Assert.Equal(line, _view.LineShown);
    }

    [Fact]
    public void ProblemSelected_OutOfRange_IsIgnored()
    {
        // Arrange
        _presenter.Open(_script.Path);

        // Act
        _view.RaiseProblemSelected(5);

        // Assert
        Assert.Null(_view.LineShown);
    }

    [Fact]
    public void Activated_AFileChangedElsewhere_ShowsTheNewText()
    {
        // Arrange
        _presenter.Open(_script.Path);
        _store.Files[_script.Path] = "theirs";

        // Act
        _view.RaiseActivated();

        // Assert
        Assert.Equal("theirs", _view.CodeText);
    }

    [Fact]
    public void Activated_WithChanges_KeepsThem()
    {
        // Arrange
        _presenter.Open(_script.Path);
        _view.CodeText = "mine";
        _store.Files[_script.Path] = "theirs";

        // Act
        _view.RaiseActivated();

        // Assert
        Assert.Equal("mine", _view.CodeText);
    }

    private sealed class FakeView : IConfigEditorView
    {
        public event EventHandler<EditableFileModel>? FileSelected;

        public event EventHandler? TextEdited;

        public event EventHandler? SaveRequested;

        public event EventHandler? NewScriptRequested;

        public event EventHandler<int>? ProblemSelected;

        public event EventHandler? ViewActivated;

        public event EventHandler<CancelEventArgs>? CloseRequested;

        public string CodeText { get; set; } = string.Empty;

        public IReadOnlyList<EditableFileModel> ShownFiles { get; private set; } = [];

        public EditableFileModel? SelectedFile { get; private set; }

        public IReadOnlyList<CodeSpan> Spans { get; private set; } = [];

        public IReadOnlyList<EditableFileProblemModel> Problems { get; private set; } = [];

        public string Title { get; private set; } = string.Empty;

        public bool CanSave { get; private set; }

        public string? Error { get; private set; }

        public int? LineShown { get; private set; }

        public int SaveQuestions { get; private set; }

        public int OverwriteQuestions { get; private set; }

        public ConfigEditorSaveChoice SaveAnswer { get; set; }

        public bool OverwriteAnswer { get; set; } = true;

        public string? NewScriptName { get; set; }

        public void ShowFiles(IReadOnlyList<EditableFileModel> files, EditableFileModel? selected)
        {
            ShownFiles = files;
            SelectedFile = selected;
        }

        public void ShowCode(string text) => CodeText = text;

        public void ShowHighlights(IReadOnlyList<CodeSpan> spans) => Spans = spans;

        public void ShowProblems(IReadOnlyList<EditableFileProblemModel> problems) => Problems = problems;

        public void ShowTitle(string title) => Title = title;

        public void ShowCanSave(bool canSave) => CanSave = canSave;

        public void ShowError(string message) => Error = message;

        public void GoToLine(int line) => LineShown = line;

        public ConfigEditorSaveChoice AskSaveChanges(string name)
        {
            SaveQuestions++;

            return SaveAnswer;
        }

        public bool AskOverwrite(string name)
        {
            OverwriteQuestions++;

            return OverwriteAnswer;
        }

        public string? AskNewScriptName() => NewScriptName;

        public void RaiseFileSelected(EditableFileModel file) => FileSelected?.Invoke(this, file);

        public void RaiseEdited() => TextEdited?.Invoke(this, EventArgs.Empty);

        public void RaiseSave() => SaveRequested?.Invoke(this, EventArgs.Empty);

        public void RaiseNewScript() => NewScriptRequested?.Invoke(this, EventArgs.Empty);

        public void RaiseProblemSelected(int index) => ProblemSelected?.Invoke(this, index);

        public void RaiseActivated() => ViewActivated?.Invoke(this, EventArgs.Empty);

        public bool RaiseClosing()
        {
            var args = new CancelEventArgs();
            CloseRequested?.Invoke(this, args);

            return args.Cancel;
        }
    }

    private sealed class FakeCatalog : IEditableFileCatalog
    {
        private readonly List<EditableFileModel> _files = [_hotkeys, _script];

        public string? CreateError { get; set; }

        public IReadOnlyList<EditableFileModel> GetFiles() => _files;

        public bool TryCreateScript(string name, out EditableFileModel? file, out string error)
        {
            error = CreateError ?? string.Empty;
            file = null;

            if (CreateError is not null)
            {
                return false;
            }

            file = new EditableFileModel(name + ".mts", name + ".mts", EditableFileKind.Script);
            _files.Add(file);

            return true;
        }
    }

    private sealed class FakeStore : IEditableFileStore
    {
        public Dictionary<string, string> Files { get; } = [];

        public Dictionary<string, (string NewLine, bool HasBom)> Formats { get; } = [];

        public Exception? ReadError { get; set; }

        public Exception? WriteError { get; set; }

        public EditableFileContentModel Read(string path)
        {
            if (ReadError is not null)
            {
                throw ReadError;
            }

            if (!Files.TryGetValue(path, out string? text))
            {
                throw new FileNotFoundException(path);
            }

            (string newLine, bool hasBom) = Formats.GetValueOrDefault(path, ("\n", false));

            return new EditableFileContentModel(text, newLine, hasBom);
        }

        public void Write(string path, EditableFileContentModel content)
        {
            if (WriteError is not null)
            {
                throw WriteError;
            }

            Files[path] = content.Text;
            Formats[path] = (content.NewLine, content.HasBom);
        }
    }

    private sealed class FakeLanguage : IEditableFileLanguage
    {
        public IReadOnlyList<EditableFileProblemModel> Problems { get; set; } = [];

        public EditableFileKind? LastKind { get; private set; }

        public IReadOnlyList<CodeSpan> Highlight(EditableFileKind kind, string text)
        {
            LastKind = kind;

            return [new CodeSpan(0, 1, CodeSpanKind.Keyword)];
        }

        public IReadOnlyList<EditableFileProblemModel> Check(EditableFileKind kind, string text) => Problems;
    }
}
