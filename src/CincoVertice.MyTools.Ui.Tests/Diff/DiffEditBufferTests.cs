using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diff;

public class DiffEditBufferTests
{
    [Theory]
    [InlineData("😀")]
    [InlineData("e\u0301")]
    [InlineData("👩‍💻")]
    public void SelectingOrDeletingAnElement_NeverSplitsItsUtf16OrCombiningSequence(string element)
    {
        var buffer = new DiffEditBuffer();
        buffer.SetLines([element + " X"]);
        buffer.SelectWord(new DiffPosition(0, 0));
        Assert.Equal(element, buffer.SelectedText);
        buffer.DeleteForward(false);
        Assert.Equal([" X"], buffer.Lines);
        buffer.Undo();
        Assert.Equal([element + " X"], buffer.Lines);
        buffer.MoveTo(new DiffPosition(0, element.Length), false);
        buffer.DeleteBackward(false);
        Assert.Equal([" X"], buffer.Lines);
    }

    [Fact]
    public void APositionInsideASurrogatePair_IsClampedToACharacterBoundary()
    {
        var buffer = new DiffEditBuffer();
        buffer.SetLines(["😀x"]);
        buffer.MoveTo(new DiffPosition(0, 1), false);
        Assert.Equal(new DiffPosition(0, 0), buffer.Caret);
        buffer.DeleteForward(false);
        Assert.Equal(["x"], buffer.Lines);
    }
    private readonly DiffEditBuffer _buffer = new();

    [Fact]
    public void AnEmptyText_HasOneEmptyLine()
    {
        // Act
        _buffer.SetLines([]);

        // Assert
        Assert.Equal([string.Empty], _buffer.Lines);
    }

    [Fact]
    public void Typing_GoesAtTheCaret_AndMovesIt()
    {
        // Arrange
        _buffer.SetLines(["ac"]);
        _buffer.MoveTo(new DiffPosition(0, 1), extend: false);

        // Act
        _buffer.Insert("b");

        // Assert
        Assert.Equal(["abc"], _buffer.Lines);
        Assert.Equal(new DiffPosition(0, 2), _buffer.Caret);
    }

    [Fact]
    public void Insert_WithLineBreaksOfAnyKind_SplitsTheLine()
    {
        // Arrange
        _buffer.SetLines(["start end"]);
        _buffer.MoveTo(new DiffPosition(0, 6), extend: false);

        // Act
        _buffer.Insert("one\r\ntwo\rthree\n");

        // Assert
        Assert.Equal(["start one", "two", "three", "end"], _buffer.Lines);
        Assert.Equal(new DiffPosition(3, 0), _buffer.Caret);
    }

    [Fact]
    public void Insert_ReplacesTheSelection_AcrossLines()
    {
        // Arrange
        _buffer.SetLines(["abc", "def", "ghi"]);
        _buffer.Select(new DiffPosition(0, 1), new DiffPosition(2, 2));

        // Act
        string selected = _buffer.SelectedText;
        _buffer.Insert("X");

        // Assert
        Assert.Equal(string.Join(Environment.NewLine, "bc", "def", "gh"), selected);
        Assert.Equal(["aXi"], _buffer.Lines);
    }

    [Fact]
    public void Backspace_AtALinesStart_JoinsItToTheOneBefore()
    {
        // Arrange
        _buffer.SetLines(["ab", "cd"]);
        _buffer.MoveTo(new DiffPosition(1, 0), extend: false);

        // Act
        _buffer.DeleteBackward(word: false);

        // Assert
        Assert.Equal(["abcd"], _buffer.Lines);
        Assert.Equal(new DiffPosition(0, 2), _buffer.Caret);
    }

    [Fact]
    public void Delete_AtALinesEnd_JoinsTheNextOne()
    {
        // Arrange
        _buffer.SetLines(["ab", "cd"]);
        _buffer.MoveTo(new DiffPosition(0, 2), extend: false);

        // Act
        _buffer.DeleteForward(word: false);

        // Assert
        Assert.Equal(["abcd"], _buffer.Lines);
    }

    [Fact]
    public void WordMoves_SkipSpaces_AndStopAtPunctuation()
    {
        // Arrange
        _buffer.SetLines(["var total = price;"]);

        // Act
        DiffPosition right1 = _buffer.WordRight(new DiffPosition(0, 0));
        DiffPosition right2 = _buffer.WordRight(right1);
        DiffPosition left = _buffer.WordLeft(new DiffPosition(0, 17));

        // Assert
        Assert.Equal(new DiffPosition(0, 4), right1);
        Assert.Equal(new DiffPosition(0, 10), right2);
        Assert.Equal(new DiffPosition(0, 12), left);
    }

    [Fact]
    public void CtrlBackspace_DeletesTheWordBefore()
    {
        // Arrange
        _buffer.SetLines(["hello world"]);
        _buffer.MoveTo(new DiffPosition(0, 11), extend: false);

        // Act
        _buffer.DeleteBackward(word: true);

        // Assert
        Assert.Equal(["hello "], _buffer.Lines);
    }

    [Fact]
    public void TypingAWord_IsOneStepOfUndo_AndRedoPutsItBack()
    {
        // Arrange
        _buffer.SetLines(["x"]);
        _buffer.MoveTo(new DiffPosition(0, 1), extend: false);
        _buffer.Insert("a");
        _buffer.Insert("b");
        _buffer.Insert("c");

        // Act
        _buffer.Undo();
        IReadOnlyList<string> undone = [.. _buffer.Lines];
        _buffer.Redo();

        // Assert
        Assert.Equal(["x"], undone);
        Assert.Equal(["xabc"], _buffer.Lines);
        Assert.Equal(new DiffPosition(0, 4), _buffer.Caret);
    }

    [Fact]
    public void MovingTheCaret_StartsANewStepOfUndo()
    {
        // Arrange
        _buffer.SetLines([string.Empty]);
        _buffer.Insert("a");
        _buffer.MoveTo(new DiffPosition(0, 1), extend: false);
        _buffer.Insert("b");

        // Act
        _buffer.Undo();

        // Assert
        Assert.Equal(["a"], _buffer.Lines);
    }

    [Fact]
    public void Undo_PutsBackWhatWasSelected()
    {
        // Arrange
        _buffer.SetLines(["one", "two"]);
        _buffer.SelectAll();
        _buffer.Insert("new");

        // Act
        _buffer.Undo();

        // Assert
        Assert.Equal(["one", "two"], _buffer.Lines);
        Assert.Equal((new DiffPosition(0, 0), new DiffPosition(1, 3)), (_buffer.SelectionStart, _buffer.SelectionEnd));
    }

    [Fact]
    public void SelectWord_TakesTheWordAtThePlace()
    {
        // Arrange
        _buffer.SetLines(["call(some_name)"]);

        // Act
        _buffer.SelectWord(new DiffPosition(0, 7));

        // Assert
        Assert.Equal("some_name", _buffer.SelectedText);
    }

    [Fact]
    public void Positions_AreKeptInTheText()
    {
        // Arrange
        _buffer.SetLines(["ab", "c"]);

        // Act
        _buffer.MoveTo(new DiffPosition(9, 9), extend: false);

        // Assert
        Assert.Equal(new DiffPosition(1, 1), _buffer.Caret);
    }

    [Theory]
    [InlineData("a\r\nb\r\n", "\r\n", true)]
    [InlineData("a\nb", "\n", false)]
    [InlineData("a\rb\r", "\r", true)]
    [InlineData("a", "\r\n", false)]
    public void Format_TellsTheLineBreak_AndJoinWritesItBack(string text, string lineBreak, bool endsWithIt)
    {
        // Act
        DiffTextFormatModel format = DiffTextFormatModel.Of(text, DiffTextFormatModel.Default.Encoding);

        // Assert
        Assert.Equal((lineBreak, endsWithIt), (format.LineBreak, format.EndsWithLineBreak));
        Assert.Equal(text, format.Join(TextDiffer.SplitLines(text, preserveFinalLine: true)));
    }
}
