using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.ColorPicker.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.ColorPicker;

public class ColorTextTests
{
    [Theory]
    [InlineData(30, 144, 255, "#1E90FF", "rgb(30, 144, 255)", "hsl(210, 100%, 56%)")]
    [InlineData(0, 0, 0, "#000000", "rgb(0, 0, 0)", "hsl(0, 0%, 0%)")]
    [InlineData(255, 255, 255, "#FFFFFF", "rgb(255, 255, 255)", "hsl(0, 0%, 100%)")]
    [InlineData(255, 0, 0, "#FF0000", "rgb(255, 0, 0)", "hsl(0, 100%, 50%)")]
    [InlineData(128, 128, 128, "#808080", "rgb(128, 128, 128)", "hsl(0, 0%, 50%)")]
    public void Formats_WriteTheColor(int r, int g, int b, string hex, string rgb, string hsl)
    {
        // Arrange
        Color color = Color.FromArgb(r, g, b);

        // Act / Assert
        Assert.Equal(hex, ColorText.Hex(color));
        Assert.Equal(rgb, ColorText.Rgb(color));
        Assert.Equal(hsl, ColorText.Hsl(color));
    }

    [Fact]
    public void CopiedMessage_SaysWhatWasCopied_ThenTheOtherWays()
    {
        // Arrange
        var picked = new PickedColorModel(Color.FromArgb(30, 144, 255), ColorTextFormat.Rgb);

        // Act
        string message = ColorText.CopiedMessage(picked);

        // Assert
        Assert.Equal("Copied rgb(30, 144, 255)\n#1E90FF  ·  hsl(210, 100%, 56%)", message);
    }
}
