using System.Drawing;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Behaviors;

public class CursorScreenMessageBoxTests
{
    [Fact]
    public void Owner_IsInTheMiddleOfThePointersScreen_InFront_AndCantBeSeen()
    {
        // Arrange
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;

        // Act
        using Form owner = CursorScreenMessageBox.CreateOwner();

        // Assert: a message box is centered on its owner, so on that screen
        Assert.Equal(new Point(area.Left + area.Width / 2, area.Top + area.Height / 2), owner.Location);
        Assert.Equal(FormStartPosition.Manual, owner.StartPosition);
        Assert.True(owner.TopMost);
        Assert.False(owner.ShowInTaskbar);
        Assert.Equal(0, owner.Opacity);
    }
}
