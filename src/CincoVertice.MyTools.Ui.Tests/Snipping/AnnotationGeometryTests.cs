using System.Drawing;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Snipping;

public class AnnotationGeometryTests
{
    private static readonly Func<string, int, Size> _noText = (_, _) => Size.Empty;

    [Fact]
    public void HandleAt_WhereHandlesOverlap_TakesTheNearest()
    {
        // Arrange: a level line 4 wide has its top and bottom handles 4 pixels apart
        var line = new StrokeAnnotationModel([new Point(100, 100), new Point(300, 100)], Color.Red, 4);
        SnipSelectionModel selection = AnnotationGeometry.SelectionOf(line, _noText);
        Point top = selection.Handles[1];
        Point bottom = selection.Handles[5];

        // Act & Assert
        Assert.Equal(1, AnnotationGeometry.HandleAt(selection, top));
        Assert.Equal(5, AnnotationGeometry.HandleAt(selection, bottom));
    }

    [Fact]
    public void HandleAt_OfADot_TakesTheCornerNearest()
    {
        // Arrange: every handle of a small frame overlaps the others
        var dot = new StrokeAnnotationModel([new Point(100, 100)], Color.Red, 4);
        SnipSelectionModel selection = AnnotationGeometry.SelectionOf(dot, _noText);

        // Act & Assert
        for (int i = 0; i < selection.Handles.Count; i++)
        {
            Assert.Equal(i, AnnotationGeometry.HandleAt(selection, selection.Handles[i]));
        }
    }

    [Fact]
    public void HandleAt_FarFromThemAll_IsNone()
    {
        // Arrange
        var box = new RectangleAnnotationModel(new Rectangle(100, 100, 100, 100), Color.Red, 4);
        SnipSelectionModel selection = AnnotationGeometry.SelectionOf(box, _noText);

        // Act & Assert
        Assert.Null(AnnotationGeometry.HandleAt(selection, new Point(150, 150)));
    }

    private static ArrowAnnotationModel LevelArrow(SnipArrowHeads heads) =>
        new(new Point(100, 100), new Point(400, 100), Color.Red, 16, heads);

    [Theory]
    [InlineData(SnipArrowHeads.End, 350, 120, true)]
    [InlineData(SnipArrowHeads.End, 350, 80, true)]
    [InlineData(SnipArrowHeads.End, 150, 120, false)]
    [InlineData(SnipArrowHeads.Start, 150, 120, true)]
    [InlineData(SnipArrowHeads.Start, 350, 120, false)]
    [InlineData(SnipArrowHeads.Both, 150, 80, true)]
    [InlineData(SnipArrowHeads.Both, 350, 120, true)]
    public void Hits_AnArrowsHead_OnlyWhereItHasOne(SnipArrowHeads heads, int x, int y, bool expected)
    {
        Assert.Equal(expected, AnnotationGeometry.Hits(LevelArrow(heads), new Point(x, y), _noText));
    }

    [Theory]
    [InlineData(250, 100, true)]
    [InlineData(250, 111, true)]
    [InlineData(250, 125, false)]
    [InlineData(385, 160, false)]
    [InlineData(440, 100, false)]
    public void Hits_AnArrow_NotBesideIt(int x, int y, bool expected)
    {
        Assert.Equal(expected, AnnotationGeometry.Hits(LevelArrow(SnipArrowHeads.End), new Point(x, y), _noText));
    }

    [Fact]
    public void Hits_AnArrowOfNoLength_DoesntFail()
    {
        // Arrange
        var arrow = new ArrowAnnotationModel(new Point(100, 100), new Point(100, 100), Color.Red, 4, SnipArrowHeads.Both);

        // Act & Assert
        Assert.True(AnnotationGeometry.Hits(arrow, new Point(100, 100), _noText));
        Assert.False(AnnotationGeometry.Hits(arrow, new Point(200, 200), _noText));
    }
}
