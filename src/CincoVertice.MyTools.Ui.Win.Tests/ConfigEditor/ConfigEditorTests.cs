using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using CincoVertice.Common.Ui.Win.Controls.Rtf;
using CincoVertice.Common.Ui.Win.Controls.Rtf.Core;
using CincoVertice.Common.Utils.Highlighting;
using CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.ConfigEditor.Presenters;
using CincoVertice.MyTools.Ui.ConfigEditor.Services;
using CincoVertice.MyTools.Ui.Win.ConfigEditor;
using CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;
using CincoVertice.MyTools.Ui.Win.Setup;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.ConfigEditor;

public sealed class ConfigEditorTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ConfigEditorTests_" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private EditableFileCatalog CreateCatalog() => new(
        new HotkeyConfigService(Path.Combine(_folder, "hotkeys.cfg")),
        new ActivitySettingsService(Path.Combine(_folder, "activity.cfg")),
        new StartupConfigService(Path.Combine(_folder, "startup.cfg")),
        () => [new NamedAction("Known")],
        Path.Combine(_folder, "scripts"));

    private static EditableFileLanguage CreateLanguage() => new(() => ["Known"]);

    [Fact]
    public void Setup_TheEditorAndTheActionsThatOpenIt_CanBeCreated()
    {
        // Arrange: the editor reads the actions, and some of the actions open the editor
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddMyToolsUiWinLayer();
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        ConfigEditorWindowService editor = provider.GetRequiredService<ConfigEditorWindowService>();
        var catalog = provider.GetRequiredService<AppActionCatalog>();
        var language = provider.GetRequiredService<IEditableFileLanguage>();

        // Assert
        Assert.NotNull(editor);
        Assert.NotNull(provider.GetRequiredService<CincoVertice.MyTools.Ui.Shell.Interfaces.IStartWithWindows>());
        Assert.NotNull(provider.GetRequiredService<StartupActionsService>());
        Assert.Contains(catalog.GetActions(), action => action.Name == "EditScripts");
        Assert.Empty(language.Check(EditableFileKind.Hotkeys, "EditScripts = Ctrl+Alt+F12"));
    }

    [Fact]
    public void Language_Hotkeys_GivesTheLinesOfItsProblems()
    {
        // Act
        var problems = CreateLanguage().Check(
            EditableFileKind.Hotkeys,
            "# top\nKnown = Ctrl+Alt+1\n\nOther = Ctrl+Alt+2\nnot a line\nKnown = Ctrl+Alt+3");

        // Assert: in the order of the file, and with the line only once in the text
        Assert.Equal([4, 5, 6], problems.Select(problem => problem.Line));
        Assert.DoesNotContain("Line", problems[1].Message);
    }

    [Fact]
    public void Readers_StillNameTheLine_WhereOnlyTheMessageIsShown()
    {
        // The tray's Hotkey problems and activity's notification have only the text
        Assert.Contains(
            "Line 1:",
            HotkeyConfigReader.Read("not a line", ["Known"]).Problems[0]);
        Assert.Contains("Line 1:", ActivitySettingsReader.Read("not a line").Problems[0]);
        Assert.Contains("Line 1:", StartupConfigReader.Read("not a line", ["Known"]).Problems[0]);
    }

    [Theory]
    [InlineData("Interval = 1\nSpeedMin = 30", 1)]
    [InlineData("# top\n\nNope = 1", 3)]
    [InlineData("Interval = 1000\nSpeedMin = 80\nSpeedMax = 40", 2)]
    [InlineData("Interval = 1000\nSpeedMin = 101", 2)]
    public void Language_Activity_GivesTheLinesOfItsProblems(string text, int line)
    {
        // Act
        var problems = CreateLanguage().Check(EditableFileKind.ActivitySettings, text);

        // Assert
        Assert.Equal(line, problems[0].Line);
    }

    [Fact]
    public void Language_Startup_GivesTheLinesOfItsProblems()
    {
        // Act
        var problems = CreateLanguage().Check(EditableFileKind.StartupActions, "# top\nRun = Known\nRun = Other");

        // Assert
        Assert.Equal(3, Assert.Single(problems).Line);
    }

    [Fact]
    public void Language_Script_GivesTheErrorsPlace()
    {
        // Act
        EditableFileProblemModel problem = Assert.Single(
            CreateLanguage().Check(EditableFileKind.Script, "x = 1\ny = \"open"));

        // Assert
        Assert.Equal(2, problem.Line);
        Assert.Equal(5, problem.Column);
    }

    [Fact]
    public void Language_ColorsEachKindItsOwnWay()
    {
        // Arrange
        EditableFileLanguage language = CreateLanguage();

        // Act & Assert
        Assert.Contains(language.Highlight(EditableFileKind.Script, "if x"), span => span.Kind == CodeSpanKind.Keyword);
        Assert.Contains(language.Highlight(EditableFileKind.Hotkeys, "if = x"), span => span.Kind == CodeSpanKind.Key);
    }

    [Fact]
    public void Catalog_ListsTheConfigFilesThenTheScripts_CreatingWhatsMissing()
    {
        // Act
        IReadOnlyList<EditableFileModel> files = CreateCatalog().GetFiles();

        // Assert
        Assert.Equal("hotkeys.cfg", files[0].Name);
        Assert.Equal(EditableFileKind.Hotkeys, files[0].Kind);
        Assert.Equal("activity.cfg", files[1].Name);
        Assert.Equal("startup.cfg", files[2].Name);
        Assert.Equal(EditableFileKind.StartupActions, files[2].Kind);
        Assert.True(File.Exists(files[0].Path));
        Assert.True(File.Exists(files[1].Path));
        Assert.True(File.Exists(files[2].Path));
        Assert.Contains(files, file => file.Kind == EditableFileKind.Script);
    }

    [Theory]
    [InlineData("New", "New.mts")]
    [InlineData(" New.mts ", "New.mts")]
    [InlineData("_Library", "_Library.mts")]
    public void Catalog_CreatesAScript(string name, string fileName)
    {
        // Act
        bool created = CreateCatalog().TryCreateScript(name, out EditableFileModel? file, out _);

        // Assert
        Assert.True(created);
        Assert.Equal(fileName, file!.Name);
        Assert.Equal(EditableFileKind.Script, file.Kind);
        Assert.True(File.Exists(file.Path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    [InlineData("a/b")]
    [InlineData("..\\evil")]
    public void Catalog_RefusesABadName(string name)
    {
        // Act
        bool created = CreateCatalog().TryCreateScript(name, out EditableFileModel? file, out string error);

        // Assert
        Assert.False(created);
        Assert.Null(file);
        Assert.NotEmpty(error);
    }

    [Theory]
    [InlineData("Known")]
    [InlineData("known")]
    [InlineData("Kn-own")]
    [InlineData("Google Search")]
    public void Catalog_RefusesANameWhoseActionIsTaken(string name)
    {
        // Arrange: Known is a built-in action, and the first run adds GoogleSearch.mts
        EditableFileCatalog catalog = CreateCatalog();
        catalog.GetFiles();

        // Act
        bool created = catalog.TryCreateScript(name, out EditableFileModel? file, out string error);

        // Assert: hotkeys.cfg could only ever run the other one
        Assert.False(created);
        Assert.Null(file);
        Assert.Contains("already", error);
    }

    [Fact]
    public void Catalog_ANewScriptSaysItsActionsName()
    {
        // Act
        CreateCatalog().TryCreateScript("My Script", out EditableFileModel? file, out _);

        // Assert
        Assert.Contains("MyScript", File.ReadAllText(file!.Path));
    }

    [Fact]
    public void Catalog_ANewLibrary_HasNoAction_SoItsNameIsOnlyTakenByAFile()
    {
        // Arrange
        EditableFileCatalog catalog = CreateCatalog();

        // Act
        bool created = catalog.TryCreateScript("_Known", out EditableFileModel? file, out _);

        // Assert: and it doesn't offer a hotkey it can't have
        Assert.True(created);
        Assert.DoesNotContain("hotkeys.cfg", File.ReadAllText(file!.Path));
    }

    [Fact]
    public void Catalog_DoesntReplaceAScript()
    {
        // Arrange
        EditableFileCatalog catalog = CreateCatalog();
        catalog.TryCreateScript("Same", out EditableFileModel? first, out _);
        File.WriteAllText(first!.Path, "mine");

        // Act
        bool created = catalog.TryCreateScript("same", out _, out string error);

        // Assert
        Assert.False(created);
        Assert.Contains("already", error);
        Assert.Equal("mine", File.ReadAllText(first.Path));
    }

    [Fact]
    public void Form_ShowsARealFile_WithoutLookingChanged()
    {
        // Arrange: new lines of every kind, a tab and an accent: the text box must give back the same text
        Directory.CreateDirectory(_folder);
        string path = Path.Combine(_folder, "hotkeys.cfg");
        File.WriteAllText(path, "# ñ\r\n\tKnown = Ctrl+Alt+1\r\n\r\n");

        using var form = new ConfigEditorForm();
        var catalog = CreateCatalog();
        var presenter = new ConfigEditorPresenter(form, catalog, new EditableFileStore(), CreateLanguage());

        // Act
        presenter.Open(path);

        // Assert
        Assert.StartsWith("hotkeys.cfg - ", form.Text);
    }

    [Fact]
    public void Form_SavingKeepsTheFilesNewLines()
    {
        // Arrange
        Directory.CreateDirectory(_folder);
        string path = Path.Combine(_folder, "hotkeys.cfg");
        const string text = "# top\r\nKnown = Ctrl+Alt+1\r\n";
        File.WriteAllText(path, text);

        using var form = new ConfigEditorForm();
        var presenter = new ConfigEditorPresenter(form, CreateCatalog(), new EditableFileStore(), CreateLanguage());
        presenter.Open(path);

        // Act: Ctrl+S
        var message = default(Message);
        TestForm.PressSave(form, ref message);

        // Assert
        Assert.Equal(text, File.ReadAllText(path));
    }

    private const int WM_MOUSEWHEEL = 0x020A;
    private const int MK_CONTROL = 0x0008;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

    /// <summary>Ctrl+wheel, one step toward the user's screen, as Windows sends it.</summary>
    private static void CtrlWheel(Control control)
    {
        Point center = control.PointToScreen(new Point(control.Width / 2, control.Height / 2));

        _ = SendMessage(control.Handle, WM_MOUSEWHEEL, (120 << 16) | MK_CONTROL, (center.Y << 16) | (center.X & 0xFFFF));
    }

    private static Form ShowControl(CodeTextControl control)
    {
        var form = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 100),
            ShowInTaskbar = false,
            ClientSize = new Size(500, 300),
        };

        control.Dock = DockStyle.Fill;
        form.Controls.Add(control);
        form.Show();

        return form;
    }

    [Fact]
    public void Zoom_CtrlWheel_IsReported_AndResetGoesBack()
    {
        // Arrange
        using var control = new CodeTextControl();
        using Form form = ShowControl(control);
        control.SetCode("if x\nend");
        int changes = 0;
        control.ZoomChanged += (_, _) => changes++;

        // Act
        CtrlWheel(control);

        // Assert
        Assert.True(control.ZoomPercent > 100);
        Assert.Equal(1, changes);

        // Act
        control.ResetZoom();

        // Assert
        Assert.Equal(100, control.ZoomPercent);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void Zoom_NumbersGrowWithTheText_WhichStaysOutOfTheirWay()
    {
        // Arrange
        using var control = new CodeTextControl();
        using Form form = ShowControl(control);
        control.SetCode("if x\nend");
        int gutter = control.GutterWidth;

        // Act
        control.ZoomFactor = 3;
        CtrlWheel(control);

        // Assert
        Assert.True(control.GutterWidth > gutter);

        // Right after the numbers: RichTextBox zooms its margin too, which would leave a wide gap
        int textStart = control.GetPositionFromCharIndex(0).X;

        Assert.InRange(textStart, control.GutterWidth, control.GutterWidth + 16);

        // Act
        control.ResetZoom();

        // Assert
        Assert.Equal(gutter, control.GutterWidth);
    }

    [Fact]
    public void Zoom_ANewFile_KeepsIt()
    {
        // Arrange
        using var control = new CodeTextControl();
        using Form form = ShowControl(control);
        control.SetCode("if x");
        control.ZoomFactor = 2;
        CtrlWheel(control);
        int percent = control.ZoomPercent;

        // Act
        control.SetCode("other");
        CtrlWheel(control);

        // Assert
        Assert.True(control.ZoomPercent >= percent);
    }

    [Fact]
    public void Form_ShowsTheZoom_AndItsButtonAndCtrl0GoBackTo100()
    {
        // Arrange
        using var form = new ConfigEditorForm
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 100),
            ShowInTaskbar = false,
        };
        const System.Reflection.BindingFlags flags =
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var editor = (CodeTextControl)typeof(ConfigEditorForm).GetField("CodeEditor", flags)!.GetValue(form)!;
        var button = (ToolStripItem)typeof(ConfigEditorForm).GetField("ZoomButton", flags)!.GetValue(form)!;
        form.Show();
        editor.SetCode("if x");

        // Act
        CtrlWheel(editor);

        // Assert
        Assert.NotEqual("100%", button.Text);
        Assert.Equal($"{editor.ZoomPercent}%", button.Text);

        // Act: the button
        button.PerformClick();

        // Assert
        Assert.Equal("100%", button.Text);
        Assert.Equal(100, editor.ZoomPercent);

        // Act: Ctrl+0
        CtrlWheel(editor);
        typeof(ConfigEditorForm).GetMethod("ProcessCmdKey", flags)!.Invoke(form, [default(Message), Keys.Control | Keys.D0]);

        // Assert
        Assert.Equal("100%", button.Text);
    }

    [Fact]
    public void Colors_DontChangeTheText_OrWhatsSelected()
    {
        // Arrange
        using var control = new CodeTextControl { Size = new Size(400, 200) };
        _ = control.Handle;
        control.SetCode("if x\nend");
        control.Select(3, 1);

        // Act
        control.SetHighlights([new CodeSpan(0, 2, CodeSpanKind.Keyword)]);

        // Assert
        Assert.Equal("if x\nend", control.Text);
        Assert.Equal(3, control.SelectionStart);
        Assert.Equal(1, control.SelectionLength);

        control.Select(0, 2);
        Assert.Equal(Color.FromArgb(0, 0, 255).ToArgb(), control.SelectionColor.ToArgb());
        control.Select(3, 1);
        Assert.Equal(control.ForeColor.ToArgb(), control.SelectionColor.ToArgb());
    }

    [Fact]
    public void Colors_AfterAnEdit_AreRightBeforeAndAfterIt()
    {
        // Arrange: "if" and "end" colored, then a word typed between them, which takes the color next to it
        const string blue = "if";
        using var control = new CodeTextControl { Size = new Size(400, 200) };
        _ = control.Handle;
        control.SetCode($"{blue} x end");
        control.SetHighlights(
        [
            new CodeSpan(0, 2, CodeSpanKind.Keyword),
            new CodeSpan(5, 3, CodeSpanKind.Keyword),
        ]);
        control.Select(2, 0);
        control.SelectedText = "yy";

        // Act: the first and last spans are the same, the last one moved
        control.SetHighlights(
        [
            new CodeSpan(0, 2, CodeSpanKind.Keyword),
            new CodeSpan(7, 3, CodeSpanKind.Keyword),
        ]);

        // Assert
        int keyword = Color.FromArgb(0, 0, 255).ToArgb();

        control.Select(0, 2);
        Assert.Equal(keyword, control.SelectionColor.ToArgb());
        control.Select(2, 5);
        Assert.Equal(control.ForeColor.ToArgb(), control.SelectionColor.ToArgb());
        control.Select(7, 3);
        Assert.Equal(keyword, control.SelectionColor.ToArgb());
    }

    [Fact]
    public void Colors_ASpanThatsGone_LosesItsColor()
    {
        // Arrange
        using var control = new CodeTextControl { Size = new Size(400, 200) };
        _ = control.Handle;
        control.SetCode("if x");
        control.SetHighlights([new CodeSpan(0, 2, CodeSpanKind.Keyword)]);

        // Act
        control.SetHighlights([]);

        // Assert
        control.Select(0, 2);
        Assert.Equal(control.ForeColor.ToArgb(), control.SelectionColor.ToArgb());
    }

    [Fact]
    public void Colors_ANewFile_StartsOver()
    {
        // Arrange
        using var control = new CodeTextControl { Size = new Size(400, 200) };
        _ = control.Handle;
        control.SetCode("if x");
        control.SetHighlights([new CodeSpan(0, 2, CodeSpanKind.Keyword)]);

        // Act: same span as the old file, in a text that has no color yet
        control.SetCode("if y");
        control.SetHighlights([new CodeSpan(0, 2, CodeSpanKind.Keyword)]);

        // Assert
        control.Select(0, 2);
        Assert.Equal(Color.FromArgb(0, 0, 255).ToArgb(), control.SelectionColor.ToArgb());
    }

    [Fact]
    public void Colors_RaiseNoTextChanged()
    {
        // Arrange
        using var control = new CodeTextControl();
        _ = control.Handle;
        control.SetCode("if x");
        int changes = 0;
        control.TextChanged += (_, _) => changes++;

        // Act
        control.SetHighlights([new CodeSpan(0, 2, CodeSpanKind.Keyword)]);

        // Assert
        Assert.Equal(0, changes);
    }

    [Fact]
    public void Undo_GoesBackOverEdits_NotOverColors_AndRedoReturns()
    {
        // Arrange
        using var control = new CodeTextControl();
        _ = control.Handle;
        control.SetCode("if x");
        control.Select(4, 0);
        control.SelectedText = " y";
        control.SetHighlights([new CodeSpan(0, 2, CodeSpanKind.Keyword)]);

        // Act & Assert
        control.UndoEdit();
        Assert.Equal("if x", control.Text);

        control.UndoEdit();
        Assert.Equal("if x", control.Text);

        control.RedoEdit();
        Assert.Equal("if x y", control.Text);
    }

    [Fact]
    public void Undo_KeepsTheColorsOfTheRest()
    {
        // Arrange
        using var control = new CodeTextControl();
        _ = control.Handle;
        control.SetCode("if x");
        control.Select(4, 0);
        control.SelectedText = "y";
        control.SetHighlights([new CodeSpan(0, 2, CodeSpanKind.Keyword)]);

        // Act
        control.UndoEdit();

        // Assert
        control.Select(0, 2);
        Assert.Equal(Color.FromArgb(0, 0, 255).ToArgb(), control.SelectionColor.ToArgb());
    }

    [Fact]
    public void Undo_CantGoBackPastANewFile()
    {
        // Arrange
        using var control = new CodeTextControl();
        _ = control.Handle;
        control.SelectedText = "old";
        control.SetCode("new");

        // Act
        control.UndoEdit();

        // Assert
        Assert.Equal("new", control.Text);
    }

    [Fact]
    public void History_TypingAWord_IsOneStep()
    {
        // Arrange
        var history = new CodeUndoHistory();
        history.Reset("a ");

        // Act
        history.Record("a b", 3);
        history.Record("a bc", 4);
        history.Record("a bcd", 5);
        history.Record("a bcd ", 6);
        history.Record("a bcd e", 7);

        // Assert
        Assert.Equal("a bcd ", history.Undo()?.Text);
        Assert.Equal("a bcd", history.Undo()?.Text);
        Assert.Equal("a ", history.Undo()?.Text);
        Assert.Null(history.Undo());
    }

    [Fact]
    public void History_DeletingCharacters_IsOneStep_AndKeepsTheCaret()
    {
        // Arrange
        var history = new CodeUndoHistory();
        history.Reset("abc");
        history.Record("abcd", 4);

        // Act: backspace, backspace
        history.Record("abc", 3);
        history.Record("ab", 2);

        // Assert
        Assert.Equal(new CodeTextState("abcd", 4), history.Undo());
        Assert.Equal(new CodeTextState("abc", 0), history.Undo());
    }

    [Fact]
    public void History_AnEditAfterUndo_ForgetsRedo()
    {
        // Arrange
        var history = new CodeUndoHistory();
        history.Reset("a");
        history.Record("a b", 3);
        history.Undo();

        // Act
        history.Record("a c", 3);

        // Assert
        Assert.Null(history.Redo());
        Assert.Equal("a", history.Undo()?.Text);
    }

    [Fact]
    public void History_RecordingTheSameText_AddsNoStep()
    {
        // Arrange
        var history = new CodeUndoHistory();
        history.Reset("a");

        // Act
        history.Record("a", 1);

        // Assert
        Assert.Null(history.Undo());
    }

    private static class TestForm
    {
        public static void PressSave(ConfigEditorForm form, ref Message message)
        {
            typeof(ConfigEditorForm)
                .GetMethod("ProcessCmdKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(form, [message, Keys.Control | Keys.S]);
        }
    }

    private sealed class NamedAction : IAppAction
    {
        public NamedAction(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string? DefaultShortcut => null;

        public Task ExecuteAsync() => Task.CompletedTask;
    }
}
