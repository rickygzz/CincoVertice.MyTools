using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.ColorPicker;

public class ColorTextTests
{
    [Theory]
    [InlineData(30, 144, 255, "#1E90FF", "rgb(30, 144, 255)", "hsl(210, 100%, 56%)", "hsv(210, 88%, 100%)")]
    [InlineData(0, 0, 0, "#000000", "rgb(0, 0, 0)", "hsl(0, 0%, 0%)", "hsv(0, 0%, 0%)")]
    [InlineData(255, 255, 255, "#FFFFFF", "rgb(255, 255, 255)", "hsl(0, 0%, 100%)", "hsv(0, 0%, 100%)")]
    [InlineData(255, 0, 0, "#FF0000", "rgb(255, 0, 0)", "hsl(0, 100%, 50%)", "hsv(0, 100%, 100%)")]
    [InlineData(128, 128, 128, "#808080", "rgb(128, 128, 128)", "hsl(0, 0%, 50%)", "hsv(0, 0%, 50%)")]
    public void Formats_WriteTheColor(int r, int g, int b, string hex, string rgb, string hsl, string hsv)
    {
        // Arrange
        Color color = Color.FromArgb(r, g, b);

        // Act / Assert
        Assert.Equal(hex, ColorText.Hex(color));
        Assert.Equal(rgb, ColorText.Rgb(color));
        Assert.Equal(hsl, ColorText.Hsl(color));
        Assert.Equal(hsv, ColorText.Hsv(color));
    }

    [Theory]
    [InlineData("#1E90FF", 30, 144, 255)]
    [InlineData(" 1e90ff ", 30, 144, 255)]
    [InlineData("#19F", 17, 153, 255)]
    public void TryParseHex_ReadsSixOrThreeDigits_WithOrWithoutHash(string text, int r, int g, int b)
    {
        // Act / Assert
        Assert.True(ColorText.TryParseHex(text, out Color color));
        Assert.Equal(Color.FromArgb(255, r, g, b), color);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("#12345")]
    [InlineData("#GG0000")]
    [InlineData("blue")]
    public void TryParseHex_AnythingElse_IsntAColor(string? text)
    {
        // Act / Assert
        Assert.False(ColorText.TryParseHex(text, out _));
    }
}
