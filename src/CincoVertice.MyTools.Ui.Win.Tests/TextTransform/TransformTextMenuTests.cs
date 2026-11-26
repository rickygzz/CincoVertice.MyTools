using System.Windows.Forms;
using CincoVertice.MyTools.Ui.TextTransform.Models;
using CincoVertice.MyTools.Ui.TextTransform.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.TextTransform.Actions;
using CincoVertice.MyTools.Ui.Win.TextTransform.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.TextTransform;

public class TransformTextMenuTests
{
    [Fact]
    public void CreateMenu_HasEachTransform_InItsOrder_TheGroupsBetweenLines()
    {
        // Act
        using ContextMenuStrip menu = DesktopTextTransformHost.CreateMenu(TextTransformer.All);

        // Assert: case, lines, encodings, JSON
        Assert.Equal(
            TextTransformer.All.Select(transform => transform.Kind),
            menu.Items.OfType<ToolStripMenuItem>().Select(item => (TextTransformKind)item.Tag!));
        Assert.Equal(3, menu.Items.OfType<ToolStripSeparator>().Count());
        Assert.Equal("&UPPER CASE", menu.Items[0].Text);
    }

    [Fact]
    public void Action_IsTransformText_OnCtrlAltX()
    {
        // Act
        var action = new TransformTextAction(new TrayNotificationService());

        // Assert: hotkeys.cfg refers to the name
        Assert.Equal(("TransformText", "Ctrl+Alt+X"), (action.Name, action.DefaultShortcut));
    }
}
