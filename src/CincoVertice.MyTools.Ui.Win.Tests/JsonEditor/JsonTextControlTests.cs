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
