using System.Drawing;
using CincoVertice.MyTools.Ui.Zones.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Zones;

public class ZoneMoverTests
{
    private readonly FakeZoneDesktop _desktop = new();
    private readonly ZoneMover _mover;

    public ZoneMoverTests()
    {
        _mover = new ZoneMover(_desktop, _desktop);
    }

    [Fact]
    public void MoveActiveWindowTo_MovesItToThatZoneOfItsMonitor()
    {
        // Arrange: on the right monitor
        _desktop.WindowBounds = new Rectangle(1200, 100, 300, 200);

        // Act
        string? problem = _mover.MoveActiveWindowTo(2);

        // Assert
        Assert.Null(problem);
        Assert.Equal((FakeZoneDesktop.Window, new Rectangle(1500, 0, 500, 600)), Assert.Single(_desktop.Moves));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void MoveActiveWindowTo_AZoneThatIsntThere_SaysWhich(int number)
    {
        // Act
        string? problem = _mover.MoveActiveWindowTo(number);

        // Assert
        Assert.Equal($"There's no zone {number}: there are 2, from 1 to 2.", problem);
        Assert.Empty(_desktop.Moves);
    }

    [Fact]
    public void MoveActiveWindowTo_WithoutAWindow_SaysSo()
    {
        // Arrange
        _desktop.ActiveWindow = 0;

        // Act
        string? problem = _mover.MoveActiveWindowTo(1);

        // Assert
        Assert.Equal("There's no window to move.", problem);
    }

    [Fact]
    public void MoveActiveWindowTo_WhenTheWindowCantBeMoved_SaysWhy()
    {
        // Arrange
        _desktop.MoveProblem = "It runs as administrator.";

        // Act
        string? problem = _mover.MoveActiveWindowTo(1);

        // Assert
        Assert.Equal("It runs as administrator.", problem);
    }

    [Fact]
    public void MoveActiveWindowBy_GoesFromZoneToZone_AroundTheEnd()
    {
        // Act and assert: from no zone to the first, then the second, then the first again
        _mover.MoveActiveWindowBy(1);
        Assert.Equal(new Rectangle(0, 0, 500, 600), _desktop.WindowBounds);

        _mover.MoveActiveWindowBy(1);
        Assert.Equal(new Rectangle(500, 0, 500, 600), _desktop.WindowBounds);

        _mover.MoveActiveWindowBy(1);
        Assert.Equal(new Rectangle(0, 0, 500, 600), _desktop.WindowBounds);
    }

    [Fact]
    public void MoveActiveWindowBy_Back_FromNoZone_GoesToTheLast()
    {
        // Act
        _mover.MoveActiveWindowBy(-1);

        // Assert
        Assert.Equal(new Rectangle(500, 0, 500, 600), _desktop.WindowBounds);

        // And back from the first to the last
        _mover.MoveActiveWindowBy(-1);
        _mover.MoveActiveWindowBy(-1);
        Assert.Equal(new Rectangle(500, 0, 500, 600), _desktop.WindowBounds);
    }
}
