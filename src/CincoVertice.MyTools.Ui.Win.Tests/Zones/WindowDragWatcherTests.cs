using System.Drawing;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Win.Zones.Services;
using CincoVertice.MyTools.Ui.Zones.Models;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Zones;

public class WindowDragWatcherTests
{
    [Theory]
    [InlineData(Keys.Escape, false, true)]
    [InlineData(Keys.ShiftKey, false, false)]
    [InlineData(Keys.ShiftKey, true, true)]
    public void FinishDrag_DeliversFinalPositionAndRememberedEscape_BeforeEnding(
        Keys key, bool isEscapeDown, bool expectedCancellation)
    {
        using var watcher = new WindowDragWatcher();
        List<string> events = [];
        ZoneDragPointerModel? last = null;
        watcher.DragMoved += (_, pointer) => { last = pointer; events.Add("move"); };
        watcher.DragEnded += (_, _) => events.Add("end");

        // No timer tick between the key press and Windows ending the drag; Esc may already be released.
        watcher.OnDragKeyDown(null, key);
        watcher.FinishDrag(new ZoneDragPointerModel(new Point(50, 60), false, IsEscapeDown: isEscapeDown));

        Assert.Equal(["move", "end"], events);
        Assert.NotNull(last);
        Assert.Equal(new Point(50, 60), last.Cursor);
        Assert.Equal(expectedCancellation, last.IsEscapeDown);
    }
}
