using System.Drawing;
using System.Windows.Forms;
using CincoVertice.Common.Ui.Win.Controls.Rtf;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Controls;

public class CodeTextControlTests
{
    private const string Code = "class A\n{\n    int x;\n\n    int y;\n}";

    /// <summary>On a form shown off the screen, as the gutter is beside the text box on its parent.</summary>
    private static TestControl Create(string text = Code)
    {
        var host = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-5000, 100),
            ClientSize = new Size(440, 240),
        };
        var control = new TestControl { Location = new Point(20, 20), Size = new Size(400, 200), Text = text, Host = host };

        host.Controls.Add(control);
        host.Show();
        Application.DoEvents();

        return control;
    }

    [Theory]
    [InlineData("    int x;  ", null, null, 4, 6)]
    [InlineData("    ", null, null, 4, 0)]
    [InlineData("", null, null, 0, 0)]
    [InlineData("int x;", 5, 1, 4, 1)]
    [InlineData("int x;", 5, null, 4, 2)]
    [InlineData("int x;", 5, 99, 4, 2)]
    [InlineData("int x;", 99, 3, 6, 0)]
    [InlineData("int x;", -3, 2, 0, 2)]
    public void FindRange_StaysInsideTheLine(string line, int? column, int? length, int start, int expectedLength)
    {
        // Act
        (int Start, int Length) range = new CodeMark(1, "m", column, length).FindRange(line);

        // Assert
        Assert.Equal((start, expectedLength), range);
    }

    [Fact]
    public void Gutter_MovesTheTextOutOfItsWay()
    {
        // Arrange
        using TestControl control = Create();

        // Act
        Point first = control.GetPositionFromCharIndex(0);

        // Assert
        Assert.True(control.GutterWidth > 0);
        Assert.True(first.X >= control.GutterWidth);
    }

    [Fact]
    public void Gutter_GrowsWithTheNumberOfDigits()
    {
        // Arrange
        using TestControl control = Create("a");
        int small = control.GutterWidth;

        // Act
        control.Text = string.Join('\n', Enumerable.Range(0, 1200));

        // Assert: and the text is still out of its way
        Assert.True(control.GutterWidth > small);
        Assert.True(control.GetPositionFromCharIndex(0).X >= control.GutterWidth);
    }

    /// <summary>The window the numbers are in, wherever it is.</summary>
    private static Control GutterOf(CodeTextControl control) => Assert.Single(
        control.Controls.Cast<Control>().Concat(control.Parent!.Controls.Cast<Control>()),
        other => other != control);

    /// <summary>Where a control's inside is on the screen.</summary>
    private static Rectangle OnScreen(Control control) => control.RectangleToScreen(control.ClientRectangle);

    private static Bitmap DrawnGutter(CodeTextControl control)
    {
        Control gutter = GutterOf(control);
        var image = new Bitmap(gutter.Width, gutter.Height);

        gutter.DrawToBitmap(image, new Rectangle(Point.Empty, gutter.Size));

        return image;
    }

    [Fact]
    public void Gutter_IsAWindowOfItsOwnOverTheMargin()
    {
        // Arrange: scrolling moves what's painted on the text box itself, and repainting it then flickers; a window
        // of its own isn't moved, so only it is repainted, all at once
        using TestControl control = Create();

        // Act
        Control gutter = GutterOf(control);

        // Assert: over the left of the text box's inside, as tall
        Rectangle text = OnScreen(control);
        Assert.Equal(new Rectangle(text.Location, new Size(control.GutterWidth, text.Height)), OnScreen(gutter));
        Assert.False(gutter.TabStop);
        Assert.True(gutter.Visible);
    }

    [Fact]
    public void Gutter_IsOverTheMargin_WhenTheCodeIsSetBeforeTheFormIsShown()
    {
        // Arrange: as the config editor does; setting it creates the text box's window before the form lays it out
        using var host = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-5000, 100),
            ClientSize = new Size(440, 240),
        };
        var panel = new SplitContainer { Dock = DockStyle.Fill };
        var control = new TestControl { Dock = DockStyle.Fill };

        panel.Panel2.Controls.Add(control);
        host.Controls.Add(panel);
        control.SetCode(Code);

        // Act
        host.Show();
        Application.DoEvents();

        // Assert
        Rectangle text = OnScreen(control);
        Assert.Equal(new Rectangle(text.Location, new Size(control.GutterWidth, text.Height)), OnScreen(GutterOf(control)));
        Assert.True(GutterOf(control).Visible);
    }

    [Fact]
    public void Gutter_StaysAtTheTopLeftOfTheText_WhenItScrolls()
    {
        // Arrange: the text box moves the windows inside it when it scrolls, which dragged the numbers down with it
        using TestControl control = Create(string.Join('\n', Enumerable.Range(1, 200).Select(i => $"line {i}")));
        control.Select(control.GetFirstCharIndexFromLine(150), 0);
        control.ScrollToCaret();
        Application.DoEvents();

        // Act
        control.Select(0, 0);
        control.ScrollToCaret();
        Application.DoEvents();

        // Assert
        Assert.Equal(OnScreen(control).Location, OnScreen(GutterOf(control)).Location);
    }

    [Fact]
    public void Gutter_HidesWithTheText()
    {
        // Arrange
        using TestControl control = Create();

        // Act
        control.Visible = false;

        // Assert
        Assert.False(GutterOf(control).Visible);
    }

    [Fact]
    public void Gutter_FollowsTheWidthAndTheSizeOfTheControl()
    {
        // Arrange
        using TestControl control = Create("a");

        // Act
        control.Text = string.Join('\n', Enumerable.Range(0, 1200));
        control.Size = new Size(400, 300);

        // Assert
        Rectangle text = OnScreen(control);
        Assert.Equal(new Rectangle(text.Location, new Size(control.GutterWidth, text.Height)), OnScreen(GutterOf(control)));
    }

    [Fact]
    public void Gutter_HasNumbersOverItsColor()
    {
        // Arrange
        using TestControl control = Create();
        using Bitmap image = DrawnGutter(control);

        // Act
        IEnumerable<int> colors = Pixels(image, new Rectangle(0, 0, control.GutterWidth, Line(control, 1)))
            .Distinct();

        // Assert
        Assert.Contains(control.GutterBackColor.ToArgb(), colors);
        Assert.True(colors.Count() > 1);
    }

    [Fact]
    public void Gutter_ShowsTheNumbersOfTheLinesShown_AfterScrolling()
    {
        // Arrange: a line's number drawn where the line is
        using TestControl control = Create(string.Join('\n', Enumerable.Range(1, 200).Select(i => $"line {i}")));
        using Bitmap before = DrawnGutter(control);

        // Act
        control.Select(control.GetFirstCharIndexFromLine(150), 0);
        control.ScrollToCaret();
        using Bitmap after = DrawnGutter(control);

        // Assert: the first rows of the gutter differ, as the numbers are others
        Rectangle firstLine = new(0, 0, control.GutterWidth, control.Font.Height);
        Assert.NotEqual(Pixels(before, firstLine), Pixels(after, firstLine));
    }

    [Theory]
    [InlineData(0x020A)]
    [InlineData(0x0201)]
    [InlineData(0x0200)]
    public void Gutter_TheMouseOverIt_GoesToTheText(int message)
    {
        // Arrange: the wheel scrolls the text, a click selects lines, as over the margin before
        using TestControl control = Create(string.Join('\n', Enumerable.Range(1, 200).Select(i => $"line {i}")));
        Control gutter = GutterOf(control);

        // Act
        SendMessage(gutter.Handle, message, message == 0x020A ? -120 << 16 : 0, (5 << 16) | 5);

        // Assert
        Assert.Contains(message, control.Received);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);

    [Fact]
    public void Marks_ADrawnSquiggle_HasItsColorUnderTheLine()
    {
        // Arrange
        using TestControl control = Create();
        control.SetMarks([new CodeMark(3, "m", Color: Color.Lime)]);

        // Act
        using Bitmap image = control.DrawnOverlay();

        // Assert: in the third line, and nowhere above it (its edges are smoothed, so only mostly that color)
        int top = Line(control, 2);
        Assert.Contains(
            Pixels(image, new Rectangle(control.GutterWidth, top, 300, control.Font.Height + 1)),
            IsGreen);
        Assert.DoesNotContain(Pixels(image, new Rectangle(control.GutterWidth, 0, 300, top)), IsGreen);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(999)]
    public void Marks_BlankLastAndMissingLines_DontBreakPainting(int line)
    {
        // Arrange
        using TestControl control = Create();
        control.SetMarks([new CodeMark(line, "m")]);

        // Act & Assert: reaching the end is the point
        using Bitmap image = control.DrawnOverlay();
        Assert.NotNull(image);
    }

    [Fact]
    public void Marks_AnEmptyControl_DontBreakPainting()
    {
        // Arrange
        using TestControl control = Create(string.Empty);
        control.SetMarks([new CodeMark(1, "m")]);

        // Act & Assert
        using Bitmap image = control.DrawnOverlay();
        Assert.NotNull(image);
    }

    [Fact]
    public void Marks_DontChangeTheText()
    {
        // Arrange
        using TestControl control = Create();
        string rtf = control.Rtf!;

        // Act
        control.SetMarks([new CodeMark(3, "m")]);
        using Bitmap image = control.DrawnOverlay();

        // Assert
        Assert.Equal(rtf, control.Rtf);
    }

    [Fact]
    public void MessageAt_FindsTheMarkUnderThePoint()
    {
        // Arrange
        using TestControl control = Create();
        control.SetMarks([new CodeMark(3, "first"), new CodeMark(3, "second", 5, 3), new CodeMark(1, "other")]);

        int lineStart = control.GetFirstCharIndexFromLine(2);
        Point onInt = control.GetPositionFromCharIndex(lineStart + 5);
        Point onX = control.GetPositionFromCharIndex(lineStart + 8);

        // Act & Assert
        Assert.Equal($"first{Environment.NewLine}second", control.MessageAt(onInt));
        Assert.Equal("first", control.MessageAt(onX));
        Assert.Null(control.MessageAt(new Point(1, onInt.Y)));
        Assert.Null(control.MessageAt(control.GetPositionFromCharIndex(control.GetFirstCharIndexFromLine(1))));
    }

    private static bool IsGreen(int argb) => Color.FromArgb(argb) is { A: > 0, G: > 0, R: 0, B: 0 };

    private static int Line(CodeTextControl control, int line) =>
        control.GetPositionFromCharIndex(control.GetFirstCharIndexFromLine(line)).Y;

    private static IEnumerable<int> Pixels(Bitmap image, Rectangle area)
    {
        for (int y = area.Top; y < area.Bottom; y++)
        {
            for (int x = area.Left; x < area.Right; x++)
            {
                yield return image.GetPixel(x, y).ToArgb();
            }
        }
    }

    private sealed class TestControl : CodeTextControl
    {
        /// <summary>The window messages the text box got.</summary>
        public List<int> Received { get; } = [];

        /// <summary>The form it's shown on, closed with it.</summary>
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Form? Host { get; set; }

        protected override void Dispose(bool disposing)
        {
            Form? host = Host;

            Host = null;
            base.Dispose(disposing);
            host?.Dispose();
        }

        protected override void WndProc(ref Message m)
        {
            Received.Add(m.Msg);
            base.WndProc(ref m);
        }

        public Bitmap DrawnOverlay()
        {
            var image = new Bitmap(ClientSize.Width, ClientSize.Height);

            using Graphics graphics = Graphics.FromImage(image);

            DrawOverlay(graphics);

            return image;
        }
    }
}
