using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using CincoVertice.MyTools.Ui.Diagramming.Views;
using CincoVertice.MyTools.Ui.Win.Diagramming;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Diagramming;

public class ThemeColorPaletteControlTests
{
    private static DiagramPaletteModel Palette(params DiagramSwatchModel[] recent)
    {
        DiagramThemeModel theme = DiagramThemes.Of("Ocean");

        return new DiagramPaletteModel(
            theme.Name,
            [.. DiagramThemes.All.Select(other => other.Name)],
            DiagramThemes.Grid(theme),
            DiagramThemes.StandardColors,
            recent);
    }

    [Fact]
    public void Palette_ShowsTheThemeGrid_TheStandardColors_AndTheRecentOnes_TenToARow()
    {
        // Arrange
        using var control = new ThemeColorPaletteControl();
        var recent = new DiagramSwatchModel(Color.FromArgb(255, 1, 2, 3), "Used lately");

        // Act
        control.Palette = Palette(recent);

        // Assert: the grid's first square, its last, the first standard color, the recent one
        IReadOnlyList<DiagramSwatchModel> grid = DiagramThemes.Grid(DiagramThemes.Of("Ocean"));
        Assert.Equal(grid[0], control.SwatchAt(control.CenterOf(0)));
        Assert.Equal(grid[59], control.SwatchAt(control.CenterOf(59)));
        Assert.Equal(DiagramThemes.StandardColors[0], control.SwatchAt(control.CenterOf(60)));
        Assert.Equal(recent, control.SwatchAt(control.CenterOf(70)));

        // Ten across, rows below each other
        Assert.Equal(control.CenterOf(0).Y, control.CenterOf(9).Y);
        Assert.True(control.CenterOf(10).Y > control.CenterOf(0).Y);
        Assert.True(control.PreferredSize.Height > control.CenterOf(70).Y);
    }

    [Fact]
    public void ToolTip_SaysWhatTheSquareIs_AndItsHex()
    {
        // Act / Assert
        Assert.Equal(
            "Accent 1 · #1B98E0",
            ThemeColorPaletteControl.ToolTipOf(DiagramThemes.Grid(DiagramThemes.Of("Ocean"))[4]));
    }

    [Fact]
    public void ColorMenus_HaveThePickerTheHexBoxAndTheThemes_AndChoosingOneRaisesIt()
    {
        // Arrange
        using var form = new DiagramEditorForm();
        IDiagramEditorView view = form;
        List<string> themes = [];
        List<DiagramColorChoiceModel> colors = [];
        List<DiagramColorTarget> picks = [];
        view.ThemeSelected += (_, theme) => themes.Add(theme);
        view.ColorSelected += (_, choice) => colors.Add(choice);
        view.ScreenColorRequested += (_, target) => picks.Add(target);
        var line = (ToolStripDropDownButton)typeof(DiagramEditorForm)
            .GetField("LineDropDownButton", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;

        // Act
        view.ShowPalette(Palette());
        ToolStripMenuItem themeMenu = line.DropDownItems.OfType<ToolStripMenuItem>().Last();
        ToolStripTextBox hex = line.DropDownItems.OfType<ToolStripTextBox>().Single();
        themeMenu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Text == "Earth").PerformClick();
        line.DropDownItems.OfType<ToolStripMenuItem>()
            .Single(item => item.Text == "Pick from the screen...")
            .PerformClick();
        hex.Text = "#123456";
        typeof(DiagramEditorForm)
            .GetMethod("HexBox_KeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, [line, hex, DiagramColorTarget.Line, new KeyEventArgs(Keys.Enter)]);

        // Assert
        Assert.Equal("Theme: Ocean", themeMenu.Text);
        Assert.True(themeMenu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Text == "Ocean").Checked);
        Assert.Equal(["Earth"], themes);
        Assert.Equal([DiagramColorTarget.Line], picks);
        Assert.Equal(
            [new DiagramColorChoiceModel(DiagramColorTarget.Line, Color.FromArgb(255, 0x12, 0x34, 0x56))],
            colors);
        Assert.Equal(string.Empty, hex.Text);
    }
}
