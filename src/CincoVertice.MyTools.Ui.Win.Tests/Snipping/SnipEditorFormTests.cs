using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Win.Snipping;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Snipping;

// Some send real keys to the window in front
[Collection(KeyboardFocusCollection.Name)]
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
        Assert.Equal(0, Field<int>(form, "_shownSnip"));
        Assert.Equal(new Size(50, 40), Field<PictureBox>(form, "Canvas").Size);
    }

    [Fact]
    public void AddSnip_EndsTheTextBeingTyped_BeforeTheNewSnip()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(50, 40, Color.Red));
        List<string> happened = [];
        form.TextCommitted += (_, input) => happened.Add($"text {input.Text}");
        form.SnipAdded += (_, _) => happened.Add("snip");
        form.BeginTextInput(new Point(1, 2), Color.Black, 14, "typed");

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
        Assert.Equal(new Size(60, 30), Field<PictureBox>(form, "Canvas").Size);
        form.SaveImage(secondPath);

        form.ShowSnip(0, 2);
        Assert.Equal(new Size(50, 40), Field<PictureBox>(form, "Canvas").Size);
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
        var form = new SnipEditorForm(Solid(1100, 300, Color.Red));

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
        using SnipEditorForm form = WithTwoSnips(new Size(1200, 450), corner);
        int toolbar = Field<ToolStrip>(form, "Tools").Height;

        // Act
        form.ShowSnip(1, 2);

        // Assert
        Assert.Equal(corner, form.Location);
        Assert.Equal(450 + toolbar, form.ClientSize.Height);
        Assert.Equal(1200, form.ClientSize.Width);

        // Act: back to the smaller one
        form.ShowSnip(0, 2);

        // Assert
        Assert.Equal(corner, form.Location);
        Assert.Equal(300 + toolbar, form.ClientSize.Height);
        Assert.Equal(1100, form.ClientSize.Width);
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

    [DllImport("user32.dll")]
    private static extern nint GetFocus();

    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint hwnd, int msg, nint wParam, nint lParam);

    private static void Pump()
    {
        for (int i = 0; i < 30; i++)
        {
            Application.DoEvents();
        }
    }

    /// <summary>Sends a key where the keyboard would: to whatever has the focus.</summary>
    private static void PressKey(Keys key)
    {
        const int WM_KEYDOWN = 0x0100;
        const int WM_KEYUP = 0x0101;

        nint focused = GetFocus();

        PostMessage(focused, WM_KEYDOWN, (nint)key, 1);
        PostMessage(focused, WM_KEYUP, (nint)key, unchecked((nint)0xC0000001));
        Pump();
    }

    /// <summary>
    ///     The keys sent here come with the real keyboard's Shift, Ctrl and Alt, as typed keys do: with one held, as
    ///     while someone uses the computer, they would mean something else.
    /// </summary>
    private static void SkipWhileAModifierIsHeld()
    {
        Assert.SkipWhen(Control.ModifierKeys != Keys.None, "Shift, Ctrl or Alt is held on the keyboard.");
    }

    /// <summary>The window in front with a text being typed, so the keyboard goes where it really would.</summary>
    private static SnipEditorForm TypingInTheForeground()
    {
        var form = new SnipEditorForm(Solid(300, 200, Color.White));

        form.ShowInTaskbar = false;
        form.Show();
        form.Location = new Point(-5000, 100);
        WindowHelper.ForceForeground(form.Handle);
        form.Activate();
        Pump();
        form.BeginTextInput(new Point(10, 10), Color.Black, 14, "abc");
        Pump();

        // Without the focus nothing here would mean anything, e.g. with the computer locked
        Assert.SkipUnless(Field<TextBox>(form, "TextInputBox").Focused, "This window can't get the keyboard focus.");
        SkipWhileAModifierIsHeld();

        return form;
    }

    [Theory]
    [InlineData(Keys.Enter)]
    [InlineData(Keys.Escape)]
    public void ToolKeys_WorkAfterTheTextEnds(Keys endingKey)
    {
        // Arrange
        using SnipEditorForm form = TypingInTheForeground();
        List<SnipTool> tools = [];
        form.ToolSelected += (_, tool) => tools.Add(tool);

        if (endingKey == Keys.Escape)
        {
            PressEsc(form);
        }
        else
        {
            PressKey(endingKey);
        }

        // Act
        PressKey(Keys.V);
        PressKey(Keys.P);

        // Assert: and the box they'd have gone to is empty of them
        Assert.Equal([SnipTool.Select, SnipTool.Pen], tools);
        Assert.False(Field<TextBox>(form, "TextInputBox").Focused);
    }

    /// <summary>The window in front with something selected, and nothing being typed.</summary>
    private static SnipEditorForm SelectedInTheForeground()
    {
        SnipEditorForm form = TypingInTheForeground();

        PressKey(Keys.Enter);
        form.ShowSelection(new SnipSelectionModel(new Rectangle(20, 20, 50, 50), []));
        Pump();

        return form;
    }

    [Theory]
    [InlineData(Keys.Left, -10, 0)]
    [InlineData(Keys.Right, 10, 0)]
    [InlineData(Keys.Up, 0, -10)]
    [InlineData(Keys.Down, 0, 10)]
    public void ArrowKeys_NudgeWhatsSelected(Keys key, int x, int y)
    {
        // Arrange
        using SnipEditorForm form = SelectedInTheForeground();
        List<Size> nudges = [];
        form.NudgeRequested += (_, delta) => nudges.Add(delta);

        // Act
        PressKey(key);

        // Assert
        Assert.Equal([new Size(x, y)], nudges);
    }

    [Theory]
    [InlineData(Keys.Left, -10, 0)]
    [InlineData(Keys.Down, 0, 10)]
    [InlineData(Keys.Left | Keys.Shift, -1, 0)]
    public void ArrowKeys_NudgeWhatsSelected_InANewWindowWithNothingTyped(Keys key, int x, int y)
    {
        // Arrange: the window just as it opens, where the canvas's panel and lists are what arrows could go to
        using var form = new SnipEditorForm(Solid(300, 200, Color.White));
        form.ShowInTaskbar = false;
        form.Show();
        form.Location = new Point(-5000, 100);
        WindowHelper.ForceForeground(form.Handle);
        form.Activate();
        Pump();
        Assert.SkipWhen(GetFocus() == 0, "This window can't get the keyboard focus.");
        SkipWhileAModifierIsHeld();
        form.ShowSelection(new SnipSelectionModel(new Rectangle(20, 20, 50, 50), []));
        List<Size> nudges = [];
        form.NudgeRequested += (_, delta) => nudges.Add(delta);

        // Act: Shift held, as the keyboard would have it, for what the key says about it
        if ((key & Keys.Shift) != 0)
        {
            InvokeKeyDown(form, new KeyEventArgs(key));
        }
        else
        {
            PressKey(key);
        }

        // Assert
        Assert.Equal([new Size(x, y)], nudges);
    }

    [Fact]
    public void TheEmojiList_HasTheOnesUsedLast_AtTheTop_ThenEveryGroup()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));

        // Act
        form.ShowEmojis(["🔥", "👍"], "👍");

        // Assert
        var button = Field<ToolStripDropDownButton>(form, "EmojiDropDownButton");
        var recent = Field<EmojiPaletteControl>(form, "RecentEmojis");
        Assert.True(button.Available);
        Assert.NotNull(button.Image);
        Assert.Equal(["🔥", "👍"], recent.Emojis);
        Assert.Equal("👍", recent.SelectedEmoji);
        Assert.Equal(
            ["Used last", string.Empty, string.Empty, "Faces", "Gestures", "Marks and arrows", "Work",
                "Hearts and symbols", "Nature and food"],
            button.DropDownItems.Cast<ToolStripItem>().Select(item => item.Text ?? string.Empty));
    }

    [Fact]
    public void TheEmojiList_WithoutRecentOnes_StartsWithTheGroups_AndHidesWithoutAnEmoji()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        var button = Field<ToolStripDropDownButton>(form, "EmojiDropDownButton");

        // Act and assert
        form.ShowEmojis([], "😀");
        Assert.False(Field<ToolStripLabel>(form, "RecentEmojisLabel").Available);
        Assert.True(button.Available);

        form.ShowEmojis([], null);
        Assert.False(button.Available);
    }

    [Fact]
    public void PickingAnEmojiFromTheList_RaisesIt()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        var picked = new List<string>();
        form.EmojiSelected += (_, emoji) => picked.Add(emoji);

        // Act
        typeof(SnipEditorForm)
            .GetMethod("EmojiPalette_EmojiPicked", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, [form, "✅"]);

        // Assert
        Assert.Equal(["✅"], picked);
    }

    [Fact]
    public void E_ChoosesTheEmojiTool()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        var tools = new List<SnipTool>();
        form.ToolSelected += (_, tool) => tools.Add(tool);

        // Act
        InvokeKeyDown(form, new KeyEventArgs(Keys.E));

        // Assert
        Assert.Equal([SnipTool.Emoji], tools);
    }

    private static void InvokeKeyDown(SnipEditorForm form, KeyEventArgs e)
    {
        typeof(SnipEditorForm)
            .GetMethod("SnipEditorForm_KeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, [form, e]);
    }

    [Fact]
    public void ArrowKeys_NudgeWhatsSelected_AfterChoosingFromAList()
    {
        // Arrange: a choice from the toolbar shouldn't leave the keys with the list
        using SnipEditorForm form = SelectedInTheForeground();
        form.ShowSizes([1, 2, 3], 2);
        List<Size> nudges = [];
        int? picked = null;
        form.NudgeRequested += (_, delta) => nudges.Add(delta);
        form.SizeSelected += (_, size) => picked = size;
        var sizes = Field<ToolStripComboBox>(form, "SizeComboBox");
        sizes.Focus();
        Pump();

        // Act
        sizes.SelectedIndex = 2;
        Pump();
        PressKey(Keys.Left);

        // Assert
        Assert.Equal(3, picked);
        Assert.Equal([new Size(-10, 0)], nudges);
        Assert.Equal(2, sizes.SelectedIndex);
    }

    [Fact]
    public void ArrowKeys_AreTheTextsWhileTyping()
    {
        // Arrange
        using SnipEditorForm form = TypingInTheForeground();
        form.ShowSelection(new SnipSelectionModel(new Rectangle(20, 20, 50, 50), []));
        List<Size> nudges = [];
        form.NudgeRequested += (_, delta) => nudges.Add(delta);

        // Act
        PressKey(Keys.Left);

        // Assert
        Assert.Empty(nudges);
    }

    [Fact]
    public void AltArrowKeys_StillGoToAnotherSnip_WithSomethingSelected()
    {
        // Arrange
        using SnipEditorForm form = SelectedInTheForeground();
        List<Size> nudges = [];
        form.NudgeRequested += (_, delta) => nudges.Add(delta);

        // Act: what ProcessCmdKey is given for Alt+Left
        var message = default(Message);
        typeof(SnipEditorForm)
            .GetMethod("ProcessCmdKey", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, [message, Keys.Alt | Keys.Left]);

        // Assert
        Assert.Empty(nudges);
    }

    [Fact]
    public void ToolKeys_ArePartOfTheTextWhileTyping()
    {
        // Arrange
        using SnipEditorForm form = TypingInTheForeground();
        List<SnipTool> tools = [];
        form.ToolSelected += (_, tool) => tools.Add(tool);

        // Act
        PressKey(Keys.V);

        // Assert
        Assert.Empty(tools);
    }

    private static void PressEsc(SnipEditorForm form)
    {
        var message = default(Message);

        typeof(SnipEditorForm)
            .GetMethod("ProcessCmdKey", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, [message, Keys.Escape]);
    }

    [Theory]
    [InlineData("typed")]
    [InlineData("")]
    public void Esc_WhileTyping_EndsTheTextOnce_AndLeavesTheWindowOpen(string text)
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        List<TextInputModel> escaped = [];
        int committed = 0;
        form.TextEscaped += (_, input) => escaped.Add(input);
        form.TextCommitted += (_, _) => committed++;
        form.BeginTextInput(new Point(1, 2), Color.Black, 14, text);

        // Act
        PressEsc(form);

        // Assert
        Assert.Equal([new TextInputModel(new Point(1, 2), text)], escaped);
        Assert.Equal(0, committed);
        Assert.False(form.IsDisposed);
        Assert.False(Field<TextBox>(form, "TextInputBox").Visible);
    }

    [Fact]
    public void Esc_AfterTheTextEnded_ThenDeselects_AndOnlyThenCloses()
    {
        // Arrange: shown, as closing needs
        var form = new SnipEditorForm(Solid(100, 100, Color.White));
        form.ShowInTaskbar = false;
        form.Show();
        form.Location = new Point(-5000, 100);
        int deselected = 0;
        form.DeselectRequested += (_, _) =>
        {
            deselected++;
            form.ShowSelection(null);
        };
        form.ShowSelection(new SnipSelectionModel(new Rectangle(1, 1, 5, 5), []));

        // Act & Assert
        PressEsc(form);
        Assert.Equal(1, deselected);
        Assert.False(form.IsDisposed);

        PressEsc(form);
        Assert.True(form.IsDisposed);
    }

    private static SnipEditorForm ShownOffScreen()
    {
        var form = new SnipEditorForm(Solid(100, 100, Color.White));

        form.ShowInTaskbar = false;
        form.Show();
        form.Location = new Point(-5000, 100);

        return form;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Close_ByTheUser_AsksFirst_AndStaysOpenWhenCancelled(bool cancel)
    {
        // Arrange
        SnipEditorForm form = ShownOffScreen();
        int asked = 0;
        form.CloseRequested += (_, e) =>
        {
            asked++;
            e.Cancel = cancel;
        };

        // Act: as the X, Alt+F4 and Esc do
        form.Close();

        // Assert
        Assert.Equal(1, asked);
        Assert.Equal(!cancel, form.IsDisposed);
        form.Dispose();
    }

    [Theory]
    [InlineData(CloseReason.WindowsShutDown)]
    [InlineData(CloseReason.ApplicationExitCall)]
    [InlineData(CloseReason.TaskManagerClosing)]
    public void Close_ForAnyOtherReason_DoesntAsk(CloseReason reason)
    {
        // Arrange
        using SnipEditorForm form = ShownOffScreen();
        int asked = 0;
        form.CloseRequested += (_, e) =>
        {
            asked++;
            e.Cancel = true;
        };
        var closing = new FormClosingEventArgs(reason, cancel: false);

        // Act
        typeof(Form)
            .GetMethod("OnFormClosing", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, [closing]);

        // Assert
        Assert.Equal(0, asked);
        Assert.False(closing.Cancel);
    }

    [Fact]
    public void Close_WhileTyping_KeepsTheTextBeforeAsking()
    {
        // Arrange: the text counts as a change to the snip
        SnipEditorForm form = ShownOffScreen();
        List<string> happened = [];
        form.TextCommitted += (_, input) => happened.Add($"text {input.Text}");
        form.CloseRequested += (_, _) => happened.Add("close");
        form.BeginTextInput(new Point(1, 2), Color.Black, 14, "typed");

        // Act
        form.Close();

        // Assert
        Assert.Equal(["text typed", "close"], happened);
        form.Dispose();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void HideWhileSnipping_ShowsAsChecked_AndClickingAsksForTheOpposite(bool shown, bool asked)
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        var item = Field<ToolStripMenuItem>(form, "HideWhileSnippingMenuItem");
        bool? selected = null;
        form.HideWhileSnippingSelected += (_, isHidden) => selected = isHidden;

        // Act
        form.ShowHideWhileSnipping(shown);
        item.PerformClick();

        // Assert: the presenter says what's checked, not the click
        Assert.Equal(asked, selected);
        Assert.Equal(shown, item.Checked);
    }

    private static void Mouse(SnipEditorForm form, string handler, MouseButtons button, int x, int y)
    {
        typeof(SnipEditorForm)
            .GetMethod(handler, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, [form, new MouseEventArgs(button, 1, x, y, 0)]);
    }

    /// <summary>A snip with a different color in each quarter: red, green, blue and yellow, clockwise from the top left.</summary>
    private static Bitmap Quarters(int width, int height)
    {
        var snip = new Bitmap(width, height);

        using var graphics = Graphics.FromImage(snip);

        graphics.FillRectangle(Brushes.Red, 0, 0, width / 2, height / 2);
        graphics.FillRectangle(Brushes.Lime, width / 2, 0, width - width / 2, height / 2);
        graphics.FillRectangle(Brushes.Yellow, 0, height / 2, width / 2, height - height / 2);
        graphics.FillRectangle(Brushes.Blue, width / 2, height / 2, width - width / 2, height - height / 2);

        return snip;
    }

    [Fact]
    public void Crop_SavesOnlyThatPart_WithTheAnnotationsOnWhatTheyWereDrawnOver()
    {
        // Arrange: kept is the bottom right quarter, blue, with a black rectangle drawn over its corner
        using var form = new SnipEditorForm(Quarters(200, 100));
        string path = SavedImagePath();
        form.ShowAnnotations([new RectangleAnnotationModel(new Rectangle(110, 60, 30, 20), Color.Black, 2, 0)]);

        // Act
        form.ShowCrop(new Rectangle(100, 50, 100, 50));
        form.SaveImage(path);

        // Assert
        using var saved = new Bitmap(path);
        Assert.Equal(new Size(100, 50), saved.Size);
        Assert.Equal(Color.Blue.ToArgb(), saved.GetPixel(50, 25).ToArgb());
        Assert.True(saved.GetPixel(10, 20).R < 80 && saved.GetPixel(10, 20).B < 80, "The rectangle moved with the crop");
    }

    [Fact]
    public void Crop_TheCanvasShowsOnlyThatPart_AndTheMouseGivesPixelsOfTheWholeSnip()
    {
        // Arrange
        using var form = new SnipEditorForm(Quarters(200, 100));
        List<Point> points = [];
        form.PointerPressed += (_, pointer) => points.Add(pointer.Location);

        // Act
        form.ShowCrop(new Rectangle(100, 50, 100, 50));
        form.ShowZoom(200);
        Mouse(form, "Canvas_MouseDown", MouseButtons.Left, 10, 6);

        // Assert: (5, 3) of the crop, at 200%, is (105, 53) of the snip
        Assert.Equal(new Size(200, 100), Field<PictureBox>(form, "Canvas").Size);
        Assert.Equal([new Point(105, 53)], points);
    }

    [Fact]
    public void Crop_TheCanvasDrawsThatPart()
    {
        // Arrange
        using var form = new SnipEditorForm(Quarters(200, 100));
        var canvas = Field<PictureBox>(form, "Canvas");

        // Act
        form.ShowCrop(new Rectangle(100, 0, 100, 50));
        using var shown = new Bitmap(canvas.Width, canvas.Height);
        canvas.DrawToBitmap(shown, new Rectangle(Point.Empty, canvas.Size));

        // Assert: the top right quarter
        Assert.Equal(Color.Lime.ToArgb(), shown.GetPixel(50, 25).ToArgb());
    }

    [Fact]
    public void CropPreview_DimsWhatWouldBeCutAway_OnTheScreenOnly()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        var canvas = Field<PictureBox>(form, "Canvas");
        string path = SavedImagePath();

        // Act
        form.ShowCropPreview(new Rectangle(20, 20, 50, 50));
        using var shown = new Bitmap(canvas.Width, canvas.Height);
        canvas.DrawToBitmap(shown, new Rectangle(Point.Empty, canvas.Size));
        form.SaveImage(path);

        // Assert
        Assert.True(shown.GetPixel(5, 5).R < 200, "Outside isn't dimmed");
        Assert.Equal(Color.White.ToArgb(), shown.GetPixel(45, 45).ToArgb());
        using var saved = new Bitmap(path);
        Assert.Equal(Color.White.ToArgb(), saved.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void CropPreview_ShowsItsButtons_AndTheKeysApplyOrCancelIt()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        var apply = Field<ToolStripButton>(form, "ApplyCropButton");
        var cancel = Field<ToolStripButton>(form, "CancelCropButton");
        List<string> asked = [];
        form.CropApplyRequested += (_, _) => asked.Add("apply");
        form.CropCancelRequested += (_, _) => asked.Add("cancel");
        var process = typeof(SnipEditorForm).GetMethod("ProcessCmdKey", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Assert: only with a crop to adjust
        Assert.False(apply.Available);
        form.ShowCropPreview(new Rectangle(10, 10, 50, 50));
        Assert.True(apply.Available && cancel.Available);

        // Act
        process.Invoke(form, [default(Message), Keys.Enter]);
        process.Invoke(form, [default(Message), Keys.Escape]);
        apply.PerformClick();
        cancel.PerformClick();
        form.ShowCropPreview(null);

        // Assert
        Assert.Equal(["apply", "cancel", "apply", "cancel"], asked);
        Assert.False(apply.Available);
        Assert.False(form.IsDisposed);
    }

    [Fact]
    public void CropPreview_HasHandles()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        var canvas = Field<PictureBox>(form, "Canvas");

        // Act
        form.ShowCropPreview(new Rectangle(20, 20, 60, 60));
        using var shown = new Bitmap(canvas.Width, canvas.Height);
        canvas.DrawToBitmap(shown, new Rectangle(Point.Empty, canvas.Size));

        // Assert: the handle at the top middle is the selection's blue, its middle white
        Color edge = shown.GetPixel(50 - 4, 20);
        Assert.True(edge.B > 150 && edge.R < 100, $"No handle at the top middle: {edge}");
    }

    [Fact]
    public void SelectionBand_IsShownOnTheScreenOnly()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        var canvas = Field<PictureBox>(form, "Canvas");
        string path = SavedImagePath();

        // Act
        form.ShowSelectionBand(new Rectangle(20, 20, 50, 50));
        using var shown = new Bitmap(canvas.Width, canvas.Height);
        canvas.DrawToBitmap(shown, new Rectangle(Point.Empty, canvas.Size));
        form.SaveImage(path);

        // Assert: tinted blue inside, and nothing of it in the image
        Color inside = shown.GetPixel(45, 45);
        Assert.True(inside.B > inside.R, $"Not tinted: {inside}");
        using var saved = new Bitmap(path);
        Assert.Equal(Color.White.ToArgb(), saved.GetPixel(45, 45).ToArgb());
    }

    [Fact]
    public void CtrlA_AsksToSelectEverything()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        int asked = 0;
        form.SelectAllRequested += (_, _) => asked++;

        // Act
        InvokeKeyDown(form, new KeyEventArgs(Keys.Control | Keys.A));

        // Assert
        Assert.Equal(1, asked);
    }

    [Fact]
    public void GetSnipSize_IsTheWholeSnip_WhateverTheCrop()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(120, 80, Color.White));

        // Act
        form.ShowCrop(new Rectangle(10, 10, 20, 20));

        // Assert
        Assert.Equal(new Size(120, 80), form.GetSnipSize(0));
    }

    [Fact]
    public void Zoom_SizesTheCanvas_AndSaysHowLarge()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(50, 40, Color.Red));

        // Act & Assert
        form.ShowZoom(200);
        Assert.Equal(new Size(100, 80), Field<PictureBox>(form, "Canvas").Size);
        Assert.Equal("200%", Field<ToolStripLabel>(form, "ZoomLabel").Text);

        form.ShowZoom(25);
        Assert.Equal(new Size(13, 10), Field<PictureBox>(form, "Canvas").Size);
    }

    [Theory]
    [InlineData(200, 41, 21, 20, 10)]
    [InlineData(400, 7, 4, 1, 1)]
    [InlineData(50, 30, 10, 60, 20)]
    public void Zoom_TheMouseGivesSnipPixels(int percent, int x, int y, int snipX, int snipY)
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(200, 200, Color.White));
        List<Point> points = [];
        form.PointerPressed += (_, pointer) => points.Add(pointer.Location);
        form.PointerMoved += (_, point) => points.Add(point);
        form.PointerReleased += (_, point) => points.Add(point);
        form.PointerHovered += (_, point) => points.Add(point);
        form.ShowZoom(percent);

        // Act
        Mouse(form, "Canvas_MouseDown", MouseButtons.Left, x, y);
        Mouse(form, "Canvas_MouseMove", MouseButtons.Left, x, y);
        Mouse(form, "Canvas_MouseUp", MouseButtons.Left, x, y);
        Mouse(form, "Canvas_MouseMove", MouseButtons.None, x, y);

        // Assert
        Assert.Equal(Enumerable.Repeat(new Point(snipX, snipY), 4), points);
    }

    [Fact]
    public void Zoom_ShowsEachPixelAsASharpSquare_WithTheAnnotationsScaled()
    {
        // Arrange: a white snip with one black pixel, and a red line across
        Bitmap snip = Solid(20, 20, Color.White);
        snip.SetPixel(5, 5, Color.Black);
        using var form = new SnipEditorForm(snip);
        form.ShowAnnotations([new RectangleAnnotationModel(new Rectangle(10, 2, 8, 8), Color.Red, 1, 0)]);
        form.ShowZoom(400);
        var canvas = Field<PictureBox>(form, "Canvas");

        // Act
        using var shown = new Bitmap(canvas.Width, canvas.Height);
        canvas.DrawToBitmap(shown, new Rectangle(Point.Empty, canvas.Size));

        // Assert: the pixel is 4 by 4, edge to edge, and the rectangle is where its snip pixels are
        for (int dx = 0; dx < 4; dx++)
        {
            for (int dy = 0; dy < 4; dy++)
            {
                Assert.Equal(Color.Black.ToArgb(), shown.GetPixel(20 + dx, 20 + dy).ToArgb());
            }
        }

        Assert.Equal(Color.White.ToArgb(), shown.GetPixel(19, 20).ToArgb());
        Assert.Equal(Color.White.ToArgb(), shown.GetPixel(24, 20).ToArgb());
        Assert.True(shown.GetPixel(41, 20).R > 200 && shown.GetPixel(41, 20).G < 100);
    }

    [Fact]
    public void Zoom_FromTheKeys_KeepsTheMiddleOfWhatShowsWhereItWas()
    {
        // Arrange: a small snip in a larger window, so the snip ends before the window does
        using var form = new SnipEditorForm(Solid(400, 200, Color.White));
        form.ShowInTaskbar = false;
        form.Show();
        form.ClientSize = new Size(1000, 330);
        form.Location = new Point(-5000, 100);
        var panel = Field<Panel>(form, "CanvasPanel");

        // Act
        form.ShowZoom(400);

        // Assert: the snip's middle, (200, 100) on the screen, is (800, 400) at 400%, scrolled to the same place on the
        // screen, as the mouse's point is with Ctrl+wheel. Not the window's middle: that's past the snip's edge here
        Assert.Equal(new Point(-600, -300), panel.AutoScrollPosition);
        form.Close();
    }

    [Fact]
    public void Zoom_LeavesTheCopiedAndSavedImageAtTheSnipsSize()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(50, 40, Color.Red));
        string path = SavedImagePath();
        form.ShowZoom(300);

        // Act
        form.SaveImage(path);

        // Assert
        using var saved = new Bitmap(path);
        Assert.Equal(new Size(50, 40), saved.Size);
    }

    [Fact]
    public void Zoom_EndsTheTextBeingTyped()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        TextInputModel? committed = null;
        form.TextCommitted += (_, input) => committed = input;
        form.BeginTextInput(new Point(10, 20), Color.Black, 14, "typed");

        // Act
        form.ShowZoom(200);

        // Assert: where it is on the snip, not on the screen
        Assert.Equal(new TextInputModel(new Point(10, 20), "typed"), committed);
    }

    [Fact]
    public void TextBox_AtAZoom_IsWhereItsTextGoesOnTheScreen()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        form.ShowZoom(200);

        // Act
        form.BeginTextInput(new Point(10, 20), Color.Black, 14, string.Empty);

        // Assert
        var box = Field<TextBox>(form, "TextInputBox");
        Assert.Equal(new Point(20, 40), box.Location);
        Assert.Equal(28, box.Font.Size);
    }

    [Fact]
    public void ZoomLabel_Click_AsksFor100()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        int? asked = null;
        form.ZoomRequested += (_, step) => asked = step;

        // Act
        Field<ToolStripLabel>(form, "ZoomLabel").PerformClick();

        // Assert
        Assert.Equal(0, asked);
    }

    [Fact]
    public void ContextMenu_ArrangeItems_FollowWhatTheyCanDo_AndPassOnWhatsPicked()
    {
        // Arrange
        using SnipEditorForm form = ShownOffScreen();
        var menu = Field<ContextMenuStrip>(form, "SelectionMenu");
        var front = Field<ToolStripMenuItem>(form, "BringToFrontMenuItem");
        var back = Field<ToolStripMenuItem>(form, "SendToBackMenuItem");
        List<string> asked = [];
        form.DuplicateRequested += (_, _) => asked.Add("duplicate");
        form.BringToFrontRequested += (_, _) => asked.Add("front");
        form.SendToBackRequested += (_, _) => asked.Add("back");

        // Act
        form.ShowContextMenu(new Point(10, 10), new SnipContextMenuModel(false, false, true));
        (bool, bool) enabled = (front.Enabled, back.Enabled);

        // A greyed out item does nothing when clicked
        front.PerformClick();
        menu.Close();
        form.ShowContextMenu(new Point(10, 10), new SnipContextMenuModel(false, true, true));
        menu.Close();
        Field<ToolStripMenuItem>(form, "DuplicateMenuItem").PerformClick();
        front.PerformClick();
        back.PerformClick();

        // Assert
        Assert.Equal((false, true), enabled);
        Assert.Equal(["duplicate", "front", "back"], asked);
    }

    [Theory]
    [InlineData(Keys.Control | Keys.D, "duplicate")]
    [InlineData(Keys.Control | Keys.OemCloseBrackets, "front")]
    [InlineData(Keys.Control | Keys.OemOpenBrackets, "back")]
    public void ArrangeKeys_WorkOnlyWithSomethingSelected(Keys key, string expected)
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        List<string> asked = [];
        form.DuplicateRequested += (_, _) => asked.Add("duplicate");
        form.BringToFrontRequested += (_, _) => asked.Add("front");
        form.SendToBackRequested += (_, _) => asked.Add("back");

        // Act: first with nothing selected
        InvokeKeyDown(form, new KeyEventArgs(key));
        form.ShowSelection(new SnipSelectionModel(new Rectangle(1, 1, 5, 5), []));
        InvokeKeyDown(form, new KeyEventArgs(key));

        // Assert
        Assert.Equal([expected], asked);
    }

    [Fact]
    public void ContextMenu_OnlyOffersTheResetWhenThereIsOne_AndPassesOnWhatsPicked()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        form.ShowInTaskbar = false;
        form.Show();
        form.Location = new Point(-5000, 100);
        var menu = Field<ContextMenuStrip>(form, "SelectionMenu");
        var reset = Field<ToolStripMenuItem>(form, "ResetMenuItem");
        var delete = Field<ToolStripMenuItem>(form, "DeleteMenuItem");
        List<string> asked = [];
        form.ResetRequested += (_, _) => asked.Add("reset");
        form.DeleteRequested += (_, _) => asked.Add("delete");

        // Act
        form.ShowContextMenu(new Point(10, 10), new SnipContextMenuModel(false, true, true));
        bool wasShown = menu.Visible;
        bool couldReset = reset.Enabled;
        menu.Close();
        form.ShowContextMenu(new Point(10, 10), new SnipContextMenuModel(true, true, true));
        bool canReset = reset.Enabled;
        menu.Close();
        reset.PerformClick();
        delete.PerformClick();

        // Assert
        Assert.True(wasShown);
        Assert.False(couldReset);
        Assert.True(canReset);
        Assert.Equal(["reset", "delete"], asked);
    }

    private static Bitmap SavedHighlight(SnipEditorForm form, Rectangle bounds)
    {
        string path = Path.Combine(Path.GetTempPath(), "SnipEditorFormTests_" + Guid.NewGuid() + ".png");

        form.ShowAnnotations([new HighlightAnnotationModel(bounds, Color.FromArgb(128, 0, 0, 255))]);
        form.SaveImage(path);

        try
        {
            using var saved = new Bitmap(path);

            return new Bitmap(saved);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void HighlightedArea_FillsItsFrameEvenly_UpToItsStraightEdges()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(200, 200, Color.White));

        // Act
        using Bitmap saved = SavedHighlight(form, new Rectangle(20, 30, 120, 80));

        // Assert: blue over white, as strong on the edges as in the middle, and nothing outside them
        int inside = saved.GetPixel(80, 70).ToArgb();

        Assert.NotEqual(Color.White.ToArgb(), inside);

        foreach (Point edge in new Point[] { new(80, 30), new(80, 109), new(20, 70), new(139, 70) })
        {
            Assert.Equal(inside, saved.GetPixel(edge.X, edge.Y).ToArgb());
        }

        foreach (Point outside in new Point[] { new(80, 29), new(80, 110), new(19, 70), new(140, 70) })
        {
            Assert.Equal(Color.White.ToArgb(), saved.GetPixel(outside.X, outside.Y).ToArgb());
        }
    }

    [Fact]
    public void HighlightedArea_HasRoundedCorners()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(200, 200, Color.White));

        // Act
        using Bitmap saved = SavedHighlight(form, new Rectangle(20, 30, 120, 80));

        // Assert: the very corners are left out, and a little way in it's full again
        int inside = saved.GetPixel(80, 70).ToArgb();

        foreach (Point corner in new Point[] { new(20, 30), new(139, 30), new(20, 109), new(139, 109) })
        {
            Assert.Equal(Color.White.ToArgb(), saved.GetPixel(corner.X, corner.Y).ToArgb());
        }

        Assert.Equal(inside, saved.GetPixel(28, 38).ToArgb());
        Assert.Equal(inside, saved.GetPixel(131, 101).ToArgb());
    }

    [Fact]
    public void HighlightedArea_WithNoRadius_HasSquareCorners()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(200, 200, Color.White));
        string path = SavedImagePath();
        form.ShowAnnotations([new HighlightAnnotationModel(new Rectangle(20, 30, 120, 80), Color.FromArgb(128, 0, 0, 255), 0)]);

        // Act
        form.SaveImage(path);

        // Assert
        using var saved = new Bitmap(path);
        Assert.Equal(saved.GetPixel(80, 70).ToArgb(), saved.GetPixel(20, 30).ToArgb());
        Assert.Equal(saved.GetPixel(80, 70).ToArgb(), saved.GetPixel(139, 109).ToArgb());
    }

    /// <summary>How far in from a rectangle's top left corner its outline crosses the diagonal.</summary>
    private int CornerInset(int radius)
    {
        using var form = new SnipEditorForm(Solid(200, 200, Color.White));
        string path = SavedImagePath();
        form.ShowAnnotations([new RectangleAnnotationModel(new Rectangle(20, 20, 150, 150), Color.Black, 2, radius)]);
        form.SaveImage(path);

        using var saved = new Bitmap(path);

        for (int i = 0; i < 80; i++)
        {
            if (saved.GetPixel(20 + i, 20 + i).R < 128)
            {
                return i;
            }
        }

        return -1;
    }

    [Fact]
    public void Rectangle_CornerRadius_RoundsItsCorners()
    {
        // Act
        int square = CornerInset(0);
        int round = CornerInset(16);
        int rounder = CornerInset(48);

        // Assert: the rounder, the farther in along the diagonal
        Assert.Equal(0, square);
        Assert.True(round > square, $"Radius 16 at {round}");
        Assert.True(rounder > round, $"Radius 48 at {rounder}");
    }

    [Fact]
    public void Rectangle_SmallerThanItsCorners_StillDraws()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        string path = SavedImagePath();
        form.ShowAnnotations([new RectangleAnnotationModel(new Rectangle(10, 10, 20, 6), Color.Black, 2, 48)]);

        // Act
        form.SaveImage(path);

        // Assert
        using var saved = new Bitmap(path);
        Assert.True(saved.GetPixel(20, 10).R < 128);
    }

    [Fact]
    public void CornerRadius_ShowsOnlyForWhatHasCorners_AndPassesOnTheChoice()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        var list = Field<ToolStripComboBox>(form, "CornerRadiusComboBox");
        int? picked = null;
        form.CornerRadiusSelected += (_, radius) => picked = radius;

        // Act & Assert
        form.ShowCornerRadius([0, 12, 48], null);
        Assert.False(list.Available);

        form.ShowCornerRadius([0, 12, 48], 12);
        Assert.True(list.Available);
        Assert.Equal("12 px", list.SelectedItem);
        Assert.Null(picked);

        list.SelectedItem = "Square";
        Assert.Equal(0, picked);
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(200, 3)]
    [InlineData(1, 1)]
    public void HighlightedArea_SmallOrThin_StillDraws(int width, int height)
    {
        // Arrange: smaller than its corners would be
        using var form = new SnipEditorForm(Solid(300, 100, Color.White));

        // Act
        using Bitmap saved = SavedHighlight(form, new Rectangle(10, 10, width, height));

        // Assert
        Assert.NotEqual(Color.White.ToArgb(), saved.GetPixel(10 + width / 2, 10 + height / 2).ToArgb());
    }

    [Fact]
    public void ToolWithNoSizes_HidesTheSizeList_UntilOneHasThem()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        var sizes = Field<ToolStripComboBox>(form, "SizeComboBox");

        // Act & Assert
        form.ShowSizes([], 0);
        Assert.False(sizes.Available);

        form.ShowSizes([1, 2], 2);
        Assert.True(sizes.Available);

        form.ShowDrawingChoices(false);
        form.ShowSizes([1, 2], 2);
        Assert.False(sizes.Available);
    }

    [Fact]
    public void Selection_IsNeverInTheSavedImage()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        string path = SavedImagePath();
        form.ShowSelection(new SnipSelectionModel(new Rectangle(10, 10, 50, 50), [new Point(10, 10)]));

        // Act
        form.SaveImage(path);

        // Assert
        using var saved = new Bitmap(path);
        Assert.Equal(Color.White.ToArgb(), saved.GetPixel(10, 10).ToArgb());
        Assert.Equal(Color.White.ToArgb(), saved.GetPixel(35, 10).ToArgb());
    }

    [Fact]
    public void TextWithAnEdgeOfNoWidth_DrawsNoEdge()
    {
        // Arrange: what a selected text keeps when its edge is taken off
        using var form = new SnipEditorForm(Solid(200, 100, Color.White));
        string path = SavedImagePath();
        form.ShowAnnotations(
        [
            new TextAnnotationModel(new Point(5, 5), "Hello", Color.Black, 36, new SnipTextOutlineModel(0, Color.Lime)),
        ]);

        // Act
        form.SaveImage(path);

        // Assert
        using var saved = new Bitmap(path);

        for (int x = 0; x < saved.Width; x++)
        {
            for (int y = 0; y < saved.Height; y++)
            {
                Color pixel = saved.GetPixel(x, y);

                Assert.True(pixel.G <= pixel.R + 2, $"Edge color at {x}, {y}: {pixel}");
            }
        }
    }

    [Fact]
    public void BeginTextInput_StartsFromTheTextGiven()
    {
        // Arrange
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        TextInputModel? committed = null;
        form.TextCommitted += (_, input) => committed = input;

        // Act
        form.BeginTextInput(new Point(1, 2), Color.Black, 14, "old\nline");
        form.AddSnip(Solid(100, 100, Color.White));

        // Assert: a new snip ends the text
        Assert.Equal("old\r\nline", committed!.Text);
    }

    [Fact]
    public void MeasureText_GrowsWithTheFont_AndTheLines()
    {
        // Act
        using var form = new SnipEditorForm(Solid(100, 100, Color.White));
        Size small = form.MeasureText("Hello", 20);
        Size large = form.MeasureText("Hello", 40);
        Size lines = form.MeasureText("Hello\nHello", 20);

        // Assert
        Assert.True(large.Width > small.Width && large.Height > small.Height);
        Assert.True(lines.Height > small.Height);
        Assert.Equal(small.Width, lines.Width);
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
