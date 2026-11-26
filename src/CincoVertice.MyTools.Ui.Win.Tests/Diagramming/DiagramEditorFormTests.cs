using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.ColorPicker.Interfaces;
using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Presenters;
using CincoVertice.MyTools.Ui.Diagramming.Views;
using CincoVertice.MyTools.Ui.Win.Diagramming;
using CincoVertice.MyTools.Ui.Win.Diagramming.Actions;
using CincoVertice.MyTools.Ui.Win.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Diagramming;

public class DiagramEditorFormTests
{
    private static readonly DiagramStyleModel _style = new(
        Color.FromArgb(255, 255, 0, 0),
        Color.FromArgb(255, 0, 0, 255),
        4,
        false,
        Color.Black,
        14);

    private static readonly DiagramViewportModel _viewport = new(1, Point.Empty);

    private static DiagramSceneModel Scene(DiagramModel diagram, IReadOnlySet<int>? selected = null) => new(
        diagram,
        selected ?? new HashSet<int>(),
        new HashSet<int>(),
        _viewport,
        null,
        [],
        null,
        null,
        null);

    private static Bitmap Render(DiagramSceneModel scene)
    {
        var image = new Bitmap(300, 200);

        using var graphics = Graphics.FromImage(image);

        DiagramRenderer.Draw(graphics, scene, image.Size);

        return image;
    }

    private static int CountOf(Bitmap image, Color color)
    {
        int count = 0;

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                if (image.GetPixel(x, y).ToArgb() == color.ToArgb())
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static DiagramModel TextDiagram(DiagramStyleModel style) => new(
        [new DiagramShapeModel(1, DiagramShapeKind.Text, new Rectangle(20, 20, 260, 120), "Hello\n\nworld", style)],
        []);

    [Theory]
    [InlineData("Roboto")]
    [InlineData("Segoe UI")]
    public void TextOutline_DrawsItsColorAroundTheLetters(string fontFamily)
    {
        // Arrange
        Color edge = Color.FromArgb(255, 0, 200, 0);
        DiagramStyleModel style = _style with { FontSize = 36, FontFamily = fontFamily, TextOutlineColor = edge };

        // Act
        using Bitmap without = Render(Scene(TextDiagram(style)));
        using Bitmap with = Render(Scene(TextDiagram(style with { TextOutlineWidth = 3 })));

        // Assert: the letters are still there, on top of it
        Assert.Equal(0, CountOf(without, edge));
        Assert.True(CountOf(with, edge) > 100);
        Assert.True(CountOf(with, Color.Black) > 100);
    }

    [Fact]
    public void Renderer_ABoldPart_HasMoreInk_ThanTheSameTextPlain()
    {
        // Arrange
        DiagramStyleModel style = _style with { FontSize = 30, FontFamily = "Roboto" };
        DiagramModel Diagram(IReadOnlyList<DiagramTextSpanModel>? spans) => new(
            [
                new DiagramShapeModel(
                    1,
                    DiagramShapeKind.Text,
                    new Rectangle(10, 20, 280, 160),
                    "Hello world",
                    style,
                    TextSpans: spans),
            ],
            []);

        // Act
        using Bitmap plain = Render(Scene(Diagram(null)));
        using Bitmap bold = Render(Scene(Diagram([new DiagramTextSpanModel(0, 5, DiagramTextFormat.Bold)])));
        using Bitmap italic = Render(Scene(Diagram([new DiagramTextSpanModel(0, 11, DiagramTextFormat.Italic)])));

        // Assert: the letters are black, anti-aliased at their edges
        int Ink(Bitmap image) => CountOf(image, Color.Black);
        Assert.True(Ink(bold) > Ink(plain) * 1.1, $"bold {Ink(bold)}, plain {Ink(plain)}");
        Assert.True(Ink(italic) > 0);
        Assert.NotEqual(Ink(plain), Ink(italic));
    }

    [Fact]
    public void TextBox_OpenedAgainWithTheSameFont_OrAnother_StillWorks()
    {
        // Arrange: the box keeps a font equal to its own, so freeing the old one would leave it a freed font
        using var form = new DiagramEditorForm
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 100),
            ShowInTaskbar = false,
        };
        IDiagramEditorView view = form;
        var bold = new DiagramTextSpanModel(0, 3, DiagramTextFormat.Bold);
        form.Show();

        // Act: a label, then another in the same font, then one in another
        var box = new Rectangle(10, 10, 200, 60);
        view.BeginTextEdit(box, new DiagramRichTextModel("yes", [bold]), 14, "Roboto", Color.Black);
        view.BeginTextEdit(box, new DiagramRichTextModel("now", [bold]), 14, "Roboto", Color.Black);
        DiagramRichTextModel second = form.TypedText();

        view.BeginTextEdit(box, DiagramRichTextModel.Plain("big"), 20, "Roboto", Color.Black);

        // Assert
        Assert.Equal(new DiagramRichTextModel("now", [bold]).Spans, second.Spans);
        Assert.Equal("big", form.TypedText().Text);
        Assert.Empty(form.TypedText().Spans);
    }

    [Fact]
    public void SwapFont_AnEqualFont_KeepsTheOneTheBoxHas_SoItsNeverLeftAFreedOne()
    {
        // Arrange: a box as the editor's, which keeps its own font when given an equal one
        using var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 100) };
        using var box = new RichTextBox();
        form.Controls.Add(box);
        form.Show();
        Font first = DiagramEditorForm.SwapFont(box, null, DiagramFonts.Create("Roboto", 14));

        // Act: the same again, then another size
        Font same = DiagramEditorForm.SwapFont(box, first, DiagramFonts.Create("Roboto", 14));
        box.SelectAll();
        box.SelectionFont = box.Font;
        Font larger = DiagramEditorForm.SwapFont(box, same, DiagramFonts.Create("Roboto", 20));

        // Assert: what the box has is what it was given, never freed
        Assert.Same(first, same);
        Assert.Same(larger, box.Font);
        box.SelectionFont = box.Font;
        larger.Dispose();
    }

    [Fact]
    public void TextBox_ShowsTheBoldParts_AndGivesThemBack_AndCtrlBTogglesWhatsSelected()

    {
        // Arrange
        using var form = new DiagramEditorForm
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 100),
            ShowInTaskbar = false,
        };
        IDiagramEditorView view = form;
        var bold = new DiagramTextSpanModel(0, 4, DiagramTextFormat.Bold);
        var italic = new DiagramTextSpanModel(5, 3, DiagramTextFormat.Italic);
        form.Show();

        // Act
        view.BeginTextEdit(
            new Rectangle(10, 10, 200, 60),
            new DiagramRichTextModel("Step\none", [bold, italic]),
            16,
            "Roboto",
            Color.Black);

        // Assert
        Assert.Equal(new DiagramRichTextModel("Step\none", [bold, italic]).Text, form.TypedText().Text);
        Assert.Equal([bold, italic], form.TypedText().Spans);

        // Act: "ep" and "o" selected and made bold, then italic all over, then italic off again
        var box = (RichTextBox)typeof(DiagramEditorForm)
            .GetField("TextInputBox", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;
        box.Select(2, 4);
        form.ToggleSelectedFormat(DiagramTextFormat.Bold);

        // Assert: "Step\no" bold; "o" keeps its italic
        const DiagramTextFormat boldItalic = DiagramTextFormat.Bold | DiagramTextFormat.Italic;
        Assert.Equal(
            [
                new DiagramTextSpanModel(0, 5, DiagramTextFormat.Bold),
                new(5, 1, boldItalic),
                new(6, 2, DiagramTextFormat.Italic),
            ],
            form.TypedText().Spans);

        // Act / Assert: all of "Step" was bold, so Ctrl+B again takes it off
        box.Select(0, 4);
        form.ToggleSelectedFormat(DiagramTextFormat.Bold);
        Assert.Equal(
            [
                new DiagramTextSpanModel(4, 1, DiagramTextFormat.Bold),
                new(5, 1, boldItalic),
                new(6, 2, DiagramTextFormat.Italic),
            ],
            form.TypedText().Spans);
    }

    [Fact]
    public void TextOutline_LeavesTheLettersSolid()
    {
        // Arrange: Roboto's letters are made of pieces that overlap, like the bar and the stems of an H
        Color edge = Color.FromArgb(255, 0, 200, 0);
        DiagramStyleModel style = _style with { FontSize = 200, FontFamily = "Roboto", TextOutlineColor = edge };
        DiagramModel Diagram(DiagramStyleModel withStyle) => new(
            [new DiagramShapeModel(1, DiagramShapeKind.Text, new Rectangle(20, 20, 260, 160), "H", withStyle)],
            []);

        // Act
        using Bitmap plain = Render(Scene(Diagram(style)));
        using Bitmap outlined = Render(Scene(Diagram(style with { TextOutlineWidth = 11 })));

        // Assert: well inside a letter, where small differences in how it's drawn don't count, there's no edge
        const int inset = 1;
        int inLetters = 0;
        int edgeInLetters = 0;

        for (int x = inset; x < plain.Width - inset; x++)
        {
            for (int y = inset; y < plain.Height - inset; y++)
            {
                bool isInside = true;

                for (int i = -inset; i <= inset; i++)
                {
                    for (int j = -inset; j <= inset; j++)
                    {
                        isInside &= plain.GetPixel(x + i, y + j).ToArgb() == Color.Black.ToArgb();
                    }
                }

                if (isInside)
                {
                    inLetters++;
                    edgeInLetters += outlined.GetPixel(x, y).ToArgb() == edge.ToArgb() ? 1 : 0;
                }
            }
        }

        Assert.True(inLetters > 100);
        Assert.Equal(0, edgeInLetters);
    }

    [Fact]
    public void TextOutline_FollowsTheZoom()
    {
        // Arrange
        Color edge = Color.FromArgb(255, 0, 200, 0);
        DiagramStyleModel style = _style with { FontSize = 18, TextOutlineWidth = 2, TextOutlineColor = edge };

        // Small and near the corner, so it's still inside the image when zoomed
        DiagramSceneModel scene = Scene(new DiagramModel(
            [new DiagramShapeModel(1, DiagramShapeKind.Text, new Rectangle(5, 5, 60, 30), "Hello", style)],
            []));

        // Act
        using Bitmap normal = Render(scene);
        using Bitmap zoomed = Render(scene with { Viewport = new DiagramViewportModel(2, Point.Empty) });

        // Assert
        Assert.True(CountOf(zoomed, edge) > CountOf(normal, edge) * 2);
    }

    [Fact]
    public void Form_ShowsATextOutline_AndPassesOnItsWidth()
    {
        // Arrange
        using var form = new DiagramEditorForm();
        var presenter =
            NewPresenter(form);
        var list = (ToolStripComboBox)typeof(DiagramEditorForm)
            .GetField("TextOutlineWidthComboBox", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;
        int? selected = null;

        presenter.Start();
        form.TextOutlineWidthSelected += (_, width) => selected = width;

        // Assert: starts with no edge, and showing that wasn't taken as the user's choice
        Assert.Equal("No edge", list.SelectedItem);
        Assert.Null(selected);

        // Act
        list.SelectedItem = "3 px";

        // Assert
        Assert.Equal(3, selected);

        // Act
        form.ShowStyle(_style, DiagramRouting.Elbow, DiagramArrowHeads.End);

        // Assert: the list follows the style shown
        Assert.Equal("No edge", list.SelectedItem);
    }

    [Fact]
    public void Form_StartsWithItsPresenter()
    {
        // Every tool button's tag, the palettes and the lists are read here
        using var form = new DiagramEditorForm();
        var presenter =
            NewPresenter(form);

        presenter.Start();

        Assert.Contains(DiagramEditorPresenter.UntitledName, form.Text);
    }

    [Theory]
    [InlineData(DiagramTool.Select)]
    [InlineData(DiagramTool.Connector)]
    [InlineData(DiagramTool.Rectangle)]
    [InlineData(DiagramTool.RoundedRectangle)]
    [InlineData(DiagramTool.Ellipse)]
    [InlineData(DiagramTool.Diamond)]
    [InlineData(DiagramTool.Parallelogram)]
    [InlineData(DiagramTool.Hexagon)]
    [InlineData(DiagramTool.Cylinder)]
    [InlineData(DiagramTool.Document)]
    [InlineData(DiagramTool.Text)]
    public void ToolIcon_HasSomethingOnIt(DiagramTool tool)
    {
        // Act
        using Bitmap icon = DiagramToolIcons.Create(tool, 32);

        // Assert
        bool hasInk = Enumerable.Range(0, 32)
            .SelectMany(x => Enumerable.Range(0, 32).Select(y => icon.GetPixel(x, y).A))
            .Any(alpha => alpha > 0);

        Assert.True(hasInk);
    }

    [Theory]
    [InlineData(DiagramShapeKind.Rectangle)]
    [InlineData(DiagramShapeKind.RoundedRectangle)]
    [InlineData(DiagramShapeKind.Ellipse)]
    [InlineData(DiagramShapeKind.Diamond)]
    [InlineData(DiagramShapeKind.Parallelogram)]
    [InlineData(DiagramShapeKind.Hexagon)]
    [InlineData(DiagramShapeKind.Cylinder)]
    [InlineData(DiagramShapeKind.Document)]
    public void Renderer_FillsTheShape(DiagramShapeKind kind)
    {
        // Arrange
        var shape = new DiagramShapeModel(1, kind, new Rectangle(50, 50, 200, 100), string.Empty, _style);

        // Act
        using Bitmap image = Render(Scene(new DiagramModel([shape], [])));

        // Assert: its middle has the fill, and outside it nothing
        Assert.Equal(_style.Fill.ToArgb(), image.GetPixel(150, 100).ToArgb());
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void Renderer_AShapeWithNoFill_ShowsWhatsBehindIt()
    {
        // Arrange
        var shape = new DiagramShapeModel(
            1,
            DiagramShapeKind.Rectangle,
            new Rectangle(50, 50, 200, 100),
            string.Empty,
            _style with { Fill = Color.Empty });

        // Act
        using Bitmap image = Render(Scene(new DiagramModel([shape], [])));

        // Assert
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(155, 105).ToArgb());
        Assert.Equal(_style.Line.ToArgb(), image.GetPixel(50, 100).ToArgb());
    }

    [Theory]
    [InlineData(DiagramRouting.Straight, DiagramArrowHeads.Both, false)]
    [InlineData(DiagramRouting.Elbow, DiagramArrowHeads.End, true)]
    [InlineData(DiagramRouting.Elbow, DiagramArrowHeads.None, false)]
    public void Renderer_DrawsConnectors(DiagramRouting routing, DiagramArrowHeads heads, bool dashed)
    {
        // Arrange
        DiagramStyleModel style = _style with { Dashed = dashed };
        DiagramModel diagram = new(
            [
                new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(10, 10, 60, 40), "a", style),
                new DiagramShapeModel(2, DiagramShapeKind.Ellipse, new Rectangle(200, 120, 60, 40), "b", style),
            ],
            [new DiagramConnectorModel(3, 1, 2, routing, heads, "label", style)]);

        // Act
        using Bitmap image = Render(Scene(diagram));

        // Assert: something other than the shapes and the grid was drawn between them, in the line's color
        bool hasLine = Enumerable.Range(75, 120)
            .SelectMany(x => Enumerable.Range(0, 200).Select(y => image.GetPixel(x, y).ToArgb()))
            .Contains(style.Line.ToArgb());

        Assert.True(hasLine);
    }

    [Fact]
    public void Renderer_AConnectorWithoutItsShape_IsSkipped()
    {
        // Arrange
        DiagramModel diagram = new(
            [],
            [new DiagramConnectorModel(3, 1, 2, DiagramRouting.Elbow, DiagramArrowHeads.End, string.Empty, _style)]);

        // Act
        using Bitmap image = Render(Scene(diagram));

        // Assert
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void Renderer_ShowsTheSelectionHandles()
    {
        // Arrange
        var shape = new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(50, 50, 200, 100), "x", _style);
        DiagramSceneModel scene = Scene(new DiagramModel([shape], []), new HashSet<int> { 1 }) with
        {
            HandleBounds = shape.Bounds,
        };

        // Act
        using Bitmap image = Render(scene);

        // Assert: white in the middle of the square at the corner, where the outline would otherwise be blue
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(50, 50).ToArgb());
    }

    [Fact]
    public void Renderer_DrawsTheCircles_WithTheHighlightedOneFilled()
    {
        // Arrange
        var shape = new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(50, 50, 200, 100), "x", _style);
        DiagramSceneModel scene = Scene(new DiagramModel([shape], [])) with
        {
            Anchors = new DiagramAnchorsModel([new PointF(150, 36), new PointF(264, 100)], new PointF(264, 100)),
        };

        // Act
        using Bitmap image = Render(scene);

        // Assert: white inside the plain one, and not inside the highlighted one
        Assert.Equal(Color.White.ToArgb(), image.GetPixel(150, 36).ToArgb());
        Assert.NotEqual(Color.White.ToArgb(), image.GetPixel(264, 100).ToArgb());
    }

    [Fact]
    public void Renderer_DrawsTheEndsOfASelectedConnector()
    {
        // Arrange
        DiagramSceneModel scene = Scene(DiagramModel.Empty) with { ConnectorEnds = [new PointF(100, 100)] };

        // Act
        using Bitmap image = Render(scene);

        // Assert
        Assert.Equal(Color.FromArgb(0, 120, 215).ToArgb(), image.GetPixel(100, 100).ToArgb());
    }

    [Fact]
    public void Form_ShowsAnyProperties_EvenOutOfItsFieldsRange()
    {
        // Arrange
        using var form = new DiagramEditorForm();
        NewPresenter(form)
            .Start();

        // Act: from a file edited by hand, say; a field's value out of its range would throw
        form.ShowProperties(new DiagramPropertiesModel(
            "Rectangle",
            new Rectangle(-999999, 999999, 1, 999999),
            5000,
            Color.Empty,
            Color.Red,
            5000,
            true,
            Color.Blue,
            1,
            "text",
            DiagramRouting.Straight,
            DiagramArrowHeads.None));
        form.ShowProperties(new DiagramPropertiesModel("Nothing selected"));

        // Assert: reaching here is the point
        Assert.NotNull(form);
    }

    [Theory]
    [InlineData(900, 600, false)]
    [InlineData(800, 500, true)]
    public void Form_TakesASavedSize(int width, int height, bool isMaximized)
    {
        // Arrange
        using var form = new DiagramEditorForm();

        // Act
        form.ShowWindowSize(new DiagramWindowSizeModel(new Size(width, height), isMaximized));

        // Assert
        Assert.Equal(isMaximized, form.WindowState == FormWindowState.Maximized);
    }

    [Fact]
    public void Form_ASavedSizeLargerThanTheScreen_FitsTheScreen()
    {
        // Arrange
        using var form = new DiagramEditorForm();
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;

        // Act
        form.ShowWindowSize(new DiagramWindowSizeModel(new Size(99999, 99999), false));

        // Assert
        Assert.Equal(area.Size, form.Size);
    }

    private static DiagramEditorForm ShowFormEditingProperties(List<string> events, string fieldName, string typed)
    {
        var form = new DiagramEditorForm
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 100),
            ShowInTaskbar = false,
        };
        IDiagramEditorView view = form;

        view.TextEdited += (_, text) => events.Add("text " + text);
        view.NameEdited += (_, name) => events.Add("name " + name);
        view.BoundsEdited += (_, bounds) => events.Add("width " + bounds.Width);
        view.CommandRequested += (_, command) => events.Add(command.ToString());
        view.Closing += (_, _) => events.Add("closing");

        form.Show();
        form.ShowPropertiesPanel(true);
        form.ShowProperties(new DiagramPropertiesModel(
            "Rectangle",
            new Rectangle(0, 0, 100, 50),
            0,
            Color.White,
            Color.Black,
            1,
            false,
            Color.Black,
            14,
            "old",
            DiagramRouting.Elbow,
            DiagramArrowHeads.End,
            Name: "Entry"));

        Control field = form.Controls.Find(fieldName, true).Single();

        field.Focus();
        Assert.True(field.ContainsFocus);

        // As typing does: a number box's own Text would apply the value at once
        (field.Controls.OfType<TextBox>().FirstOrDefault() ?? field).Text = typed;
        events.Clear();

        return form;
    }

    private static void ClickToolbarButton(DiagramEditorForm form, string name)
    {
        var button = (ToolStripItem)typeof(DiagramEditorForm)
            .GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(form)!;

        button.PerformClick();
    }

    [Theory]
    [InlineData("SaveMenuItem", "Save")]
    [InlineData("UndoMenuItem", "Undo")]
    public void Toolbar_TakesTheTextBeingTyped_BeforeItsCommand(string button, string command)
    {
        // Arrange
        List<string> events = [];
        using DiagramEditorForm form = ShowFormEditingProperties(events, "TextPropertyBox", "new");

        // Act
        ClickToolbarButton(form, button);

        // Assert: and only once: a late one would save the old text, or undo the undo
        Assert.Equal(["text new", command], events);
    }

    [Fact]
    public void ZoomLabel_ClickingIt_GoesBackTo100()
    {
        // Arrange
        List<string> events = [];
        using DiagramEditorForm form = ShowFormEditingProperties(events, "TextPropertyBox", "new");

        // Act
        ClickToolbarButton(form, "ZoomLabel");

        // Assert
        Assert.Equal(["text new", "ZoomReset"], events);
    }

    [Fact]
    public void Toolbar_TakesTheNumberBeingTyped_BeforeItsCommand()
    {
        // Arrange
        List<string> events = [];
        using DiagramEditorForm form = ShowFormEditingProperties(events, "WidthBox", "250");

        // Act
        ClickToolbarButton(form, "SaveMenuItem");

        // Assert
        Assert.Equal(["width 250", "Save"], events);
    }

    [Fact]
    public void Properties_TheName_IsShown_AndWhatsTypedIsTakenOnEnter_OrBeforeACommand()
    {
        // Arrange
        List<string> events = [];
        using DiagramEditorForm form = ShowFormEditingProperties(events, "NamePropertyBox", "Exit");
        var box = (TextBox)form.Controls.Find("NamePropertyBox", true).Single();

        // Act: Enter, then another name before a command
        box.Text = "Way out";
        typeof(DiagramEditorForm)
            .GetMethod("NamePropertyBox_KeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, [box, new KeyEventArgs(Keys.Enter)]);
        box.Text = "Exit";
        ClickToolbarButton(form, "SaveMenuItem");

        // Assert
        Assert.Equal(["name Way out", "name Exit", "Save"], events);

        // Act / Assert: several selected, no name to show
        form.ShowProperties(new DiagramPropertiesModel("2 shapes"));
        Assert.False(box.Visible);
    }

    [Fact]
    public void FileExit_ClosesLikeTheWindowsX_TakingTheTextBeingTyped()

    {
        // Arrange
        List<string> events = [];
        using DiagramEditorForm form = ShowFormEditingProperties(events, "TextPropertyBox", "new");

        // Act
        ClickToolbarButton(form, "ExitMenuItem");

        // Assert
        Assert.Equal(["text new", "closing"], events);
    }

    [Theory]
    [InlineData("NewMenuItem", "New")]
    [InlineData("OpenMenuItem", "Open")]
    [InlineData("SaveAsMenuItem", "SaveAs")]
    [InlineData("CopyStyleMenuItem", "CopyStyle")]
    [InlineData("PasteStyleMenuItem", "PasteStyle")]
    [InlineData("RedoMenuItem", "Redo")]
    [InlineData("GroupMenuItem", "Group")]
    [InlineData("UngroupMenuItem", "Ungroup")]
    [InlineData("CopyStyleContextMenuItem", "CopyStyle")]
    [InlineData("PasteStyleContextMenuItem", "PasteStyle")]
    [InlineData("GroupContextMenuItem", "Group")]
    [InlineData("UngroupContextMenuItem", "Ungroup")]
    [InlineData("CopyPictureMenuItem", "CopyPicture")]
    [InlineData("CopyPictureContextMenuItem", "CopyPicture")]
    public void Toolbar_ItemsAskForTheirCommand(string item, string command)
    {
        // Arrange
        List<string> events = [];
        using DiagramEditorForm form = ShowFormEditingProperties(events, "TextPropertyBox", "new");

        // Act
        ClickToolbarButton(form, item);

        // Assert
        Assert.Equal(["text new", command], events);
    }

    [Fact]
    public void PictureData_IsABitmap_AndAPng_OfTheDiagram()
    {
        // Arrange
        var diagram = new DiagramModel(
            [new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(0, 0, 100, 50), "A", _style)],
            []);
        using Bitmap picture = PngDiagramExporter.Draw(diagram, 2);
        using var png = new MemoryStream();

        // Act
        DataObject data = DiagramEditorForm.PictureData(picture, png);

        // Assert: the PNG is the same picture, which a program can read
        Assert.Same(picture, data.GetImage());
        png.Position = 0;
        Assert.True(data.TryGetData(DiagramEditorForm.PngClipboardFormat, out MemoryStream? stream));
        using var read = new Bitmap(stream!);
        Assert.Equal(picture.Size, read.Size);
        Assert.Equal(_style.Fill.ToArgb(), read.GetPixel(read.Width / 2, read.Height / 2).ToArgb());
    }

    [Fact]
    public void SidePanel_IsTabbed_ThePanelsButtonShowsOrHidesIt_AndSaysWhatShowsAndWhatStops()
    {
        // Arrange
        List<string> events = [];
        using DiagramEditorForm form = ShowFormEditingProperties([], "TextPropertyBox", "new");
        IDiagramEditorView view = form;
        T Field<T>(string name) => (T)typeof(DiagramEditorForm)
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;
        var tabs = Field<TabControl>("SidePanelTabs");
        view.PropertiesPanelToggled += (_, shown) => events.Add($"properties {shown}");
        view.ImagesPanelToggled += (_, shown) => events.Add($"images {shown}");
        view.LayersPanelToggled += (_, shown) => events.Add($"layers {shown}");

        // Act: as the presenter shows it, nothing is raised
        view.ShowPropertiesPanel(true);
        view.ShowImagesPanel(false);

        // Assert: Images, Layers, Properties, at the canvas' right
        Assert.Equal(["Images", "Layers", "Properties"], tabs.TabPages.Cast<TabPage>().Select(tab => tab.Text));
        Assert.Equal(DockStyle.Right, tabs.Dock);
        Assert.True(tabs.Visible);
        Assert.Equal("Properties", tabs.SelectedTab!.Text);
        Assert.True(Field<ToolStripButton>("PanelsButton").Checked);
        Assert.Null(form.Controls.Find("LayersButton", true).FirstOrDefault());
        Assert.Empty(events);

        // Act: a click on the Layers tab, then the Panels button hides the panel, then shows it again
        tabs.SelectedIndex = 1;
        Field<ToolStripButton>("PanelsButton").PerformClick();
        bool isHidden = !tabs.Visible && !Field<ToolStripButton>("PanelsButton").Checked;
        Field<ToolStripButton>("PanelsButton").PerformClick();

        // Assert
        Assert.Equal(["properties False", "layers True", "layers False", "layers True"], events);
        Assert.True(isHidden);
        Assert.True(tabs.Visible);
        Assert.Equal("Layers", tabs.SelectedTab!.Text);
    }

    [Fact]
    public void LayersPanel_AClickOnARowsEyeOrLock_SaysWhichToHideOrLock()
    {
        // Arrange
        List<string> events = [];
        using DiagramEditorForm form = ShowFormEditingProperties([], "TextPropertyBox", "new");
        IDiagramEditorView view = form;
        var list = (ListView)typeof(DiagramEditorForm)
            .GetField("LayersListView", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;
        view.LayerHiddenToggled += (_, id) => events.Add($"hide {id}");
        view.LayerLockedToggled += (_, id) => events.Add($"lock {id}");
        view.ShowLayersPanel(true);
        view.ShowLayers([new DiagramLayerModel(7, "Rectangle", string.Empty, false, IsLocked: true)]);
        Rectangle row = new(list.Items[0].Bounds.Location, list.TileSize);
        void ClickAt(int x) => typeof(DiagramEditorForm)
            .GetMethod("LayersListView_MouseUp", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, [list, new MouseEventArgs(MouseButtons.Left, 1, x, row.Top + row.Height / 2, 0)]);

        // Act: the eye is the last square of the row, the lock the one before; the text is neither
        ClickAt(row.Right - row.Height / 2);
        ClickAt(row.Right - row.Height * 3 / 2);
        ClickAt(row.Left + 5);

        // Assert
        Assert.Equal(["hide 7", "lock 7"], events);
    }

    [Fact]
    public void FileMenu_Recent_ListsWhatItsGiven_NumberedForTheKeyboard_AndChoosingOneRaisesIt()
    {
        // Arrange
        List<DiagramRecentModel> chosen = [];
        using DiagramEditorForm form = ShowFormEditingProperties([], "TextPropertyBox", "new");
        IDiagramEditorView view = form;
        var recent = (ToolStripMenuItem)typeof(DiagramEditorForm)
            .GetField("RecentMenuItem", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;
        view.RecentChosen += (_, item) => chosen.Add(item);
        var file = new DiagramRecentModel(@"R&D.svg  —  C:\Work", @"C:\Work\R&D.svg", null);

        // Act: none, then two
        view.ShowRecent([]);
        bool isEmptyDisabled = !recent.DropDownItems[0].Enabled;
        view.ShowRecent([new DiagramRecentModel("Unsaved: Untitled, 15/11/2026 9:00", null, "old"), file]);
        recent.DropDownItems[1].PerformClick();

        // Assert
        Assert.True(isEmptyDisabled);
        Assert.Equal(
            ["&1 Unsaved: Untitled, 15/11/2026 9:00", @"&2 R&&D.svg  —  C:\Work"],
            recent.DropDownItems.Cast<ToolStripItem>().Select(item => item.Text));
        Assert.Equal([file], chosen);
    }

    [Fact]
    public void ViewMenu_AsksForTheTopLeft_OrTheMiddle_OfTheCanvasAsLargeAsItIs_TakingTheTextFirst()
    {
        // Arrange
        List<string> events = [];
        using DiagramEditorForm form = ShowFormEditingProperties(events, "TextPropertyBox", "new");
        IDiagramEditorView view = form;
        Control canvas = form.Controls.Find("Canvas", true).Single();
        view.BringIntoViewRequested += (_, request) => events.Add($"{request.Placement} {request.CanvasSize}");

        // Act
        ClickToolbarButton(form, "TopLeftMenuItem");
        ClickToolbarButton(form, "CenterMenuItem");
        ClickToolbarButton(form, "ZoomResetMenuItem");

        // Assert
        Assert.Equal(
            ["text new", $"TopLeft {canvas.ClientSize}", $"Center {canvas.ClientSize}", "ZoomReset"],
            events);
    }

    [Fact]
    public void ImagesPanel_LinksToTheOfficialIcons_OfAzureAndAws()
    {
        // Arrange
        using var form = new DiagramEditorForm();
        var link = (LinkLabel)form.Controls.Find("OfficialIconsLinkLabel", true).Single();

        // Act / Assert: each name is a link to its maker's page
        Assert.Equal(
            ["https://learn.microsoft.com/azure/architecture/icons/", "https://aws.amazon.com/architecture/icons/"],
            link.Links.Cast<LinkLabel.Link>().Select(each => (string)each.LinkData!));
        Assert.Equal(
            ["Azure", "AWS"],
            link.Links.Cast<LinkLabel.Link>().Select(each => link.Text.Substring(each.Start, each.Length)));
    }

    [Fact]
    public void LayersPanel_ShowsTheRowsAndSelection_WithoutTakingThemForTheUsers_AndTheUsersAreRaised()
    {
        // Arrange
        List<IReadOnlyList<int>> selected = [];
        using DiagramEditorForm form = ShowFormEditingProperties([], "TextPropertyBox", "new");
        IDiagramEditorView view = form;
        var list = (ListView)typeof(DiagramEditorForm)
            .GetField("LayersListView", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;
        view.LayersSelected += (_, ids) => selected.Add(ids);

        // Act
        view.ShowLayersPanel(true);
        view.ShowLayers(
        [
            new DiagramLayerModel(3, "Connector: Start → End", string.Empty, true),
            new DiagramLayerModel(2, "Rectangle: End", string.Empty, false),
            new DiagramLayerModel(1, "Entry", "Entry", false, GroupId: 5),
        ]);
        view.ShowLayerSelection(new HashSet<int> { 2 });

        // Assert
        Assert.Equal(
            ["Connector: Start → End", "Rectangle: End", "Entry"],
            list.Items.Cast<ListViewItem>().Select(item => item.Text));
        Assert.Equal([1], list.SelectedIndices.Cast<int>());
        Assert.Empty(selected);
        Assert.Contains("group", list.Items[2].ToolTipText);

        // Act: the user selects the last row too
        list.Items[2].Selected = true;

        // Assert
        Assert.Equal([2, 1], selected[^1]);
    }

    [Fact]
    public void StatusBar_ShowsWhatItsGiven_AndTheFindBoxAsksToFind()
    {
        // Arrange
        List<DiagramFindModel> finds = [];
        List<string> events = [];
        using DiagramEditorForm form = ShowFormEditingProperties(events, "TextPropertyBox", "new");
        IDiagramEditorView view = form;
        var findBox = (ToolStripTextBox)typeof(DiagramEditorForm)
            .GetField("FindBox", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;
        view.FindRequested += (_, find) => finds.Add(find);

        // Act
        view.ShowStatus("12, 34", "1 shape: 100 × 50 at 0, 0");
        view.ShowFindResult("1 of 2");
        findBox.Text = "web";

        // Assert
        StatusStrip status = form.Controls.OfType<StatusStrip>().Single();
        Assert.Equal(
            ["12, 34", "1 shape: 100 × 50 at 0, 0", "1 of 2"],
            status.Items.OfType<ToolStripStatusLabel>().Select(label => label.Text));
        DiagramFindModel find = Assert.Single(finds);
        Assert.Equal(("web", DiagramFindStep.First), (find.Text, find.Step));
        Assert.False(find.CanvasSize.IsEmpty);
    }

    [Fact]
    public void Closing_TakesTheTextBeingTyped_BeforeAskingAboutChanges()
    {
        // Arrange
        List<string> events = [];
        using DiagramEditorForm form = ShowFormEditingProperties(events, "TextPropertyBox", "new");

        // Act
        form.Close();

        // Assert
        Assert.Equal(["text new", "closing"], events);
    }

    [Fact]
    public void Properties_ALockedField_CantBeTypedIn_AndItsLockFreesIt()
    {
        // Arrange
        using var form = new DiagramEditorForm();
        List<DiagramLockChoiceModel> toggled = [];
        form.LockToggled += (_, choice) => toggled.Add(choice);

        // Act
        form.ShowProperties(new DiagramPropertiesModel(
            "Rectangle",
            new Rectangle(0, 0, 100, 50),
            Locks: DiagramLock.Width));

        // Assert: showing it isn't the user's choice
        var widthLock = (CheckBox)form.Controls.Find("WidthLockCheckBox", true).Single();
        var xLock = (CheckBox)form.Controls.Find("XLockCheckBox", true).Single();
        Assert.True(widthLock.Checked);
        Assert.Equal("\U0001F512", widthLock.Text);
        Assert.False(form.Controls.Find("WidthBox", true).Single().Enabled);
        Assert.False(xLock.Checked);
        Assert.True(form.Controls.Find("XBox", true).Single().Enabled);
        Assert.Empty(toggled);

        widthLock.Checked = false;
        Assert.Equal([new DiagramLockChoiceModel(DiagramLock.Width, false)], toggled);
        Assert.Equal("\U0001F513", widthLock.Text);
    }

    [Fact]
    public void Properties_ALinesDirection_IsShownOnlyForLines_AndChoosingItIsRaised()
    {
        // Arrange
        using var form = new DiagramEditorForm();
        List<DiagramLineDirection> chosen = [];
        form.LineDirectionSelected += (_, direction) => chosen.Add(direction);
        var directions = (ComboBox)form.Controls.Find("DirectionPropertyBox", true).Single();

        // Act / Assert: showing it isn't the user's choice
        form.Show();
        form.ShowPropertiesPanel(true);
        form.ShowProperties(new DiagramPropertiesModel("Rectangle", new Rectangle(0, 0, 100, 50)));
        Assert.False(directions.Visible);

        form.ShowProperties(new DiagramPropertiesModel("Line", LineDirection: DiagramLineDirection.Vertical));
        Assert.True(directions.Visible);
        Assert.Equal("Vertical", directions.SelectedItem);
        Assert.Empty(chosen);

        directions.SelectedIndex = 1;
        Assert.Equal([DiagramLineDirection.Horizontal], chosen);
    }

    [Theory]
    [InlineData("DuplicateMenuItem")]
    [InlineData("DuplicateContextMenuItem")]
    public void Duplicate_InTheEditAndRightClickMenus_AsksToDuplicate(string item)
    {
        // Arrange
        using var form = new DiagramEditorForm();
        List<DiagramCommand> commands = [];
        form.CommandRequested += (_, command) => commands.Add(command);

        // Act
        ClickToolbarButton(form, item);

        // Assert
        Assert.Equal([DiagramCommand.Duplicate], commands);
    }

    [Fact]
    public void Properties_KeepProportions_ShowsTheLock_AndResetSizeIsOnlyForImages()
    {
        // Arrange
        using var form = new DiagramEditorForm();
        List<DiagramLockChoiceModel> toggled = [];
        List<DiagramCommand> commands = [];
        form.LockToggled += (_, choice) => toggled.Add(choice);
        form.CommandRequested += (_, command) => commands.Add(command);
        var proportions = (CheckBox)form.Controls.Find("ProportionsCheckBox", true).Single();
        var reset = (Button)form.Controls.Find("ResetSizeButton", true).Single();
        form.Show();
        form.ShowPropertiesPanel(true);

        // Act / Assert: showing it isn't the user's choice
        form.ShowProperties(
            new DiagramPropertiesModel("Rectangle", new Rectangle(0, 0, 9, 9), Locks: DiagramLock.None));
        Assert.True(proportions.Visible);
        Assert.False(proportions.Checked);
        Assert.False(reset.Visible);

        form.ShowProperties(new DiagramPropertiesModel(
            "Image",
            new Rectangle(0, 0, 9, 9),
            Locks: DiagramLock.AspectRatio,
            HasImages: true));
        Assert.True(proportions.Checked);
        Assert.True(reset.Visible);
        Assert.Empty(toggled);

        proportions.Checked = false;
        reset.PerformClick();
        Assert.Equal([new DiagramLockChoiceModel(DiagramLock.AspectRatio, false)], toggled);
        Assert.Equal([DiagramCommand.ResetSize], commands);
    }

    [Fact]
    public void ImagesPanel_ShowsTheImagesInTheirGroups_AndChoosingOneAsksToInsertIt()
    {
        // Arrange
        using var form = new DiagramEditorForm();
        List<string> chosen = [];
        form.CatalogImageChosen += (_, path) => chosen.Add(path);
        var list = (ListView)form.Controls.Find("ImagesListView", true).Single();
        var empty = (Label)form.Controls.Find("ImagesEmptyLabel", true).Single();
        const string Markup = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 9 9"/>""";
        form.Show();
        form.ShowImagesPanel(true);

        // Act / Assert
        form.ShowCatalog([], "Nothing yet");
        Assert.True(empty.Visible);
        Assert.Equal("Nothing yet", empty.Text);

        form.ShowCatalog(
            [
                new DiagramCatalogImageModel("Azure/vm.svg", "VM", "Recent", Markup),
                new DiagramCatalogImageModel("Azure/vm.svg", "VM", "Azure", Markup),
                new DiagramCatalogImageModel("Azure/app.svg", "App", "Azure", Markup),
            ],
            "Nothing yet");
        Assert.False(empty.Visible);
        Assert.Equal(["Recent", "Azure"], list.Groups.Cast<ListViewGroup>().Select(group => group.Header));
        Assert.Equal(3, list.Items.Count);

        list.Items[2].Focused = true;
        form.ChooseFocusedImage();
        Assert.Equal(["Azure/app.svg"], chosen);
    }

    [Fact]
    public void Properties_TextAlign_ShowsTheAlignment_AndChoosingOneIsRaised()
    {
        // Arrange
        using var form = new DiagramEditorForm();
        List<DiagramTextAlignment> chosen = [];
        form.TextAlignmentSelected += (_, alignment) => chosen.Add(alignment);
        var align = (ComboBox)form.Controls.Find("TextAlignPropertyBox", true).Single();
        form.Show();
        form.ShowPropertiesPanel(true);

        // Act / Assert: showing it isn't the user's choice
        form.ShowProperties(new DiagramPropertiesModel("Container", TextAlignment: DiagramTextAlignment.Left));
        Assert.True(align.Visible);
        Assert.Equal("Left", align.SelectedItem);
        Assert.Empty(chosen);

        align.SelectedIndex = 2;
        Assert.Equal([DiagramTextAlignment.Right], chosen);

        form.ShowProperties(new DiagramPropertiesModel("Nothing selected"));
        Assert.False(align.Visible);
    }

    [Fact]
    public void Properties_TitleFill_WithoutItsOwn_ShowsTheFillItHas_AndSaysSo()
    {
        // Arrange
        using var form = new DiagramEditorForm();
        List<DiagramColorChoiceModel> chosen = [];
        form.ColorSelected += (_, choice) => chosen.Add(choice);
        var button = (Button)form.Controls.Find("TitleFillColorButton", true).Single();
        Color blue = Color.FromArgb(0, 99, 177);
        Color orange = Color.FromArgb(255, 140, 0);
        form.Show();

        // Act / Assert: the band is the fill's color, and so is its button
        form.ShowProperties(new DiagramPropertiesModel("Container", Fill: blue, TitleFill: Color.Empty));
        Assert.Equal((blue, "Same as fill", Color.White), (button.BackColor, button.Text, button.ForeColor));

        form.ShowProperties(new DiagramPropertiesModel("Container", Fill: blue, TitleFill: orange));
        Assert.Equal((orange, string.Empty), (button.BackColor, button.Text));

        form.ShowProperties(new DiagramPropertiesModel("Container", Fill: Color.Empty, TitleFill: Color.Empty));
        Assert.Equal("None", button.Text);

        var sameAsFill = (ToolStripMenuItem)typeof(DiagramEditorForm)
            .GetField("SameAsFillMenuItem", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;
        var noTitleFill = (ToolStripMenuItem)typeof(DiagramEditorForm)
            .GetField("NoTitleFillMenuItem", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(form)!;
        sameAsFill.PerformClick();
        noTitleFill.PerformClick();
        Assert.Equal(
            [
                new DiagramColorChoiceModel(DiagramColorTarget.TitleFill, Color.Empty),
                new DiagramColorChoiceModel(DiagramColorTarget.TitleFill, DiagramStyleModel.NoTitleFill),
            ],
            chosen);

        // A title fill of none is "None": a button can't be see-through
        form.ShowProperties(
            new DiagramPropertiesModel("Container", Fill: blue, TitleFill: DiagramStyleModel.NoTitleFill));
        Assert.Equal("None", button.Text);
    }

    private static DiagramEditorPresenter NewPresenter(DiagramEditorForm form) => new(
        form,
        new NoStore(),
        new NoSettings(),
        new NoExporter(),
        new PngDiagramExporter(),
        new NoCatalog(),
        new NoRecovery(),
        new NoPicker());

    [Theory]
    [InlineData(CloseReason.WindowsShutDown)]
    [InlineData(CloseReason.ApplicationExitCall)]
    [InlineData(CloseReason.TaskManagerClosing)]
    public void Closing_NotByTheUser_AsksNothing_ButSaysTheWindowClosed(CloseReason reason)
    {
        // Arrange
        List<string> events = [];
        using var form = new DiagramEditorForm();
        IDiagramEditorView view = form;
        view.Closing += (_, _) => events.Add("closing");
        view.WindowClosed += (_, _) => events.Add("closed");

        // Act
        typeof(DiagramEditorForm)
            .GetMethod("DiagramEditorForm_FormClosing", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, [form, new FormClosingEventArgs(reason, false)]);

        // Assert
        Assert.Equal(["closed"], events);
    }

    [Fact]
    public void RecoverQuestion_NamesEachDiagram_WithWhenItsCopyWasMade()
    {
        // Arrange
        DateTime savedAt = new(2026, 11, 14, 15, 30, 0, DateTimeKind.Utc);
        DiagramRecoveryModel[] left =
        [
            new("a", @"C:\work\flow.svg", null, DiagramModel.Empty, savedAt),
            new("b", null, null, DiagramModel.Empty, savedAt),
        ];

        // Act
        string question = OpenDiagramEditorAction.RecoverQuestion(left, CultureInfo.InvariantCulture);

        // Assert
        string time = savedAt.ToLocalTime().ToString("g", CultureInfo.InvariantCulture);
        Assert.Contains("these diagrams", question);
        Assert.Contains($"• flow.svg, {time}\n• Untitled, {time}", question);
        Assert.Contains("Cancel: ask again next time.", question);
    }

    private sealed class NoPicker : IScreenColorPicker
    {
        public Task<PickedColorModel?> PickAsync() => Task.FromResult<PickedColorModel?>(null);
    }

    private sealed class NoRecovery : IDiagramRecoveryStore
    {
        public string NewId() => Guid.NewGuid().ToString("N");

        public void Save(DiagramRecoveryModel recovery)
        {
        }

        public void Delete(string id)
        {
        }

        public IReadOnlyList<DiagramRecoveryModel> Left() => [];

        public void Adopt(string id)
        {
        }
    }

    private sealed class NoSettings : IDiagramEditorSettingsStore
    {
        public DiagramEditorSettingsModel? Load() => null;

        public void Save(DiagramEditorSettingsModel settings)
        {
        }
    }

    private sealed class NoStore : IDiagramFileStore
    {
        public void Save(string path, DiagramModel diagram)
        {
        }

        public DiagramModel Load(string path) => DiagramModel.Empty;

        public string? VersionOf(string path) => null;

        public string ReadImage(string path) => string.Empty;
    }

    private sealed class NoCatalog : IDiagramImageCatalog
    {
        public string Folder => string.Empty;

        public void CreateFolder()
        {
        }

        public IReadOnlyList<DiagramCatalogImageModel> Images() => [];

        public IReadOnlyList<string> Recent() => [];

        public void Add(string markup, string fileName)
        {
        }

        public void MarkUsed(string path)
        {
        }
    }

    private sealed class NoExporter : IDiagramPdfExporter
    {
        public void Export(string path, DiagramModel diagram)
        {
        }
    }
}
