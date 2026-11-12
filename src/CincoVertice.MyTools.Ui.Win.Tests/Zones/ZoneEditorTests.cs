using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Zones;
using CincoVertice.MyTools.Ui.Win.Zones.Models;
using CincoVertice.MyTools.Ui.Win.Zones.Services;
using CincoVertice.MyTools.Ui.Zones.Models;
using CincoVertice.MyTools.Ui.Zones.Presenters;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Zones;

/// <summary>Saving zones.cfg, and the editor window with the real form, shown for a moment.</summary>
public sealed class ZoneEditorTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ZoneEditorTests_" + Guid.NewGuid());
    private readonly ZonesConfigService _zones;

    public ZoneEditorTests()
    {
        _zones = new ZonesConfigService(Path.Combine(_folder, "zones.cfg"), new TrayNotificationService());
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Save_WritesZonesTheReaderReadsBack_WithThirdsThatStillMeet()
    {
        // Arrange
        var layout = new ZoneLayoutModel(ZoneLayoutModel.Columns(ZoneLayoutModel.Equal(3)), 12);

        // Act
        string? problem = _zones.Save(new ZoneLayoutsModel(layout, []));
        ZonesConfigResultModel read = ZonesConfigReader.Read(File.ReadAllText(_zones.FilePath));

        // Assert
        Assert.Null(problem);
        Assert.Empty(read.Problems);
        Assert.Equal(12, read.Layouts.DefaultLayout.Gap);
        Assert.Equal(3, read.Layouts.DefaultLayout.Zones.Count);
        Assert.Equal(read.Layouts.DefaultLayout.Zones[0].Right, read.Layouts.DefaultLayout.Zones[1].Left, 6);
        Assert.Equal(read.Layouts.DefaultLayout.Zones[1].Right, read.Layouts.DefaultLayout.Zones[2].Left, 6);
        Assert.Equal(100, read.Layouts.DefaultLayout.Zones[2].Right, 6);
    }

    [Fact]
    public void Save_WhereItCantWrite_SaysWhy()
    {
        // Arrange: a folder where the file should be
        Directory.CreateDirectory(_zones.FilePath);

        // Act
        string? problem = _zones.Save(ZoneLayoutsModel.Default);

        // Assert
        Assert.StartsWith($"Could not save {_zones.FilePath}:", problem);
    }

    [Fact]
    public void Save_WritesEachMonitorsOwnLayout_ThatTheReaderReadsBack()
    {
        // Arrange: a monitor left of the main one, which has a negative left
        var layouts = new ZoneLayoutsModel(
            new ZoneLayoutModel(ZoneLayoutModel.Columns(ZoneLayoutModel.Equal(2)), 8),
            [
                new(new Rectangle(-1920, 0, 1920, 1080), new(ZoneLayoutModel.Rows(ZoneLayoutModel.Equal(2)), 0)),
                new(new Rectangle(1920, 0, 2560, 1440), new(ZoneLayoutModel.Columns([25, 50, 25]), 16)),
            ]);

        // Act
        _zones.Save(layouts);
        ZonesConfigResultModel read = ZonesConfigReader.Read(File.ReadAllText(_zones.FilePath));

        // Assert
        Assert.Empty(read.Problems);
        Assert.True(read.Layouts.SameAs(layouts));
    }

    [Fact]
    public void Editor_DraggingABorderWithTheMouse_ChangesTheZones_AndShowsThemUnsaved()
    {
        // Arrange: two columns on the one monitor of the size of the canvas
        var twoColumns = new ZoneLayoutModel(ZoneLayoutModel.Columns(ZoneLayoutModel.Equal(2)), 0);
        _zones.Save(new ZoneLayoutsModel(twoColumns, []));
        using var form = new ZoneEditorForm();
        new ZoneEditorPresenter(form, _zones, new FakeDesktop(), () => { }).Start();
        form.Show();
        Application.DoEvents();

        Control canvas = form.Controls["Canvas"]!;
        Rectangle monitor = Monitor(form);
        var border = new Point(monitor.Left + (monitor.Width / 2), monitor.Top + (monitor.Height / 2));
        var to = new Point(monitor.Left + (monitor.Width * 3 / 4), border.Y);

        // Act
        Raise(form, "Canvas_MouseDown", new MouseEventArgs(MouseButtons.Left, 1, border.X, border.Y, 0));
        Raise(form, "Canvas_MouseMove", new MouseEventArgs(MouseButtons.Left, 0, to.X, to.Y, 0));
        Raise(form, "Canvas_MouseUp", new MouseEventArgs(MouseButtons.Left, 1, to.X, to.Y, 0));

        // Assert
        Assert.Equal("Window zones *", form.Text);
        Assert.NotNull(canvas);

        // And saving it writes the zones moved
        Raise(form, "SaveButton_Click", EventArgs.Empty);
        IReadOnlyList<ZoneModel> saved =
            ZonesConfigReader.Read(File.ReadAllText(_zones.FilePath)).Layouts.DefaultLayout.Zones;
        Assert.InRange(saved[0].Width, 74, 76);
        Assert.Equal("Window zones", form.Text);
    }

    // Where the form draws the monitor, in the canvas
    private static Rectangle Monitor(ZoneEditorForm form) =>
        (Rectangle)typeof(ZoneEditorForm)
            .GetMethod("MonitorArea", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, null)!;

    private static void Raise(ZoneEditorForm form, string handler, EventArgs args) =>
        typeof(ZoneEditorForm)
            .GetMethod(handler, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, [form, args]);

    private sealed class FakeDesktop : Ui.Zones.Interfaces.IZoneDesktop
    {
        private static readonly Rectangle _monitor = new(0, 0, 1600, 900);

        public IReadOnlyList<ZoneMonitorModel> GetMonitors() => [new(_monitor, _monitor)];

        public ZoneMonitorModel GetMonitorOf(nint window) => new(_monitor, _monitor);

        public nint GetActiveWindow() => 0;

        public Rectangle GetWindowBounds(nint window) => Rectangle.Empty;

        public string? MoveWindow(nint window, Rectangle bounds) => null;
    }
}
