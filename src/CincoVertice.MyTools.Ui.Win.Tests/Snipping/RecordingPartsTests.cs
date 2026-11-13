using System.Drawing;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Win.Snipping.Models;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Snipping;

/// <summary>What a recording shows of keys and clicks, and recording.cfg.</summary>
public class RecordingPartsTests
{
    [Theory]
    [InlineData(Keys.P, Keys.Control | Keys.Shift, "Ctrl+Shift+P")]
    [InlineData(Keys.D5, Keys.Alt, "Alt+5")]
    [InlineData(Keys.E, Keys.LWin, "Win+E")]
    [InlineData(Keys.Enter, Keys.None, "Enter")]
    [InlineData(Keys.Escape, Keys.None, "Esc")]
    [InlineData(Keys.Tab, Keys.Shift, "Shift+Tab")]
    [InlineData(Keys.F5, Keys.None, "F5")]
    [InlineData(Keys.Delete, Keys.Control | Keys.Alt, "Ctrl+Alt+Del")]
    [InlineData(Keys.Left, Keys.Control, "Ctrl+Left")]
    public void KeyComboText_IsTheShortcut_AsOnTheKeyboard(Keys key, Keys modifiers, string expected)
    {
        Assert.Equal(expected, KeyComboText.From(key, modifiers));
    }

    [Theory]
    [InlineData(Keys.A, Keys.None)]
    [InlineData(Keys.A, Keys.Shift)]
    [InlineData(Keys.D1, Keys.None)]
    [InlineData(Keys.Space, Keys.None)]
    [InlineData(Keys.Back, Keys.None)]
    [InlineData(Keys.ControlKey, Keys.Control)]
    [InlineData(Keys.LShiftKey, Keys.Shift)]
    public void KeyComboText_OfTyping_OrAModifierAlone_IsNothing(Keys key, Keys modifiers)
    {
        // Typed text, such as a password, never shows
        Assert.Null(KeyComboText.From(key, modifiers));
    }

    [Fact]
    public void AClick_IsARingWhereItWas_ThatFades()
    {
        // Arrange: the frame starts at (100, 100) on the screen; the click is at (150, 140)
        var overlay = new RecordingOverlay();
        overlay.AddClick(TimeSpan.FromSeconds(1), new Point(150, 140), MouseButtons.Left);

        // Act and assert: a ring around (50, 40) of the frame just after, and nothing once it faded
        Assert.True(YellowAround(overlay, TimeSpan.FromSeconds(1.1)) > 10);
        Assert.Equal(0, YellowAround(overlay, TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public void AShortcut_IsShownInTheBottomLeftCorner_ForAMoment()
    {
        // Arrange
        var overlay = new RecordingOverlay();
        overlay.AddKeys(TimeSpan.FromSeconds(1), "Ctrl+Shift+P");

        // Act and assert: just after, a dark box at the bottom left, nothing at the top right; gone later
        Assert.True(DarkAt(overlay, TimeSpan.FromSeconds(1.2), new Rectangle(0, 200, 160, 100)) > 200);
        Assert.Equal(0, DarkAt(overlay, TimeSpan.FromSeconds(1.2), new Rectangle(240, 0, 160, 100)));
        Assert.Equal(0, DarkAt(overlay, TimeSpan.FromSeconds(3), new Rectangle(0, 200, 160, 100)));
    }

    private static int DarkAt(RecordingOverlay overlay, TimeSpan now, Rectangle area)
    {
        using var frame = new Bitmap(400, 300);
        using (var graphics = Graphics.FromImage(frame))
        {
            graphics.Clear(Color.White);
            overlay.Draw(graphics, new Rectangle(500, 500, 400, 300), now);
        }

        int count = 0;

        for (int x = area.Left; x < area.Right; x++)
        {
            for (int y = area.Top; y < area.Bottom; y++)
            {
                if (frame.GetPixel(x, y).GetBrightness() < 0.5f)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int YellowAround(RecordingOverlay overlay, TimeSpan now)
    {
        using var frame = new Bitmap(200, 150);
        using (var graphics = Graphics.FromImage(frame))
        {
            graphics.Clear(Color.White);
            overlay.Draw(graphics, new Rectangle(100, 100, 200, 150), now);
        }

        int count = 0;

        for (int x = 0; x < 100; x++)
        {
            for (int y = 0; y < 90; y++)
            {
                Color pixel = frame.GetPixel(x, y);

                if (pixel.R > 200 && pixel.G > 150 && pixel.B < 150)
                {
                    count++;
                }
            }
        }

        return count;
    }

    [Fact]
    public void Settings_TheNewFile_HasTheDefaults_WithoutProblems()
    {
        // Act
        RecordingSettingsResultModel result =
            RecordingSettingsReader.Read(RecordingSettingsService.CreateDefaultText());

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(RecordingSettingsModel.Default, result.Settings);
    }

    [Fact]
    public void Settings_TakesEverySetting()
    {
        // Arrange
        string folder = Path.TrimEndingDirectorySeparator(Path.GetTempPath());

        // Act
        RecordingSettingsResultModel result = RecordingSettingsReader.Read(
            "FramesPerSecond = 30\nquality = high\nMaxMinutes = 5 # short\nCountdown = 0\nShowClicks = false\n"
            + $"ShowKeys = FALSE\nFolder = {folder}\nGifFramesPerSecond = 8\nGifMaxWidth = 640");

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(
            new RecordingSettingsModel(30, RecordingQuality.High, 5, 0, false, false, folder, 8, 640),
            result.Settings);
    }

    [Theory]
    [InlineData("FramesPerSecond = 60", "FramesPerSecond: must be a whole number from 5 to 30; the default is used.")]
    [InlineData("Quality = Best", "Quality: must be Low, Medium or High; the default is used.")]
    [InlineData("Countdown = -1", "Countdown: must be a whole number from 0 to 10; the default is used.")]
    [InlineData("ShowKeys = yes", "ShowKeys: must be true or false; the default is used.")]
    [InlineData(
        @"Folder = Q:\No\Such\Folder",
        @"Folder: there's no folder 'Q:\No\Such\Folder'; the Videos folder is used.")]
    public void Settings_AValueThatCantBeUsed_IsAProblem_AndKeepsTheDefault(string line, string message)
    {
        // Act
        RecordingSettingsResultModel result = RecordingSettingsReader.Read("# recording\n" + line);

        // Assert
        Assert.Equal($"Line 2: {message}", Assert.Single(result.Problems));
        Assert.Equal(RecordingSettingsModel.Default, result.Settings);
    }
}
