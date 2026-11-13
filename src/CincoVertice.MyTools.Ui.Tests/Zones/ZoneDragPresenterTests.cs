using System.Drawing;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Models;
using CincoVertice.MyTools.Ui.Zones.Presenters;
using CincoVertice.MyTools.Ui.Zones.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Zones;

public class ZoneDragPresenterTests
{
    private const nint Window = FakeZoneDesktop.Window;

    // The zones of both monitors, left to right
    private static readonly Rectangle[] _zones =
    [
        new(0, 0, 500, 600), new(500, 0, 500, 600), new(1000, 0, 500, 600), new(1500, 0, 500, 600),
    ];

    private readonly FakeDrags _drags = new();
    private readonly FakeOverlay _overlay = new();
    private readonly FakeZoneDesktop _desktop = new();

    public ZoneDragPresenterTests()
    {
        new ZoneDragPresenter(_drags, _overlay, _desktop, _desktop).Start();
    }

    [Fact]
    public void ADragWithoutShift_ShowsNothing_AndMovesNothing()
    {
        // Act
        _drags.Start(Window);
        _drags.Move(700, 300, shift: false);
        _drags.End();

        // Assert
        Assert.Empty(_overlay.Shown);
        Assert.Empty(_desktop.Moves);
        Assert.Equal(0, _desktop.Loads);
    }

    [Fact]
    public void WithShift_ShowsTheZonesOfEveryMonitor_WithTheOneUnderTheMouse()
    {
        // Act
        _drags.Start(Window);
        _drags.Move(1700, 300, shift: true);

        // Assert
        (IReadOnlyList<Rectangle> zones, int? highlighted) = Assert.Single(_overlay.Shown);
        Assert.Equal(_zones, zones);
        Assert.Equal(3, highlighted);
    }

    [Fact]
    public void MovingInTheSameZone_DoesntShowItAgain_AnotherZoneDoes()
    {
        // Act
        _drags.Start(Window);
        _drags.Move(100, 100, shift: true);
        _drags.Move(200, 150, shift: true);
        _drags.Move(600, 150, shift: true);

        // Assert
        Assert.Equal([0, 1], _overlay.Shown.Select(shown => shown.Highlighted));
    }

    [Fact]
    public void LettingGoOverAZone_WithShift_MovesTheWindowThere_AndHidesTheZones()
    {
        // Act
        _drags.Start(Window);
        _drags.Move(600, 300, shift: true);
        _drags.End();

        // Assert
        Assert.Equal((Window, _zones[1]), Assert.Single(_desktop.Moves));
        Assert.Equal(1, _overlay.Hides);
    }

    [Fact]
    public void LettingGoShift_BeforeTheEnd_HidesTheZones_AndMovesNothing()
    {
        // Act
        _drags.Start(Window);
        _drags.Move(600, 300, shift: true);
        _drags.Move(620, 300, shift: false);
        _drags.End();

        // Assert
        Assert.Equal(1, _overlay.Hides);
        Assert.Empty(_desktop.Moves);
    }

    [Fact]
    public void ShiftAgain_ShowsTheZonesAgain_WithoutReadingThemAgain()
    {
        // Act
        _drags.Start(Window);
        _drags.Move(600, 300, shift: true);
        _drags.Move(620, 300, shift: false);
        _drags.Move(640, 300, shift: true);

        // Assert
        Assert.Equal(2, _overlay.Shown.Count);
        Assert.Equal(1, _desktop.Loads);
    }

    [Fact]
    public void EachDrag_ReadsTheLayoutAgain()
    {
        // Arrange
        _drags.Start(Window);
        _drags.Move(600, 300, shift: true);
        _drags.End();
        _desktop.Layout = new ZoneLayoutModel(ZoneLayoutModel.Columns([100]), 0);

        // Act
        _drags.Start(Window);
        _drags.Move(600, 300, shift: true);

        // Assert
        Assert.Equal([FakeZoneDesktop.Left, FakeZoneDesktop.Right], _overlay.Shown[^1].Zones);
    }

    [Fact]
    public void AMonitorWithItsOwnLayout_ShowsIts_TheOthersTheDefault()
    {
        // Arrange: the right monitor in two rows
        _desktop.Layouts = _desktop.Layouts with
        {
            Monitors =
            [
                new(FakeZoneDesktop.Right, new(ZoneLayoutModel.Rows(ZoneLayoutModel.Equal(2)), 0)),
            ],
        };

        // Act
        _drags.Start(Window);
        _drags.Move(1500, 500, shift: true);
        _drags.End();

        // Assert
        Assert.Equal(
            [_zones[0], _zones[1], new Rectangle(1000, 0, 1000, 300), new Rectangle(1000, 300, 1000, 300)],
            _overlay.Shown[0].Zones);
        Assert.Equal((Window, new Rectangle(1000, 300, 1000, 300)), Assert.Single(_desktop.Moves));
    }

    [Fact]
    public void AWindowThatCantBeMoved_IsAProblemShown()
    {
        // Arrange
        _desktop.MoveProblem = "It runs as administrator.";

        // Act
        _drags.Start(Window);
        _drags.Move(600, 300, shift: true);
        _drags.End();

        // Assert
        Assert.Equal(["It runs as administrator."], _overlay.Problems);
    }

    [Fact]
    public void MovesWithoutADrag_AreIgnored()
    {
        // Act
        _drags.Move(600, 300, shift: true);
        _drags.End();

        // Assert
        Assert.Empty(_overlay.Shown);
        Assert.Empty(_desktop.Moves);
    }

    private sealed class FakeDrags : IWindowDragSource
    {
        public event EventHandler<nint>? DragStarted;

        public event EventHandler<ZoneDragPointerModel>? DragMoved;

        public event EventHandler? DragEnded;

        public void Start(nint window) => DragStarted?.Invoke(this, window);

        public void Move(int x, int y, bool shift) =>
            DragMoved?.Invoke(this, new ZoneDragPointerModel(new Point(x, y), shift));

        public void End() => DragEnded?.Invoke(this, EventArgs.Empty);
    }

    private sealed class FakeOverlay : IZoneOverlayView
    {
        public List<(IReadOnlyList<Rectangle> Zones, int? Highlighted)> Shown { get; } = [];

        public int Hides { get; private set; }

        public List<string> Problems { get; } = [];

        public void ShowZones(IReadOnlyList<Rectangle> zones, int? highlighted) => Shown.Add((zones, highlighted));

        public void HideZones() => Hides++;

        public void ShowProblem(string message) => Problems.Add(message);
    }
}
