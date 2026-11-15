using System.Drawing;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Services;
using CincoVertice.MyTools.Ui.Win.Diff;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Diff;

/// <summary>The control with a stand-in for the presenter: each edit compares the sides again at once.</summary>
public sealed class DiffTextControlTests : IDisposable
{
    private readonly Form _form = new() { ClientSize = new Size(800, 300) };
    private readonly DiffTextControl _control = new() { Dock = DockStyle.Fill };
    private readonly List<DiffSide> _edits = [];
    private IReadOnlyList<DiffHunkModel> _hunks = [];

    public DiffTextControlTests()
    {
        _form.Controls.Add(_control);
        _control.TextEdited += (_, side) =>
        {
            _edits.Add(side);
            Compare(keepPlace: true);
        };
    }

    public void Dispose()
    {
        _form.Dispose();
    }

    [Fact]
    public void ColumnsOf_TakesTabsToTheirStops()
    {
        // Act
        int[] columns = DiffTextControl.ColumnsOf("a\tbc\td");

        // Assert: a at 0, the tab to 4, b at 4, c at 5, the tab to 8, d at 8, then the end
        Assert.Equal([0, 1, 4, 5, 6, 8, 9], columns);
        Assert.Equal(9, DiffTextControl.WidthOf("a\tbc\td"));
        Assert.Equal("a   bc  d", DiffTextControl.Expanded("a\tbc\td"));
    }

    [Fact]
    public void ShowHunk_ScrollsAChangeOutOfViewIntoIt_AndPutsTheCaretThere()
    {
        // Arrange
        Show(Numbered(200), Numbered(200, changedLine: 150));

        // Act
        _control.ShowHunk(_hunks[0]);

        // Assert
        Assert.InRange(149, _control.TopRow, _control.TopRow + 10);
        Assert.Equal(149, _control.CurrentRow);
        Assert.Equal(new DiffPosition(149, 0), _control.BufferOf(DiffSide.Left).Caret);
    }

    [Fact]
    public void SetRows_KeepingPlace_StaysAtTheRowsShowing()
    {
        // Arrange
        Show(Numbered(200), Numbered(200, changedLine: 150));
        _control.TopRow = 80;

        // Act
        Compare(keepPlace: true);
        int kept = _control.TopRow;
        Compare(keepPlace: false);

        // Assert
        Assert.Equal(80, kept);
        Assert.Equal(0, _control.TopRow);
    }

    [Fact]
    public void Typing_OnASide_ChangesItsText_AndTheRowsFollow()
    {
        // Arrange
        Show(["one", "two"], ["one", "two"]);
        _control.Press(EndOfRow(1, DiffSide.Right), extend: false);

        // Act
        _control.Type('s');

        // Assert
        Assert.Equal(DiffSide.Right, _control.EditedSide);
        Assert.Equal(["one", "twos"], _control.LinesOf(DiffSide.Right));
        Assert.Equal([DiffSide.Right], _edits);
        Assert.Equal(DiffRowKind.Changed, _control.Rows[1].Kind);
    }

    [Fact]
    public void Enter_StartsALine_WithTheSameIndent_AndUndoTakesItBack()
    {
        // Arrange
        Show(["    if (x)"], ["    if (x)"]);
        _control.Press(EndOfRow(0, DiffSide.Left), extend: false);

        // Act
        _control.HandleKey(Keys.Enter);
        IReadOnlyList<string> entered = [.. _control.LinesOf(DiffSide.Left)];
        _control.HandleKey(Keys.Control | Keys.Z);

        // Assert
        Assert.Equal(["    if (x)", "    "], entered);
        Assert.Equal(["    if (x)"], _control.LinesOf(DiffSide.Left));
        Assert.Equal(DiffRowKind.Same, _control.Rows[0].Kind);
    }

    [Fact]
    public void ADrag_SelectsText_OnTheSideItStarted()
    {
        // Arrange
        Show(["abc", "def"], ["x"]);

        // Act: from the start of the first line to the end of the second
        _control.Press(new Point(1, Middle(0)), extend: false);
        _control.DragTo(EndOfRow(1, DiffSide.Left));
        _control.Release();

        // Assert
        Assert.Equal("abc" + Environment.NewLine + "def", _control.SelectedText());
    }

    [Fact]
    public void AClickOnABlankRow_PutsTheCaretOnTheNextLineOfThatSide()
    {
        // Arrange: the right side has a line the left hasn't, at row 1
        Show(["a", "c"], ["a", "b", "c"]);

        // Act
        _control.Press(EndOfRow(1, DiffSide.Left), extend: false);

        // Assert
        Assert.Equal(DiffRowKind.Added, _control.Rows[1].Kind);
        Assert.Equal(new DiffPosition(1, 0), _control.BufferOf(DiffSide.Left).Caret);
    }

    [Fact]
    public void Backspace_AtALinesStart_JoinsTheLines_AndTheRowsFollow()
    {
        // Arrange
        Show(["ab", "cd"], ["abcd"]);
        _control.Press(new Point(1, Middle(1)), extend: false);

        // Act
        _control.HandleKey(Keys.Back);

        // Assert
        Assert.Equal(["abcd"], _control.LinesOf(DiffSide.Left));
        Assert.Single(_control.Rows);
        Assert.Equal(DiffRowKind.Same, _control.Rows[0].Kind);
    }

    [Fact]
    public void F6_GoesToTheOtherSide_AtTheSameRow()
    {
        // Arrange
        Show(["a", "b", "c"], ["a", "b", "c"]);
        _control.Press(EndOfRow(2, DiffSide.Left), extend: false);

        // Act
        _control.HandleKey(Keys.F6);

        // Assert
        Assert.Equal(DiffSide.Right, _control.EditedSide);
        Assert.Equal(new DiffPosition(2, 0), _control.BufferOf(DiffSide.Right).Caret);
    }

    private static string[] Numbered(int lines, int changedLine = -1) =>
        [.. Enumerable.Range(1, lines).Select(i => i == changedLine ? "changed" : $"line {i}")];

    private void Show(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        _control.SetText(DiffSide.Left, left);
        _control.SetText(DiffSide.Right, right);
        Compare(keepPlace: false);
    }

    private void Compare(bool keepPlace)
    {
        DiffResultModel result = TextDiffer.Compare(
            _control.LinesOf(DiffSide.Left),
            _control.LinesOf(DiffSide.Right),
            new DiffOptionsModel());
        _control.SetRows(result.Rows, result.Hunks, keepPlace);
        _hunks = result.Hunks;
    }

    private int Middle(int row) => row * _control.RowHeight + _control.RowHeight / 2;

    // Far right in a side: past the end of its short lines
    private Point EndOfRow(int row, DiffSide side) =>
        new(side == DiffSide.Left ? _control.Width / 2 - 40 : _control.Width - 60, Middle(row));
}
