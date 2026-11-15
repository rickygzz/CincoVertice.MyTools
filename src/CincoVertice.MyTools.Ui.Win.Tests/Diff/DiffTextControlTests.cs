using System.Drawing;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Services;
using CincoVertice.MyTools.Ui.Win.Diff;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Diff;

public class DiffTextControlTests
{
    [Fact]
    public void ColumnsOf_TakesTabsToTheirStops()
    {
        // Act
        int[] columns = DiffTextControl.ColumnsOf("a\tbc\td");

        // Assert: a at 0, the tab to 4, b at 4, c at 5, the tab to 8, d at 8, then the end
        Assert.Equal([0, 1, 4, 5, 6, 8, 9], columns);
        Assert.Equal("a   bc  d", DiffTextControl.Expanded("a\tbc\td"));
    }

    [Fact]
    public void ShowHunk_ScrollsAChangeOutOfViewIntoIt()
    {
        // Arrange
        using var control = new DiffTextControl { Size = new Size(600, 200) };
        DiffResultModel result = Compared(200, changedLine: 150);
        control.SetRows(result.Rows, result.Hunks, keepPlace: false);

        // Act
        control.ShowHunk(result.Hunks[0]);

        // Assert
        Assert.InRange(149, control.TopRow, control.TopRow + 10);
        Assert.Equal(149, control.CurrentRow);
    }

    [Fact]
    public void SetRows_KeepingPlace_StaysAtTheRowsShowing()
    {
        // Arrange
        using var control = new DiffTextControl { Size = new Size(600, 200) };
        DiffResultModel result = Compared(200, changedLine: 150);
        control.SetRows(result.Rows, result.Hunks, keepPlace: false);
        control.TopRow = 80;

        // Act
        control.SetRows(result.Rows, result.Hunks, keepPlace: true);
        int kept = control.TopRow;
        control.SetRows(result.Rows, result.Hunks, keepPlace: false);

        // Assert
        Assert.Equal(80, kept);
        Assert.Equal(0, control.TopRow);
    }

    [Fact]
    public void ADragOverRows_SelectsThem_AndTheirTextIsTheirSides()
    {
        // Arrange
        using var form = new Form { ClientSize = new Size(600, 300) };
        var control = new DiffTextControl { Dock = DockStyle.Fill };
        form.Controls.Add(control);
        DiffResultModel result = TextDiffer.Compare("a\nb\nc", "a\nB\nc\nd", new DiffOptionsModel());
        control.SetRows(result.Rows, result.Hunks, keepPlace: false);
        int rowHeight = control.RowHeight;

        // Act: on the right side, from the first row to the last
        Point start = new(control.Width * 3 / 4 - 40, 2);
        Point end = new(control.Width * 3 / 4 - 40, rowHeight * 3 + rowHeight / 2);
        control.Press(start, extend: false);
        control.DragTo(end);
        control.Release();

        // Assert
        Assert.Equal(DiffSide.Right, control.SideAt(start));
        Assert.Equal(DiffSide.Left, control.SideAt(new Point(10, 2)));
        Assert.Equal(string.Join(Environment.NewLine, "a", "B", "c", "d") + Environment.NewLine, control.SelectedText());
    }

    [Fact]
    public void NothingSelected_HasNoText()
    {
        // Arrange
        using var control = new DiffTextControl { Size = new Size(600, 200) };
        DiffResultModel result = Compared(10, changedLine: 5);
        control.SetRows(result.Rows, result.Hunks, keepPlace: false);

        // Act
        control.ShowHunk(result.Hunks[0]);

        // Assert: going to a change marks it, but selects nothing
        Assert.Equal(string.Empty, control.SelectedText());
    }

    private static DiffResultModel Compared(int lines, int changedLine)
    {
        string left = string.Join("\n", Enumerable.Range(1, lines).Select(i => $"line {i}"));
        string right = string.Join(
            "\n",
            Enumerable.Range(1, lines).Select(i => i == changedLine ? "changed" : $"line {i}"));

        return TextDiffer.Compare(left, right, new DiffOptionsModel());
    }
}
