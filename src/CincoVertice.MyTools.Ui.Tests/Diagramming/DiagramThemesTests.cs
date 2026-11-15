using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diagramming;

public class DiagramThemesTests
{
    private static readonly DiagramThemeModel _office = DiagramThemes.Of("Office");
    private static readonly DiagramThemeModel _ocean = DiagramThemes.Of("Ocean");

    private static void AssertNear(int rgb, Color actual)
    {
        Color expected = Color.FromArgb(rgb);
        Assert.InRange(actual.R, expected.R - 3, expected.R + 3);
        Assert.InRange(actual.G, expected.G - 3, expected.G + 3);
        Assert.InRange(actual.B, expected.B - 3, expected.B + 3);
    }

    [Fact]
    public void All_HaveTenColors_AndNamesOfTheirOwn()
    {
        // Assert
        Assert.Equal(7, DiagramThemes.All.Count);
        Assert.All(DiagramThemes.All, theme => Assert.Equal(DiagramThemes.Columns, theme.Colors.Count));
        Assert.Equal(DiagramThemes.All.Count, DiagramThemes.All.Select(theme => theme.Name).Distinct().Count());
        Assert.Equal(10, DiagramThemes.StandardColors.Count);
    }

    [Theory]
    [InlineData("ocean", "Ocean")]
    [InlineData("Nothing", "Office")]
    [InlineData(null, "Office")]
    public void Of_FindsTheThemeInAnyCase_OrElseTheDefault(string? name, string expected)
    {
        // Act / Assert
        Assert.Equal(expected, DiagramThemes.Of(name).Name);
    }

    [Fact]
    public void Grid_IsTheThemesColors_ThenTheirVersions_AsOfficeMakesThem()
    {
        // Act
        IReadOnlyList<DiagramSwatchModel> grid = DiagramThemes.Grid(_office);

        // Assert: accent 1 is 4472C4, lighter 80% DAE3F3 and darker 50% 203864, as in Word
        Assert.Equal(60, grid.Count);
        Assert.Equal(Color.FromArgb(255, 0x44, 0x72, 0xC4), grid[4].Color);
        Assert.Equal("Accent 1", grid[4].Name);
        AssertNear(0xDAE3F3, grid[14].Color);
        Assert.Equal("Accent 1, lighter 80%", grid[14].Name);
        AssertNear(0x203864, grid[54].Color);

        // A white column gets darker versions, a black one lighter
        AssertNear(0xF2F2F2, grid[10].Color);
        Assert.Equal("Background 1, darker 5%", grid[10].Name);
        AssertNear(0x7F7F7F, grid[11].Color);
        Assert.Equal("Text 1, lighter 50%", grid[11].Name);
    }

    [Fact]
    public void Recolored_GivesEachThemeColorTheOneAtTheSamePlace_AndLeavesTheOthers()
    {
        // Arrange: accent 1 lighter 40% filled, a standard red line, a color of its own as text, no title fill
        IReadOnlyList<DiagramSwatchModel> office = DiagramThemes.Grid(_office);
        IReadOnlyList<DiagramSwatchModel> ocean = DiagramThemes.Grid(_ocean);
        Color own = Color.FromArgb(255, 1, 2, 3);
        var style = new DiagramStyleModel(office[34].Color, DiagramThemes.StandardColors[1].Color, 2, false, own, 14)
        {
            TitleFill = DiagramStyleModel.NoTitleFill,
        };
        var diagram = new DiagramModel(
            [new DiagramShapeModel(1, DiagramShapeKind.Container, new Rectangle(0, 0, 9, 9), "", style)],
            [
                new DiagramConnectorModel(
                    2,
                    1,
                    1,
                    DiagramRouting.Straight,
                    DiagramArrowHeads.End,
                    string.Empty,
                    style with { Line = office[1].Color }),
            ]);

        // Act
        DiagramModel recolored = DiagramThemes.Recolored(diagram, _office, _ocean);

        // Assert
        DiagramStyleModel shape = recolored.Shapes[0].Style;
        Assert.Equal(ocean[34].Color, shape.Fill);
        Assert.Equal(DiagramThemes.StandardColors[1].Color, shape.Line);
        Assert.Equal(own, shape.TextColor);
        Assert.Equal(DiagramStyleModel.NoTitleFill, shape.TitleFill);
        Assert.Equal(ocean[1].Color, recolored.Connectors[0].Style.Line);
    }

    [Fact]
    public void DefaultsOf_AreAccent1LightInside_AndDarkerForTheLine_WithText1_KeepingTheRest()
    {
        // Arrange
        var style = new DiagramStyleModel(Color.White, Color.Gray, 3, true, Color.Red, 20);
        IReadOnlyList<DiagramSwatchModel> grid = DiagramThemes.Grid(_ocean);

        // Act
        DiagramStyleModel themed = DiagramThemes.DefaultsOf(_ocean, style);

        // Assert
        Assert.Equal(style with { Fill = grid[14].Color, Line = grid[44].Color, TextColor = grid[1].Color }, themed);
    }

    [Fact]
    public void Offers_TheGridAndTheStandardColors_NotOthers()
    {
        // Act / Assert
        Assert.True(DiagramThemes.Offers(_office, DiagramThemes.Grid(_office)[27].Color));
        Assert.True(DiagramThemes.Offers(_office, Color.FromArgb(255, 0xC0, 0, 0)));
        Assert.False(DiagramThemes.Offers(_office, Color.FromArgb(255, 1, 2, 3)));
    }
}
