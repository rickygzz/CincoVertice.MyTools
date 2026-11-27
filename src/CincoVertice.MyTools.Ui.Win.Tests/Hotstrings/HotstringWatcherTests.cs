using System.Windows.Forms;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Hotstrings.Models;
using CincoVertice.MyTools.Ui.Hotstrings.Services;
using CincoVertice.MyTools.Ui.Win.Hotstrings.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Hotstrings;

/// <summary>The keys as the keyboard hook gets them, turned into what's typed.</summary>
public class HotstringWatcherTests
{
    private static readonly HotstringModel _btw = new("btw", "by the way", 1);
    private static readonly HotstringModel _addr1 = new("addr1", "221B Baker Street", 2);

    private readonly HotstringTracker _tracker = new();

    // The keys held down, as Windows would say
    private readonly HashSet<KeyCode> _held = [];

    public HotstringWatcherTests()
    {
        _tracker.Use([_btw, _addr1]);
    }

    private HotstringModel? Press(params Keys[] keys)
    {
        HotstringModel? found = null;

        foreach (Keys key in keys)
        {
            found = HotstringWatcher.Follow(_tracker, key, _held.Contains);
        }

        return found;
    }

    [Theory]
    [InlineData(Keys.Space)]
    [InlineData(Keys.Return)]
    [InlineData(Keys.Tab)]
    public void LettersThenAnEndingKey_FindTheHotstring(Keys ending)
    {
        Assert.Equal(_btw, Press(Keys.B, Keys.T, Keys.W, ending));
    }

    [Fact]
    public void ShiftForCapitals_IsTyping_AndDigitsOfBothKinds_Count()
    {
        // Arrange
        _held.Add(KeyCode.VK_SHIFT);

        // Act
        HotstringModel? capitals = Press(Keys.LShiftKey, Keys.B, Keys.T, Keys.W, Keys.Space);
        _held.Clear();
        HotstringModel? topRow = Press(Keys.A, Keys.D, Keys.D, Keys.R, Keys.D1, Keys.Space);
        HotstringModel? numPad = Press(Keys.A, Keys.D, Keys.D, Keys.R, Keys.NumPad1, Keys.Space);

        // Assert
        Assert.Equal(_btw, capitals);
        Assert.Equal(_addr1, topRow);
        Assert.Equal(_addr1, numPad);
    }

    [Fact]
    public void ShiftWithADigit_IsASymbol_AndEndsTheWord()
    {
        // Arrange: "addr!" on most layouts
        Press(Keys.A, Keys.D, Keys.D, Keys.R);
        _held.Add(KeyCode.VK_SHIFT);

        // Act
        HotstringModel? found = Press(Keys.D1, Keys.Space);

        // Assert
        Assert.Null(found);
    }

    [Theory]
    [InlineData(KeyCode.VK_CONTROL)]
    [InlineData(KeyCode.VK_MENU)]
    [InlineData(KeyCode.VK_LWIN)]
    public void AShortcut_StartsANewWord(KeyCode modifier)
    {
        // Arrange: Ctrl+Z, say, between the letters
        Press(Keys.B, Keys.T);
        _held.Add(modifier);
        Press(Keys.Z);
        _held.Clear();

        // Act
        HotstringModel? found = Press(Keys.W, Keys.Space);

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void Backspace_Erases_AndOtherKeys_StartANewWord()
    {
        // Act
        HotstringModel? corrected = Press(Keys.B, Keys.X, Keys.Back, Keys.T, Keys.W, Keys.Space);
        HotstringModel? afterADot = Press(Keys.B, Keys.OemPeriod, Keys.T, Keys.W, Keys.Space);
        HotstringModel? afterAnArrow = Press(Keys.B, Keys.T, Keys.Left, Keys.W, Keys.Space);

        // Assert
        Assert.Equal(_btw, corrected);
        Assert.Null(afterADot);
        Assert.Null(afterAnArrow);
    }
}
