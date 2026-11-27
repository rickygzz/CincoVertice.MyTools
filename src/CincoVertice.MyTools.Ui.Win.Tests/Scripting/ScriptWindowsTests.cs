using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using CincoVertice.Common.Application.Scripting.Models;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Scripting;

/// <summary>
///     On a real window, of a thread with its own message loop, as another program's window is: changes are asked
///     without waiting, and done by that loop. Alone, since minimizing a window puts another in front, which tests of
///     the foreground would see.
/// </summary>
[Collection(KeyboardFocusCollection.Name)]
public class ScriptWindowsTests
{
    /// <summary>A window shown off the screen on a thread of its own, until it's closed.</summary>
    private static (nint Window, Thread Thread) ShowWindow(string title)
    {
        nint handle = 0;
        using var shown = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            using var form = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-3000, -3000),
                Size = new Size(300, 200),
            };
            form.Shown += (_, _) =>
            {
                handle = form.Handle;
                shown.Set();
            };
            Application.Run(form);
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        Assert.True(shown.Wait(TimeSpan.FromSeconds(10)));

        return (handle, thread);
    }

    [Fact]
    public async Task Window_IsFoundHiddenShownMinimizedMaximizedRestored_AndClosed()
    {
        // Arrange
        string title = $"Script window test {Guid.NewGuid():N}";
        (nint window, Thread thread) = ShowWindow(title);
        using Process self = Process.GetCurrentProcess();
        List<ScriptWindowState> states = [];

        // Act
        nint found = ScriptWindows.ByTitle(title);
        bool isOfProcess = ScriptWindows.OfProcess(self.ProcessName).Contains(window);
        bool isRunning = ScriptWindows.IsProcessRunning(self.ProcessName);

        foreach (ScriptWindowChange change in new[]
        {
            ScriptWindowChange.Hide, ScriptWindowChange.Show, ScriptWindowChange.Minimize,
            ScriptWindowChange.Maximize, ScriptWindowChange.Restore,
        })
        {
            Assert.True(await ScriptWindows.ChangeAsync(window, change));
            states.Add(ScriptWindows.StateOf(window));

            if (change == ScriptWindowChange.Hide)
            {
                // Still found, to be shown again
                Assert.Equal(window, ScriptWindows.ByTitle(title));
            }
        }

        await ScriptWindows.ChangeAsync(window, ScriptWindowChange.Close);
        bool ended = thread.Join(TimeSpan.FromSeconds(10));

        // Assert
        Assert.Equal(window, found);
        Assert.True(isOfProcess);
        Assert.True(isRunning);
        Assert.Equal(
            [
                ScriptWindowState.Hidden, ScriptWindowState.Normal, ScriptWindowState.Minimized,
                ScriptWindowState.Maximized, ScriptWindowState.Normal,
            ],
            states);
        Assert.True(ended);
        Assert.Equal(ScriptWindowState.Closed, ScriptWindows.StateOf(window));
        Assert.False(await ScriptWindows.ChangeAsync(window, ScriptWindowChange.Hide));
        Assert.Equal(0, ScriptWindows.ByTitle(title));
    }

    [Fact]
    public async Task OnTop_SwitchesTheWindow_AndOfAClosedOneIsAnError()
    {
        // Arrange
        (nint window, Thread thread) = ShowWindow($"Script on top test {Guid.NewGuid():N}");

        // Act
        bool on = ScriptWindows.SetOnTop(window, null);
        bool off = ScriptWindows.SetOnTop(window, false);
        await ScriptWindows.ChangeAsync(window, ScriptWindowChange.Close);
        thread.Join(TimeSpan.FromSeconds(10));

        // Assert
        Assert.True(on);
        Assert.False(off);
        var error = Assert.Throws<ArgumentException>(() => ScriptWindows.SetOnTop(window, true));
        Assert.Equal(ScriptWindows.Closed, error.Message);
    }

    [Fact]
    public async Task Bounds_AreMovedAndSized_AsSeen_AndTheProgramIsNamed_AndOfAClosedOneThereAreNone()
    {
        // Arrange
        (nint window, Thread thread) = ShowWindow($"Script bounds test {Guid.NewGuid():N}");
        using Process self = Process.GetCurrentProcess();
        var bounds = new Rectangle(-2900, -2950, 420, 260);

        // Act
        ScriptWindows.SetBounds(window, bounds);
        Rectangle? moved = ScriptWindows.BoundsOf(window);
        string program = ScriptWindows.ProcessOf(window);
        await ScriptWindows.ChangeAsync(window, ScriptWindowChange.Close);
        thread.Join(TimeSpan.FromSeconds(10));

        // Assert: what's seen of it, without the invisible borders
        Assert.Equal(bounds, moved);
        Assert.Equal(self.ProcessName, program);
        Assert.Null(ScriptWindows.BoundsOf(window));
        Assert.Equal(string.Empty, ScriptWindows.ProcessOf(window));
        Assert.Equal(
            ScriptWindows.Closed,
            Assert.Throws<ArgumentException>(() => ScriptWindows.SetBounds(window, bounds)).Message);
    }

    [Fact]
    public void Program_NotRunning_HasNoWindows()
    {
        // Act
        string name = $"no-such-program-{Guid.NewGuid():N}";

        // Assert
        Assert.False(ScriptWindows.IsProcessRunning(name));
        Assert.Empty(ScriptWindows.OfProcess(name));
    }
}
