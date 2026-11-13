using System.Drawing;
using System.Windows.Forms;
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
        Application.DoEvents();

        var recorder = new ScreenRecorder();

        // Act
        await recorder.StartAsync(form.Bounds, _path);
        await Pump(TimeSpan.FromSeconds(1.5));
        await recorder.StopAsync();

        // Assert
        Assert.True(new FileInfo(_path).Length > 1000);
        using Bitmap frame = await FrameAt(TimeSpan.FromSeconds(0.5));
        Assert.Equal(new Size(200, 120), frame.Size);
        AssertNear(Color.Red, frame.GetPixel(30, 20));
        AssertNear(Color.Blue, frame.GetPixel(170, 100));
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
