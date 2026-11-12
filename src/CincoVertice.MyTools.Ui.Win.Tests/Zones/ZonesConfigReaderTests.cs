using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Zones.Models;
using CincoVertice.MyTools.Ui.Win.Zones.Services;
using CincoVertice.MyTools.Ui.Zones.Models;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Zones;

public class ZonesConfigReaderTests
{
    [Fact]
    public void Read_TheNewFile_IsThreeColumns_WithTheDefaultGap_AndNoProblems()
    {
        // Act
        ZonesConfigResultModel result = ZonesConfigReader.Read(ZonesConfigService.CreateDefaultText());

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(ZoneLayoutModel.Default.Zones, result.Layout.Zones);
        Assert.Equal(ZoneLayoutModel.DefaultGap, result.Layout.Gap);
    }

    [Fact]
    public void Read_NothingButComments_IsTheDefault()
    {
        // Act
        ZonesConfigResultModel result = ZonesConfigReader.Read("# nothing\n");

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(ZoneLayoutModel.Default.Zones, result.Layout.Zones);
    }

    [Theory]
    [InlineData("Layout = Columns 2", "0 0 50 100|50 0 50 100")]
    [InlineData("layout = columns 25 50 25 # wide center", "0 0 25 100|25 0 50 100|75 0 25 100")]
    [InlineData("Layout = Rows 30 70", "0 0 100 30|0 30 100 70")]
    [InlineData("Layout = Grid 2 x 2", "0 0 50 50|50 0 50 50|0 50 50 50|50 50 50 50")]
    [InlineData("Layout = Grid 2x1", "0 0 50 100|50 0 50 100")]
    [InlineData("Zone = 0, 0, 60, 100\nZone = 60, 0, 40, 50\nZone = 60 50 40 50", "0 0 60 100|60 0 40 50|60 50 40 50")]
    public void Read_GivesTheZones(string text, string expected)
    {
        // Act
        ZonesConfigResultModel result = ZonesConfigReader.Read(text);

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(expected, Describe(result.Layout.Zones));
    }

    [Fact]
    public void Read_ThirdsInPercent_AddUpToCloseEnoughTo100()
    {
        // Act
        ZonesConfigResultModel result = ZonesConfigReader.Read("Layout = Columns 33.33 33.33 33.33");

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(3, result.Layout.Zones.Count);
    }

    [Fact]
    public void Read_AGap_IsUsed()
    {
        // Act
        ZonesConfigResultModel result = ZonesConfigReader.Read("Layout = Columns 2\nGap = 0");

        // Assert
        Assert.Equal(0, result.Layout.Gap);
    }

    private const string LayoutExample = "e.g. Layout = Columns 3, Columns 25 50 25, Rows 2 or Grid 3 x 2.";
    private const string GridHelp = "Layout: Grid takes columns x rows, e.g. Grid 3 x 2, with 99 zones at most.";

    [Theory]
    [InlineData("Layout = Diagonal 3", "Layout: 'Diagonal 3' is not Columns, Rows or Grid; " + LayoutExample)]
    [InlineData("Layout =", "Layout: which layout? " + LayoutExample)]
    [InlineData("Layout = Columns 30 60", "Layout: the columns add up to 90 percent, not 100.")]
    [InlineData(
        "Layout = Rows 0",
        "Layout: how many rows from 1 to 99, or the size of each in percent, e.g. Rows 3 or Rows 25 50 25.")]
    [InlineData("Layout = Grid 3", GridHelp)]
    [InlineData("Layout = Grid 10 x 10", GridHelp)]
    [InlineData(
        "Zone = 0, 0, 50",
        "Zone: four numbers in percent of the screen: left, top, width, height; e.g. Zone = 0, 0, 50, 100.")]
    [InlineData("Zone = 60, 0, 50, 100", "Zone: it goes past the right edge: left + width is 110, more than 100.")]
    [InlineData("Zone = 0, 60, 50, 50", "Zone: it goes past the bottom: top + height is 110, more than 100.")]
    [InlineData("Zone = 0, 0, 0, 100", "Zone: its width and height must be more than 0.")]
    [InlineData("Gap = -1", "Gap: a whole number of pixels from 0 to 100, not '-1'.")]
    [InlineData("Gap = 101", "Gap: a whole number of pixels from 0 to 100, not '101'.")]
    [InlineData("Columns = 3", "Columns: each line is Layout = ..., Zone = ... or Gap = ....")]
    public void Read_ALineThatCantBeUsed_IsAProblemWithItsLine_AndTheDefaultIsUsed(string line, string message)
    {
        // Act
        ZonesConfigResultModel result = ZonesConfigReader.Read("# zones\n" + line);

        // Assert
        Assert.Equal(new ConfigLineProblemModel(2, message), Assert.Single(result.LineProblems));
        Assert.Equal($"Line 2: {message}", Assert.Single(result.Problems));
        Assert.Equal(ZoneLayoutModel.Default.Zones, result.Layout.Zones);
    }

    [Fact]
    public void Read_ASecondLayout_IsAProblem_AndTheFirstIsUsed()
    {
        // Act
        ZonesConfigResultModel result = ZonesConfigReader.Read("Layout = Columns 2\nLayout = Rows 2");

        // Assert
        Assert.Equal("Line 2: Layout: there's one already, line 1, which is used.", Assert.Single(result.Problems));
        Assert.Equal("0 0 50 100|50 0 50 100", Describe(result.Layout.Zones));
    }

    [Fact]
    public void Read_ALayoutAndZones_IsAProblem_AndTheZonesAreUsed()
    {
        // Act
        ZonesConfigResultModel result = ZonesConfigReader.Read("Layout = Columns 2\nZone = 0, 0, 100, 100");

        // Assert
        Assert.Equal(
            "Line 1: Layout: there are Zone lines too, and they're used instead.",
            Assert.Single(result.Problems));
        Assert.Equal("0 0 100 100", Describe(result.Layout.Zones));
    }

    private static string Describe(IEnumerable<ZoneModel> zones) =>
        string.Join(
            "|",
            zones.Select(zone => $"{zone.Left:0.##} {zone.Top:0.##} {zone.Width:0.##} {zone.Height:0.##}"));
}
