using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Snipping;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Snipping;

/// <summary>Shows the bar and the frame for a moment. Alone: another test's window could take the focus.</summary>
[Collection(KeyboardFocusCollection.Name)]
public class RecordingBarFormTests
{
    private static T Field<T>(RecordingBarForm form, string name) =>
        (T)typeof(RecordingBarForm).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;

    [Fact]
    public void ShowRecording_FramesTheArea_AndPutsTheBarBelowIt_ShowingTheTime()
    {
        // Arrange: an area in the middle of the main screen, with room below it
        Rectangle screen = Screen.PrimaryScreen!.WorkingArea;
        var area = new Rectangle(screen.Left + 100, screen.Top + 100, 300, 200);
        using var bar = new RecordingBarForm(new TrayNotificationService());

        // Act
        bar.ShowRecording(area);
        bar.ShowElapsed(TimeSpan.FromSeconds(75));
        Application.DoEvents();

        // Assert
        var frame = Field<RecordingFrameForm>(bar, "_frame");
        Assert.True(bar.Visible);
        Assert.True(frame.Visible);
        Assert.Equal(Rectangle.Inflate(area, RecordingFrameForm.Thickness, RecordingFrameForm.Thickness), frame.Bounds);
        Assert.True(bar.Top > area.Bottom);
        Assert.Equal("● 1:15", Field<Label>(bar, "TimeLabel").Text);

        // And closing the bar closes the frame
        bar.CloseView();
        Assert.True(frame.IsDisposed);
    }

    [Fact]
    public void ShowSaving_StopsTheTime_AndTheStopButton()
    {
        // Arrange
        using var bar = new RecordingBarForm(new TrayNotificationService());

        // Act
        bar.ShowSaving();

        // Assert
        Assert.Equal("Saving...", Field<Label>(bar, "TimeLabel").Text);
        Assert.False(Field<Button>(bar, "StopButton").Enabled);
    }

    [Fact]
    public void TheStopButton_AsksToStop()
    {
        // Arrange
        using var bar = new RecordingBarForm(new TrayNotificationService());
        int stops = 0;
        bar.StopRequested += (_, _) => stops++;

        // Act
        typeof(RecordingBarForm)
            .GetMethod("StopButton_Click", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(bar, [bar, EventArgs.Empty]);

        // Assert
        Assert.Equal(1, stops);
    }
}
