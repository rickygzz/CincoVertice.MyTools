using System.Drawing;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;
using Windows.Media.Editing;
using Windows.Storage;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Snipping;

/// <summary>
///     With Windows' own encoder, recording a window shown on the screen for a moment. Alone: another test's window
///     could cover it.
/// </summary>
[Collection(KeyboardFocusCollection.Name)]
public sealed class ScreenRecorderTests : IDisposable
{
    // Without clicks or shortcuts, which would draw over the colors checked
    private static readonly RecordingSettingsModel _plain =
        RecordingSettingsModel.Default with { ShowClicks = false, ShowKeys = false };

    private readonly string _path = Path.Combine(Path.GetTempPath(), "ScreenRecorderTests_" + Guid.NewGuid() + ".mp4");

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    [Fact]
    public async Task Recording_WritesAnMp4OfTheArea_RightSideUp_InItsColors()
    {
        // Arrange: red on the top left, blue on the bottom right
        Rectangle screen = Screen.PrimaryScreen!.WorkingArea;
        using var form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            TopMost = true,
            BackColor = Color.Blue,
            Bounds = new Rectangle(screen.Left + 40, screen.Top + 40, 200, 120),
        };
        form.Controls.Add(new Panel { BackColor = Color.Red, Bounds = new Rectangle(0, 0, 100, 60) });
        form.Show();
        Settle();

        var recorder = new ScreenRecorder();

        // Act: with the pointer away, as it's in the video too
        using (MovePointer(new Point(screen.Right - 20, screen.Bottom - 20)))
        {
            await recorder.StartAsync(form.Bounds, _path, _plain);
            await Pump(TimeSpan.FromSeconds(1.5));
            await recorder.StopAsync();
        }

        // Assert
        Assert.True(new FileInfo(_path).Length > 1000);
        using Bitmap frame = await FrameAt(TimeSpan.FromSeconds(0.5));
        Assert.Equal(new Size(200, 120), frame.Size);
        AssertNear(Color.Red, frame.GetPixel(30, 20));
        AssertNear(Color.Blue, frame.GetPixel(170, 100));
    }

    [Fact]
    public async Task Recording_HasTheMousePointer_WhereItIs()
    {
        // Arrange: a white window, with the pointer in its middle
        Rectangle screen = Screen.PrimaryScreen!.WorkingArea;
        using var form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            TopMost = true,
            BackColor = Color.White,
            Bounds = new Rectangle(screen.Left + 300, screen.Top + 40, 200, 120),
        };
        form.Show();
        Application.DoEvents();

        var recorder = new ScreenRecorder();

        // Act
        using (MovePointer(new Point(form.Left + 100, form.Top + 60)))
        {
            await recorder.StartAsync(form.Bounds, _path, _plain);
            await Pump(TimeSpan.FromSeconds(1.5));
            await recorder.StopAsync();
        }

        // Assert: the pointer's arrow, at the middle, and nothing at the corner
        using Bitmap frame = await FrameAt(TimeSpan.FromSeconds(0.5));
        Assert.True(Dark(frame, new Rectangle(98, 58, 20, 30)) > 10, "The pointer isn't in the video.");
        Assert.Equal(0, Dark(frame, new Rectangle(0, 0, 40, 40)));
    }

    // Puts the pointer there until disposed, then back where it was
    private static IDisposable MovePointer(Point point)
    {
        Point before = Cursor.Position;
        Cursor.Position = point;
        Application.DoEvents();

        return new PointerBack(before);
    }

    private static int Dark(Bitmap image, Rectangle area)
    {
        int count = 0;

        for (int x = area.Left; x < area.Right; x++)
        {
            for (int y = area.Top; y < area.Bottom; y++)
            {
                if (image.GetPixel(x, y).GetBrightness() < 0.5f)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private sealed class PointerBack(Point position) : IDisposable
    {
        public void Dispose() => Cursor.Position = position;
    }

    // Until the window is drawn on the screen, and the one of the test before is gone
    private static void Settle()
    {
        for (int i = 0; i < 20; i++)
        {
            Application.DoEvents();
            Thread.Sleep(15);
        }
    }

    // Keeps the window drawn while the recorder copies it
    private static async Task Pump(TimeSpan time)
    {
        DateTime end = DateTime.UtcNow + time;

        while (DateTime.UtcNow < end)
        {
            Application.DoEvents();
            await Task.Delay(20, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task APause_IsLeftOutOfTheVideo()
    {
        // Arrange
        using Form form = ShowWhite(new Point(40, 220), new Size(200, 120));
        var recorder = new ScreenRecorder();

        // Act: a second, a second paused, then another second
        await recorder.StartAsync(form.Bounds, _path, _plain);
        await Pump(TimeSpan.FromSeconds(1));
        recorder.Pause();
        await Pump(TimeSpan.FromSeconds(1));
        recorder.Resume();
        await Pump(TimeSpan.FromSeconds(1));
        await recorder.StopAsync();

        // Assert
        TimeSpan length = await LengthOf(_path);
        Assert.InRange(length.TotalSeconds, 1.7, 2.5);
    }

    [Fact]
    public async Task SavingAsAGif_KeepsItsFramesPerSecond_AtItsMostWidth_PlayingOverAndOver()
    {
        // Arrange: a 2 second video, 400 wide
        using Form form = ShowWhite(new Point(300, 220), new Size(400, 200));
        var recorder = new ScreenRecorder();
        await recorder.StartAsync(form.Bounds, _path, _plain);
        await Pump(TimeSpan.FromSeconds(2));
        await recorder.StopAsync();
        string gif = Path.ChangeExtension(_path, ".gif");
        var settings = _plain with { GifFramesPerSecond = 5, GifMaxWidth = 200 };

        try
        {
            // Act
            await new RecordingFileStore().KeepAsync(_path, gif, settings);

            // Assert: about 10 frames of 20 hundredths, half the size, and the video gone
            using var image = Image.FromFile(gif);
            int frames = image.GetFrameCount(System.Drawing.Imaging.FrameDimension.Time);
            Assert.InRange(frames, 8, 12);
            Assert.Equal(new Size(200, 100), image.Size);
            Assert.Equal(20, BitConverter.ToInt32(image.GetPropertyItem(0x5100)!.Value!, 0));
            Assert.Equal(0, BitConverter.ToInt16(image.GetPropertyItem(0x5101)!.Value!, 0));
            Assert.False(File.Exists(_path));
        }
        finally
        {
            File.Delete(gif);
        }
    }

    // Each test's window at a place of its own: disposed on another thread than its own, after the awaits, a
    // window stays on the screen until its thread ends, and would be in the next test's video
    private static Form ShowWhite(Point offset, Size size)
    {
        Rectangle screen = Screen.PrimaryScreen!.WorkingArea;
        var form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            TopMost = true,
            BackColor = Color.White,
            Bounds = new Rectangle(new Point(screen.Left + offset.X, screen.Top + offset.Y), size),
        };
        form.Show();
        Settle();

        return form;
    }

    private static async Task<TimeSpan> LengthOf(string path)
    {
        StorageFile file = await StorageFile.GetFileFromPathAsync(path);
        MediaClip clip = await MediaClip.CreateFromFileAsync(file);

        return clip.OriginalDuration;
    }

    private async Task<Bitmap> FrameAt(TimeSpan time)
    {
        StorageFile file = await StorageFile.GetFileFromPathAsync(_path);
        var composition = new MediaComposition();
        composition.Clips.Add(await MediaClip.CreateFromFileAsync(file));

        using var image = await composition.GetThumbnailAsync(time, 0, 0, VideoFramePrecision.NearestFrame);
        using Stream stream = image.AsStreamForRead();
        using var decoded = new Bitmap(stream);

        // A copy: GDI+ reads from the stream a bitmap was made from, which closes here
        return new Bitmap(decoded);
    }

    // The encoder blurs colors a little
    private static void AssertNear(Color expected, Color actual)
    {
        Assert.True(
            Math.Abs(expected.R - actual.R) < 40
            && Math.Abs(expected.G - actual.G) < 40
            && Math.Abs(expected.B - actual.B) < 40,
            $"Expected about {expected}, got {actual}");
    }
}
