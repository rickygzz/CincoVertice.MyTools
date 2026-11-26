using System.Drawing;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Services;
using CincoVertice.MyTools.Ui.Win.JsonEditor;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.JsonEditor;

public class JsonTextControlTests
{
    private static readonly string[] _json =
    [
        "{",
        "  \"people\": [",
        "    {",
        "      \"name\": \"Ann\"",
        "    }",
        "  ],",
        "  \"count\": 1",
        "}",
    ];

    // As the presenter does: the document read again after each edit
    private static JsonTextControl Create()
    {
        var control = new JsonTextControl { Width = 500, Height = 300 };
        control.TextEdited += (_, _) => control.ShowDocument(JsonDocumentReader.Read(control.Lines));
        control.SetText(_json);
        control.ShowDocument(JsonDocumentReader.Read(control.Lines));

        return control;
    }

    [Fact]
    public void Toggle_CollapsesASection_ToItsFirstLine_AndOpensItAgain()
    {
        // Arrange
        using JsonTextControl control = Create();

        // Act
        control.Toggle(1);
        IReadOnlyList<int> collapsed = [.. control.ShownLines];
        control.Toggle(1);

        // Assert: the array's lines, its closing one too, hidden
        Assert.Equal([0, 1, 6, 7], collapsed);
        Assert.Equal(8, control.ShownLines.Count);
    }

    [Fact]
    public void Menu_CollapsesTheSectionClickedIn_ThenExpandsIt_FromItsFirstLine()
    {
        StaThread.Run(() =>
        {
            // Arrange
            using JsonTextControl control = Create();

            // Act: on Ann's name, in the object over lines 2 to 4
            Assert.Contains("Co&llapse section", control.MenuAt(new DiffPosition(3, 8)));
            control.ClickMenu("Co&llapse section");
            IReadOnlyList<int> collapsed = [.. control.Collapsed];
            IReadOnlyList<string> menu = control.MenuAt(new DiffPosition(2, 2));
            control.ClickMenu("E&xpand section");

            // Assert
            Assert.Equal([2], collapsed);
            Assert.Contains("E&xpand section", menu);
            Assert.Contains("&Expand all", menu);
            Assert.Empty(control.Collapsed);
        });
    }

    [Fact]
    public void CtrlMinus_CollapsesTheSectionOfTheCaret_ThenTheOneAroundIt_AndCtrlPlusOpensItAgain()
    {
        // Arrange: the caret on Ann's name, in the object over lines 2 to 4, in the array over 1 to 5
        using JsonTextControl control = Create();
        control.Select(new JsonMatchModel(new DiffPosition(3, 8), new DiffPosition(3, 8)));

        // Act
        control.HandleKey(Keys.Control | Keys.OemMinus);
        IReadOnlyList<int> once = [.. control.Collapsed.Order()];
        control.HandleKey(Keys.Control | Keys.Subtract);
        IReadOnlyList<int> twice = [.. control.Collapsed.Order()];
        control.HandleKey(Keys.Control | Keys.Oemplus);

        // Assert: the caret goes to each first line, so the next press takes the next one out
        Assert.Equal([2], once);
        Assert.Equal([1, 2], twice);
        Assert.Equal([2], control.Collapsed);
        Assert.Equal(1, control.Caret.Line);
    }

    [Fact]
    public void CtrlShiftMinus_CollapsesAll_AndCtrlShiftPlusExpandsAll()
    {
        // Arrange
        using JsonTextControl control = Create();

        // Act
        control.HandleKey(Keys.Control | Keys.Shift | Keys.OemMinus);
        IReadOnlyList<int> collapsed = [.. control.Collapsed.Order()];
        control.HandleKey(Keys.Control | Keys.Shift | Keys.Add);

        // Assert
        Assert.Equal([1, 2], collapsed);
        Assert.Empty(control.Collapsed);
    }

    [Fact]
    public void Menu_InASelection_AsksForItsValues_AndSaysHowMany()
    {
        StaThread.Run(() =>
        {
            // Arrange: the presenter's answer, as many values as the selection holds
            using JsonTextControl control = Create();
            (DiffPosition, DiffPosition)? asked = null;
            control.PathRequested += (_, request) =>
            {
                asked = request.Selection;
                (request.Value, request.ValueCount) = request.Selection is null ? ("1", 1) : ("1,\n2,\n3", 3);
                (request.NameAndValue, request.NameAndValueCount) =
                    request.Selection is null ? (null, 0) : ("\"a\": 1", 2);
            };

            // Act
            IReadOnlyList<string> one = control.MenuAt(new DiffPosition(6, 12));
            control.HandleKey(Keys.Control | Keys.A);
            IReadOnlyList<string> several = control.MenuAt(new DiffPosition(3, 8));

            // Assert: one value, whose name the presenter didn't give; then the selection's
            Assert.Contains("Copy &value", one);
            Assert.DoesNotContain("Copy &name and value", one);
            Assert.Contains("Copy &values (3)", several);
            Assert.Contains("Copy &names and values (2)", several);
            Assert.Equal((new DiffPosition(0, 0), new DiffPosition(7, 1)), asked);
        });
    }

    [Fact]
    public void ShowTheme_Dark_DrawsTheTextDark_AndLightAgain()
    {
        // Arrange
        using JsonTextControl control = Create();

        // Act
        control.ShowTheme(isDark: true);
        Color dark = control.BackColor;
        control.ShowTheme(isDark: false);

        // Assert
        Assert.True(dark.GetBrightness() < 0.2f);
        Assert.Equal(Color.White, control.BackColor);
        Assert.False(control.IsDark);
    }

    [Fact]
    public void Form_ShowTheme_RecolorsTheTextToo()
    {
        // Arrange
        using var form = new JsonEditorForm(isDark: false);

        // Act
        form.ShowTheme(true);

        // Assert
        Assert.True(form.IsDark);
        Assert.True(form.TextControl.IsDark);
    }

    [Fact]
    public void Select_InACollapsedSection_OpensIt()
    {
        // Arrange
        using JsonTextControl control = Create();
        control.Toggle(1);

        // Act
        control.Select(new JsonMatchModel(new DiffPosition(3, 15), new DiffPosition(3, 18)));

        // Assert
        Assert.Empty(control.Collapsed);
        Assert.Equal(new DiffPosition(3, 18), control.Caret);
    }

    [Fact]
    public void CollapseAll_LeavesTheOutermostOpen()
    {
        // Arrange
        using JsonTextControl control = Create();

        // Act
        control.CollapseAll();

        // Assert
        Assert.Equal([1, 2], control.Collapsed.Order());
        Assert.Equal([0, 1, 6, 7], control.ShownLines);
    }

    [Fact]
    public void Enter_AfterAnOpeningBracket_IndentsOneStepMore()
    {
        // Arrange
        using JsonTextControl control = Create();
        control.Select(new JsonMatchModel(new DiffPosition(1, 13), new DiffPosition(1, 13)));

        // Act
        control.HandleKey(Keys.Enter);

        // Assert
        Assert.Equal("    ", control.Lines[2]);
        Assert.Equal(new DiffPosition(2, 4), control.Caret);
    }

    [Fact]
    public void ReplaceAll_IsOneEdit_ThatUndoTakesBack()
    {
        // Arrange
        using JsonTextControl control = Create();

        // Act
        control.ReplaceAll("{\"count\":1}");
        string replaced = string.Join("\n", control.Lines);
        control.HandleKey(Keys.Control | Keys.Z);

        // Assert
        Assert.Equal("{\"count\":1}", replaced);
        Assert.Equal(_json, control.Lines);
    }

    [Fact]
    public void ALineAddedAbove_KeepsACollapsedSectionCollapsed()
    {
        // Arrange
        using JsonTextControl control = Create();
        control.Toggle(1);
        control.Select(new JsonMatchModel(new DiffPosition(0, 1), new DiffPosition(0, 1)));

        // Act: a line after the {
        control.HandleKey(Keys.Enter);

        // Assert
        Assert.Equal([2], control.Collapsed);
        Assert.DoesNotContain(3, control.ShownLines);
    }
}
