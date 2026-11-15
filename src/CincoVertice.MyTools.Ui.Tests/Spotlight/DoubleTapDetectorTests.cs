using CincoVertice.MyTools.Ui.Spotlight.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Spotlight;

public class DoubleTapDetectorTests
{
    private readonly DoubleTapDetector _detector = new();

    [Fact]
    public void TwoQuickTaps_AreADoubleTap()
    {
        // Act
        bool first = Tap(0, 80);
        bool second = Tap(250, 330);

        // Assert
        Assert.False(first);
        Assert.True(second);
    }

    [Fact]
    public void AThirdTap_StartsOver()
    {
        // Arrange
        Tap(0, 80);
        Tap(200, 280);

        // Act
        bool third = Tap(400, 480);

        // Assert
        Assert.False(third);
    }

    [Fact]
    public void TapsTooFarApart_AreNot()
    {
        // Arrange
        Tap(0, 80);

        // Act
        bool second = Tap(80 + 401, 560);

        // Assert
        Assert.False(second);
    }

    [Fact]
    public void AKeyHeldDown_IsNoTap_EvenWithItsRepeats()
    {
        // Arrange
        Tap(0, 80);

        // Act
        _detector.KeyDown(true, Ms(200));
        _detector.KeyDown(true, Ms(450));
        _detector.KeyDown(true, Ms(480));
        bool second = _detector.KeyUp(true, Ms(600));

        // Assert
        Assert.False(second);
    }

    [Fact]
    public void AnotherKey_InBetween_StopsIt()
    {
        // Arrange: Ctrl+C, then Ctrl
        _detector.KeyDown(true, Ms(0));
        _detector.KeyDown(false, Ms(50));
        _detector.KeyUp(false, Ms(90));
        _detector.KeyUp(true, Ms(120));

        // Act
        bool second = Tap(250, 320);

        // Assert
        Assert.False(second);
    }

    [Fact]
    public void AClick_InBetween_StopsIt()
    {
        // Arrange
        Tap(0, 80);
        _detector.Interrupt();

        // Act
        bool second = Tap(200, 280);

        // Assert
        Assert.False(second);
    }

    [Fact]
    public void AnotherKeyLetGoOf_DoesNotCount()
    {
        // Arrange: a key held from before the taps
        Tap(0, 80);
        _detector.KeyUp(false, Ms(120));

        // Act
        bool second = Tap(200, 280);

        // Assert
        Assert.True(second);
    }

    private bool Tap(int downMs, int upMs)
    {
        _detector.KeyDown(true, Ms(downMs));

        return _detector.KeyUp(true, Ms(upMs));
    }

    private static TimeSpan Ms(int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
}
