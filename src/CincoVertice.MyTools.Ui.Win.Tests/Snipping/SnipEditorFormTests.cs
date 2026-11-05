using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Win.Snipping;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Snipping;

public sealed class SnipEditorFormTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "SnipEditorFormTests_" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static Bitmap Solid(int width, int height, Color color)
    {
        var snip = new Bitmap(width, height);

        using var graphics = Graphics.FromImage(snip);

        graphics.Clear(color);

        return snip;
    }

    private static bool IsDisposed(Bitmap snip)
    {
        try
        {
            _ = snip.Width;

            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private static T Field<T>(SnipEditorForm form, string name) =>
        (T)typeof(SnipEditorForm).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;

    private string SavedImagePath()
    {
        Directory.CreateDirectory(_folder);

        return Path.Combine(_folder, Guid.NewGuid() + ".png");
    }

    [Fact]
    public void Dispose_DisposesEverySnip()
    {
        // Arrange
        Bitmap first = Solid(50, 40, Color.Red);
        Bitmap second = Solid(60, 30, Color.Blue);
        var form = new SnipEditorForm(first);
        form.AddSnip(second);

        // Act
        form.Dispose();

        // Assert
        Assert.True(IsDisposed(first));
        Assert.True(IsDisposed(second));
    }

    [Fact]
    public void AddSnip_LeavesTheOthersInMemory_ForGoingBack()
    {
        // Arrange
        Bitmap first = Solid(50, 40, Color.Red);
        using var form = new SnipEditorForm(first);

        // Act
        form.AddSnip(Solid(60, 30, Color.Blue));

        // Assert
        Assert.False(IsDisposed(first));
    }

    [Fact]
    public void AddSnip_RaisesSnipAdded_ShowingNothingYet()
    {
        // Arrange: the presenter says what to show, and may not be there at all
        Bitmap first = Solid(50, 40, Color.Red);
        using var form = new SnipEditorForm(first);
        int added = 0;
        form.SnipAdded += (_, _) => added++;

        // Act
        form.AddSnip(Solid(60, 30, Color.Blue));

        // Assert
        Assert.Equal(1, added);
        Assert.Same(first, Field<PictureBox>(form, "Canvas").Image);
    }

    [Fact]
    public void AddSnip_EndsTheTextBeingTyped_BeforeTheNewSnip()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(50, 40, Color.Red));
        List<string> happened = [];
        form.TextCommitted += (_, input) => happened.Add($"text {input.Text}");
        form.SnipAdded += (_, _) => happened.Add("snip");
        form.BeginTextInput(new Point(1, 2), Color.Black, 14);
        Field<TextBox>(form, "TextInputBox").Text = "typed";

        // Act
        form.AddSnip(Solid(60, 30, Color.Blue));

        // Assert: it goes on the snip it was typed over
        Assert.Equal(["text typed", "snip"], happened);
    }

    [Fact]
    public void ShowSnip_ShowsThatOne_AndSavesThatOne()
    {
        // Arrange
        Bitmap first = Solid(50, 40, Color.Red);
        Bitmap second = Solid(60, 30, Color.Blue);
        using var form = new SnipEditorForm(first);
        string firstPath = SavedImagePath();
        string secondPath = SavedImagePath();
        form.AddSnip(second);

        // Act & Assert
        form.ShowSnip(1, 2);
        Assert.Same(second, Field<PictureBox>(form, "Canvas").Image);
        form.SaveImage(secondPath);

        form.ShowSnip(0, 2);
        Assert.Same(first, Field<PictureBox>(form, "Canvas").Image);
        form.SaveImage(firstPath);

        using var savedFirst = new Bitmap(firstPath);
        using var savedSecond = new Bitmap(secondPath);
        Assert.Equal(new Size(50, 40), savedFirst.Size);
        Assert.Equal(Color.Red.ToArgb(), savedFirst.GetPixel(5, 5).ToArgb());
        Assert.Equal(new Size(60, 30), savedSecond.Size);
        Assert.Equal(Color.Blue.ToArgb(), savedSecond.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void ShowSnip_SaysWhichOne_AndOnlyLetsYouGoWhereThereAreSnips()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(50, 40, Color.Red));
        form.AddSnip(Solid(60, 30, Color.Blue));
        form.AddSnip(Solid(70, 30, Color.Green));

        // Act
        form.ShowSnip(0, 3);

        // Assert
        Assert.Equal("1 / 3", Field<ToolStripLabel>(form, "SnipPositionLabel").Text);
        Assert.False(Field<ToolStripButton>(form, "PreviousSnipButton").Enabled);
        Assert.True(Field<ToolStripButton>(form, "NextSnipButton").Enabled);

        // Act
        form.ShowSnip(2, 3);

        // Assert
        Assert.True(Field<ToolStripButton>(form, "PreviousSnipButton").Enabled);
        Assert.False(Field<ToolStripButton>(form, "NextSnipButton").Enabled);
    }

    /// <summary>A window with two snips, the first shown, with its upper left corner there.</summary>
    private static SnipEditorForm WithTwoSnips(Size second, Point location)
    {
        // Wider than the toolbar, which the window never gets narrower than
        var form = new SnipEditorForm(Solid(900, 300, Color.Red));

        form.AddSnip(new Bitmap(second.Width, second.Height));
        form.Location = location;

        return form;
    }

    private static Point NearTheTopLeft()
    {
        Rectangle area = Screen.PrimaryScreen!.WorkingArea;

        return new Point(area.Left + 20, area.Top + 20);
    }

    [Fact]
    public void GoingToAnotherSnip_FitsTheWindowToIt_KeepingItsUpperLeftCorner()
    {
        // Arrange
        Point corner = NearTheTopLeft();
        using SnipEditorForm form = WithTwoSnips(new Size(1000, 450), corner);
        int toolbar = Field<ToolStrip>(form, "Tools").Height;

        // Act
        form.ShowSnip(1, 2);

        // Assert
        Assert.Equal(corner, form.Location);
        Assert.Equal(450 + toolbar, form.ClientSize.Height);
        Assert.Equal(1000, form.ClientSize.Width);

        // Act: back to the smaller one
        form.ShowSnip(0, 2);

        // Assert
        Assert.Equal(corner, form.Location);
        Assert.Equal(300 + toolbar, form.ClientSize.Height);
        Assert.Equal(900, form.ClientSize.Width);
    }

    [Fact]
    public void GoingToASnipTooLargeToFit_StopsAtTheEdgesOfTheScreen()
    {
        // Arrange
        Point corner = NearTheTopLeft();
        Rectangle area = Screen.PrimaryScreen!.WorkingArea;
        using SnipEditorForm form = WithTwoSnips(new Size(area.Width * 2, area.Height * 2), corner);

        // Act
        form.ShowSnip(1, 2);

        // Assert: the image scrolls, and the window takes all the room there is
        Assert.Equal(corner, form.Location);
        Assert.Equal(area.Right, form.Right);
        Assert.Equal(area.Bottom, form.Bottom);
    }

    [Fact]
    public void GoingToAnotherSnip_WhileMaximized_LeavesTheWindowAlone()
    {
        // Arrange
        using SnipEditorForm form = WithTwoSnips(new Size(800, 450), NearTheTopLeft());
        Size before = form.ClientSize;
        form.WindowState = FormWindowState.Maximized;

        // Act
        form.ShowSnip(1, 2);

        // Assert
        Assert.Equal(before, form.ClientSize);
    }

    [Fact]
    public void ShowingTheSnipAlreadyShown_LeavesTheWindowAsTheUserMadeIt()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(700, 300, Color.Red));
        form.ClientSize = new Size(1000, 600);

        // Act
        form.ShowSnip(0, 1);

        // Assert
        Assert.Equal(new Size(1000, 600), form.ClientSize);
    }

    [Fact]
    public void BlurOnASnip_TakesItsColorFromThatSnip()
    {
        // Arrange: the first snip is red, the second blue, and the blur is on the second
        using var form = new SnipEditorForm(Solid(100, 100, Color.Red));
        string path = SavedImagePath();
        form.AddSnip(Solid(100, 100, Color.Blue));
        form.ShowSnip(1, 2);
        form.ShowAnnotations([new BlurAnnotationModel(new Rectangle(20, 20, 40, 40))]);

        // Act
        form.SaveImage(path);

        // Assert
        using var saved = new Bitmap(path);
        Color pixel = saved.GetPixel(40, 40);
        Assert.True(pixel.B > pixel.R + 100, $"Not from the blue snip: {pixel}");
    }
}
