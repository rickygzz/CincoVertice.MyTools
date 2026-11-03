using System.ComponentModel;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using CincoVertice.MyTools.Ui.Diagramming.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.Diagramming.Services;

namespace CincoVertice.MyTools.Ui.Win.Diagramming;

/// <summary>
///     Diagram editor view: the canvas, with the shapes on a bar at the left and the style and file commands on a bar
///     at the top. <see cref="Ui.Diagramming.Presenters.DiagramEditorPresenter"/> does the work; the form only shows
///     what it's given and passes on what the user does.
/// </summary>
public partial class DiagramEditorForm : Form, IDiagramEditorView
{
    private const string WindowTitle = "Diagram editor";

    // How many pixels the arrow keys move the selection; with Shift, one
    private const int NudgeStep = 10;

    private readonly int _iconSize;
    private readonly List<Image> _images = [];

    // The color buttons' images: the current colors, replaced when they change
    private readonly Dictionary<ToolStripDropDownButton, Image> _swatches = [];

    private DiagramSceneModel? _scene;
    private IReadOnlyList<int> _lineWidths = [];
    private IReadOnlyList<int> _fontSizes = [];

    // Set while the toolbar is filled from the presenter, so that isn't taken as the user's choice
    private bool _isShowingChoices;

    // True while the text box holds a text the presenter is waiting for
    private bool _isEditingText;

    // The text box's font, made here for the size asked for
    private Font? _textFont;

    // Held with a drag to move the view instead
    private bool _isSpaceDown;

    public DiagramEditorForm()
    {
        InitializeComponent();

        // 16 pixels at 100% scaling, drawn at the screen's size rather than stretched, so they stay sharp
        _iconSize = (int)Math.Round(16 * DeviceDpi / 96.0);
        ToolsStrip.ImageScalingSize = new Size(_iconSize, _iconSize);
        StyleStrip.ImageScalingSize = new Size(_iconSize, _iconSize);

        foreach (ToolStripButton button in ToolButtons())
        {
            button.Image = Keep(DiagramToolIcons.Create(ToolOf(button), _iconSize));
        }

        foreach (ToolStripDropDownButton button in ColorButtons())
        {
            ((ToolStripDropDownMenu)button.DropDown).ShowImageMargin = false;
        }

        RoutingComboBox.Items.AddRange(["Elbow", "Straight"]);
        ArrowHeadsComboBox.Items.AddRange(["Arrow at end", "Arrow at start", "Arrows at both", "No arrows"]);

        FormExtensions.CenterOnCursorScreen(this);
        FormExtensions.ActivateWhenShown(this);
    }

    public event EventHandler<DiagramPointerModel>? PointerPressed;

    public event EventHandler<DiagramPointerModel>? PointerMoved;

    public event EventHandler<DiagramPointerModel>? PointerReleased;

    public event EventHandler<DiagramPointerModel>? PointerDoubleClicked;

    public event EventHandler? PointerLeft;

    public event EventHandler<DiagramWheelModel>? WheelTurned;

    public event EventHandler<DiagramTool>? ToolSelected;

    public event EventHandler<DiagramCommand>? CommandRequested;

    public event EventHandler<Size>? NudgeRequested;

    public event EventHandler<DiagramColorChoiceModel>? ColorSelected;

    public event EventHandler<DiagramColorTarget>? MoreColorsRequested;

    public event EventHandler<int>? LineWidthSelected;

    public event EventHandler<bool>? DashedSelected;

    public event EventHandler<int>? FontSizeSelected;

    public event EventHandler<DiagramRouting>? RoutingSelected;

    public event EventHandler<DiagramArrowHeads>? ArrowHeadsSelected;

    public event EventHandler<string>? TextCommitted;

    event EventHandler<CancelEventArgs>? IDiagramEditorView.Closing
    {
        add => _closing += value;
        remove => _closing -= value;
    }

    private event EventHandler<CancelEventArgs>? _closing;

    public void ShowScene(DiagramSceneModel scene)
    {
        _scene = scene;
        Canvas.Invalidate();
    }

    public void ShowTool(DiagramTool tool)
    {
        foreach (ToolStripButton button in ToolButtons())
        {
            button.Checked = ToolOf(button) == tool;
        }
    }

    public void ShowPalette(IReadOnlyList<Color> colors)
    {
        FillPalette.Colors = colors;
        LinePalette.Colors = colors;
        TextPalette.Colors = colors;
    }

    public void ShowSizes(IReadOnlyList<int> lineWidths, IReadOnlyList<int> fontSizes)
    {
        _lineWidths = lineWidths;
        _fontSizes = fontSizes;
        _isShowingChoices = true;
        LineWidthComboBox.Items.AddRange([.. lineWidths.Select(width => $"{width} px")]);
        FontSizeComboBox.Items.AddRange([.. fontSizes.Select(size => $"{size} px")]);
        _isShowingChoices = false;
    }

    public void ShowStyle(DiagramStyleModel style, DiagramRouting routing, DiagramArrowHeads heads)
    {
        _isShowingChoices = true;

        FillPalette.SelectedColor = style.Fill;
        LinePalette.SelectedColor = style.Line;
        TextPalette.SelectedColor = style.TextColor;
        ShowSwatch(FillDropDownButton, style.Fill);
        ShowSwatch(LineDropDownButton, style.Line);
        ShowSwatch(TextColorDropDownButton, style.TextColor);

        LineWidthComboBox.SelectedIndex = _lineWidths.ToList().IndexOf(style.LineWidth);
        FontSizeComboBox.SelectedIndex = _fontSizes.ToList().IndexOf(style.FontSize);
        DashedButton.Checked = style.Dashed;
        RoutingComboBox.SelectedIndex = routing == DiagramRouting.Elbow ? 0 : 1;
        ArrowHeadsComboBox.SelectedIndex = (int)heads;

        _isShowingChoices = false;
    }

    public void ShowUndoRedo(bool canUndo, bool canRedo)
    {
        UndoButton.Enabled = canUndo;
        RedoButton.Enabled = canRedo;
    }

    public void ShowDocument(string name, bool hasChanges)
    {
        Text = $"{name}{(hasChanges ? "*" : string.Empty)} - {WindowTitle}";
    }

    public void ShowZoom(int percent)
    {
        ZoomLabel.Text = $"{percent}%";
    }

    public void ShowCursor(DiagramCursor cursor)
    {
        Canvas.Cursor = cursor switch
        {
            DiagramCursor.Cross => Cursors.Cross,
            DiagramCursor.Move => Cursors.SizeAll,
            DiagramCursor.Pan => Cursors.Hand,
            DiagramCursor.SizeVertical => Cursors.SizeNS,
            DiagramCursor.SizeHorizontal => Cursors.SizeWE,
            DiagramCursor.SizeTopLeftBottomRight => Cursors.SizeNWSE,
            DiagramCursor.SizeTopRightBottomLeft => Cursors.SizeNESW,
            _ => Cursors.Default,
        };
    }

    public void BeginTextEdit(Rectangle bounds, string text, int fontSize, Color color)
    {
        Font? oldFont = _textFont;

        _textFont = new Font(DiagramSvg.FontFamily, fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        TextInputBox.Font = _textFont;
        oldFont?.Dispose();
        TextInputBox.ForeColor = color;

        // A light text on a light box couldn't be read while typing
        TextInputBox.BackColor = color.GetBrightness() > 0.7f ? Color.FromArgb(64, 64, 64) : Color.White;
        TextInputBox.Text = text.ReplaceLineEndings(Environment.NewLine);

        // A small shape still gets a box large enough to type in
        TextInputBox.Bounds = new Rectangle(
            bounds.Location,
            new Size(Math.Max(bounds.Width, fontSize * 6), Math.Max(bounds.Height, fontSize * 2)));
        TextInputBox.Visible = true;
        TextInputBox.SelectAll();
        TextInputBox.Focus();
        _isEditingText = true;
    }

    public Color? AskColor(Color current)
    {
        using var dialog = new ColorDialog { AnyColor = true, Color = current, FullOpen = true };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.Color : null;
    }

    public string? AskSavePath(string suggestedFileName)
    {
        using var dialog = new SaveFileDialog
        {
            DefaultExt = "svg",
            FileName = suggestedFileName,
            Filter = "SVG image (*.svg)|*.svg",
            Title = "Save diagram",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public string? AskOpenPath()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "SVG image (*.svg)|*.svg",
            Title = "Open diagram",
        };

        return dialog.ShowDialog(this) == DialogResult.OK ? dialog.FileName : null;
    }

    public DiagramSaveChoice AskSaveChanges(string name)
    {
        DialogResult answer = MessageBox.Show(
            this,
            $"Save the changes to {name}?",
            WindowTitle,
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        return answer switch
        {
            DialogResult.Yes => DiagramSaveChoice.Save,
            DialogResult.No => DiagramSaveChoice.Discard,
            _ => DiagramSaveChoice.Cancel,
        };
    }

    public void ShowError(string message)
    {
        MessageBox.Show(this, message, WindowTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public void SetClipboardText(string text)
    {
        Clipboard.SetText(text);
    }

    public string? GetClipboardText() => Clipboard.ContainsText() ? Clipboard.GetText() : null;

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // An open list closes itself on Esc
        if (keyData != Keys.Escape
            || LineWidthComboBox.DroppedDown
            || FontSizeComboBox.DroppedDown
            || RoutingComboBox.DroppedDown
            || ArrowHeadsComboBox.DroppedDown)
        {
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Esc never reaches the text box's KeyDown: it is a dialog key. While typing, it only drops the text
        if (_isEditingText)
        {
            CancelText();
        }
        else
        {
            CommandRequested?.Invoke(this, DiagramCommand.Cancel);
        }

        return true;
    }

    private static DiagramTool ToolOf(ToolStripButton button) => Enum.Parse<DiagramTool>((string)button.Tag!);

    private static DiagramCommand CommandOf(object? button) =>
        Enum.Parse<DiagramCommand>((string)((ToolStripItem)button!).Tag!);

    private static Bitmap CreateSwatch(Color color, int size)
    {
        var swatch = new Bitmap(size, size);
        int side = size - 2;

        using var graphics = Graphics.FromImage(swatch);

        if (color.IsEmpty)
        {
            // No fill: a red line through an empty square
            graphics.FillRectangle(Brushes.White, 1, 1, side, side);
            graphics.DrawLine(Pens.Red, 1, side, side, 1);
        }
        else
        {
            using var brush = new SolidBrush(color);

            graphics.FillRectangle(brush, 1, 1, side, side);
        }

        graphics.DrawRectangle(Pens.Gray, 1, 1, side - 1, side - 1);

        return swatch;
    }

    private ToolStripButton[] ToolButtons() =>
    [
        SelectButton,
        ConnectorButton,
        RectangleButton,
        RoundedRectangleButton,
        EllipseButton,
        DiamondButton,
        ParallelogramButton,
        HexagonButton,
        CylinderButton,
        DocumentButton,
        TextButton,
    ];

    private ToolStripDropDownButton[] ColorButtons() =>
        [FillDropDownButton, LineDropDownButton, TextColorDropDownButton];

    private Image Keep(Image image)
    {
        _images.Add(image);

        return image;
    }

    private void ShowSwatch(ToolStripDropDownButton button, Color color)
    {
        _swatches.TryGetValue(button, out Image? old);

        Image swatch = CreateSwatch(color, _iconSize);

        _swatches[button] = swatch;
        button.Image = swatch;
        old?.Dispose();
    }

    private DiagramPointerModel PointerOf(MouseEventArgs e) => new(
        e.Location,
        (ModifierKeys & Keys.Shift) != 0,
        (ModifierKeys & Keys.Control) != 0,
        (ModifierKeys & Keys.Alt) != 0,
        e.Button == MouseButtons.Middle || _isSpaceDown);

    private void CommitText()
    {
        if (!_isEditingText)
        {
            return;
        }

        // Cleared first: hiding the focused box raises Leave, which commits again
        _isEditingText = false;
        string text = TextInputBox.Text;
        TextInputBox.Visible = false;
        Canvas.Focus();

        TextCommitted?.Invoke(this, text);
    }

    private void CancelText()
    {
        _isEditingText = false;
        TextInputBox.Visible = false;
        Canvas.Focus();
    }

    private void ToolButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        ToolSelected?.Invoke(this, ToolOf((ToolStripButton)sender!));
        Canvas.Focus();
    }

    private void CommandButton_Click(object? sender, EventArgs e)
    {
        CommitText();
        CommandRequested?.Invoke(this, CommandOf(sender));
        Canvas.Focus();
    }

    private void FillPalette_ColorPicked(object? sender, Color color)
    {
        FillDropDownButton.HideDropDown();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.Fill, color));
    }

    private void LinePalette_ColorPicked(object? sender, Color color)
    {
        LineDropDownButton.HideDropDown();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.Line, color));
    }

    private void TextPalette_ColorPicked(object? sender, Color color)
    {
        TextColorDropDownButton.HideDropDown();
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.Text, color));
    }

    private void NoFillMenuItem_Click(object? sender, EventArgs e)
    {
        ColorSelected?.Invoke(this, new DiagramColorChoiceModel(DiagramColorTarget.Fill, Color.Empty));
    }

    private void MoreFillColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, DiagramColorTarget.Fill);
    }

    private void MoreLineColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, DiagramColorTarget.Line);
    }

    private void MoreTextColorsMenuItem_Click(object? sender, EventArgs e)
    {
        MoreColorsRequested?.Invoke(this, DiagramColorTarget.Text);
    }

    private void LineWidthComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && LineWidthComboBox.SelectedIndex >= 0)
        {
            LineWidthSelected?.Invoke(this, _lineWidths[LineWidthComboBox.SelectedIndex]);
            Canvas.Focus();
        }
    }

    private void FontSizeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && FontSizeComboBox.SelectedIndex >= 0)
        {
            FontSizeSelected?.Invoke(this, _fontSizes[FontSizeComboBox.SelectedIndex]);
            Canvas.Focus();
        }
    }

    private void RoutingComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && RoutingComboBox.SelectedIndex >= 0)
        {
            RoutingSelected?.Invoke(
                this,
                RoutingComboBox.SelectedIndex == 0 ? DiagramRouting.Elbow : DiagramRouting.Straight);
            Canvas.Focus();
        }
    }

    private void ArrowHeadsComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isShowingChoices && ArrowHeadsComboBox.SelectedIndex >= 0)
        {
            ArrowHeadsSelected?.Invoke(this, (DiagramArrowHeads)ArrowHeadsComboBox.SelectedIndex);
            Canvas.Focus();
        }
    }

    private void DashedButton_Click(object? sender, EventArgs e)
    {
        DashedSelected?.Invoke(this, !DashedButton.Checked);
        Canvas.Focus();
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        if (_scene is not null)
        {
            DiagramRenderer.Draw(e.Graphics, _scene, Canvas.ClientSize);
        }
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        // Clicking the canvas doesn't take the focus from the text box by itself
        CommitText();
        Canvas.Focus();

        if (e.Button is MouseButtons.Left or MouseButtons.Middle)
        {
            PointerPressed?.Invoke(this, PointerOf(e));
        }
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        PointerMoved?.Invoke(this, PointerOf(e));
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button is MouseButtons.Left or MouseButtons.Middle)
        {
            PointerReleased?.Invoke(this, PointerOf(e));
        }
    }

    private void Canvas_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            PointerDoubleClicked?.Invoke(this, PointerOf(e));
        }
    }

    private void Canvas_MouseLeave(object? sender, EventArgs e)
    {
        PointerLeft?.Invoke(this, EventArgs.Empty);
    }

    private void Canvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        WheelTurned?.Invoke(
            this,
            new DiagramWheelModel(
                e.Location,
                e.Delta / (float)SystemInformation.MouseWheelScrollDelta,
                (ModifierKeys & Keys.Control) != 0,
                (ModifierKeys & Keys.Shift) != 0));
    }

    private void TextInputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        // Enter ends the text; Shift+Enter starts a new line
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            CommitText();
        }
    }

    private void TextInputBox_Leave(object? sender, EventArgs e)
    {
        CommitText();
    }

    private void DiagramEditorForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        CommitText();

        var closing = new CancelEventArgs();

        _closing?.Invoke(this, closing);
        e.Cancel = closing.Cancel;
    }

    private void DiagramEditorForm_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space)
        {
            _isSpaceDown = false;
        }
    }

    private void DiagramEditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        // While typing a text, keys are the text's
        if (_isEditingText)
        {
            return;
        }

        if (e.KeyCode == Keys.Space)
        {
            _isSpaceDown = true;
        }

        Action? action = KeyAction(e);

        if (action is null)
        {
            return;
        }

        action();
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private Action? KeyAction(KeyEventArgs e)
    {
        int step = e.Shift ? 1 : NudgeStep;

        if (e.KeyCode is Keys.Left or Keys.Right or Keys.Up or Keys.Down && !e.Control && !e.Alt)
        {
            Size delta = e.KeyCode switch
            {
                Keys.Left => new Size(-step, 0),
                Keys.Right => new Size(step, 0),
                Keys.Up => new Size(0, -step),
                _ => new Size(0, step),
            };

            return () => NudgeRequested?.Invoke(this, delta);
        }

        DiagramCommand? command = (e.Modifiers, e.KeyCode) switch
        {
            (Keys.Control, Keys.N) => DiagramCommand.New,
            (Keys.Control, Keys.O) => DiagramCommand.Open,
            (Keys.Control, Keys.S) => DiagramCommand.Save,
            (Keys.Control | Keys.Shift, Keys.S) => DiagramCommand.SaveAs,
            (Keys.Control, Keys.Z) => DiagramCommand.Undo,
            (Keys.Control, Keys.Y) or (Keys.Control | Keys.Shift, Keys.Z) => DiagramCommand.Redo,
            (Keys.Control, Keys.X) => DiagramCommand.Cut,
            (Keys.Control, Keys.C) => DiagramCommand.Copy,
            (Keys.Control, Keys.V) => DiagramCommand.Paste,
            (Keys.Control, Keys.D) => DiagramCommand.Duplicate,
            (Keys.Control, Keys.A) => DiagramCommand.SelectAll,
            (Keys.Control, Keys.G) => DiagramCommand.Group,
            (Keys.Control | Keys.Shift, Keys.G) => DiagramCommand.Ungroup,
            (Keys.Control, Keys.OemCloseBrackets) => DiagramCommand.BringToFront,
            (Keys.Control, Keys.OemOpenBrackets) => DiagramCommand.SendToBack,
            (Keys.Control, Keys.Oemplus or Keys.Add) => DiagramCommand.ZoomIn,
            (Keys.Control, Keys.OemMinus or Keys.Subtract) => DiagramCommand.ZoomOut,
            (Keys.Control, Keys.D0 or Keys.NumPad0) => DiagramCommand.ZoomReset,
            (Keys.None, Keys.Delete or Keys.Back) => DiagramCommand.Delete,
            _ => null,
        };

        if (command is DiagramCommand requested)
        {
            return () => CommandRequested?.Invoke(this, requested);
        }

        DiagramTool? tool = (e.Modifiers, e.KeyCode) switch
        {
            (Keys.None, Keys.V) => DiagramTool.Select,
            (Keys.None, Keys.C) => DiagramTool.Connector,
            (Keys.None, Keys.R) => DiagramTool.Rectangle,
            (Keys.None, Keys.U) => DiagramTool.RoundedRectangle,
            (Keys.None, Keys.O) => DiagramTool.Ellipse,
            (Keys.None, Keys.D) => DiagramTool.Diamond,
            (Keys.None, Keys.P) => DiagramTool.Parallelogram,
            (Keys.None, Keys.H) => DiagramTool.Hexagon,
            (Keys.None, Keys.Y) => DiagramTool.Cylinder,
            (Keys.None, Keys.M) => DiagramTool.Document,
            (Keys.None, Keys.T) => DiagramTool.Text,
            _ => null,
        };

        return tool is DiagramTool selected ? () => ToolSelected?.Invoke(this, selected) : null;
    }
}
