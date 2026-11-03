using System.Drawing;
using CincoVertice.Common.Ui.Win.Controls.Rtf;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Controls;

public class CodeTextControlTests
{
    private const string Code = "class A\n{\n    int x;\n\n    int y;\n}";

    private static TestControl Create(string text = Code)
    {
        var control = new TestControl { Size = new Size(400, 200), Text = text };

        control.CreateControl();
        _ = control.Handle;

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

    [Fact]
    public void Gutter_HasNumbersOverItsColor()
    {
        // Arrange
        using TestControl control = Create();
        using Bitmap image = control.DrawnOverlay();

        // Act
        IEnumerable<int> colors = Pixels(image, new Rectangle(0, 0, control.GutterWidth, Line(control, 1)))
            .Distinct();

        // Assert
        Assert.Contains(control.GutterBackColor.ToArgb(), colors);
        Assert.True(colors.Count() > 1);
    }

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
        public Bitmap DrawnOverlay()
        {
            var image = new Bitmap(ClientSize.Width, ClientSize.Height);

            using Graphics graphics = Graphics.FromImage(image);

            DrawOverlay(graphics);

            return image;
        }
    }
}
