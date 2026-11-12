using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;
using CincoVertice.MyTools.Ui.Zones.Models;
using CincoVertice.MyTools.Ui.Zones.Services;
using CincoVertice.MyTools.Ui.Zones.Views;

namespace CincoVertice.MyTools.Ui.Win.Zones;

/// <summary>
///     Window zones editor view: the monitor chosen, drawn at its shape in the middle of the window, with the zones
///     over it as they'll be on the screen, gaps included. <see cref="Ui.Zones.Presenters.ZoneEditorPresenter"/>
///     decides everything; the form turns the mouse into percent of the monitor and draws what it's given.
/// </summary>
public partial class ZoneEditorForm : Form, IZoneEditorView
{
    private const string BaseTitle = "Window zones";

    // Space around the monitor, in pixels of the window
    private const int MonitorMargin = 24;

    // How near a border the mouse grabs it, in pixels of the window
    private const int GrabPixels = 6;

    // How large the handles of the selected zone are drawn, in pixels of the window
    private const int HandleSize = 8;

    private static readonly Color _monitorColor = Color.FromArgb(24, 24, 24);
    private static readonly Color _zoneColor = Color.FromArgb(90, 90, 90);
    private static readonly Color _selectedColor = Color.FromArgb(0, 120, 215);

    private IReadOnlyList<ZoneModel> _zones = [];
    private int? _selected;
    private int _gap;
    private Size _monitor = new(1920, 1040);

    // While the toolbar is filled, its changes are not the user's
    private bool _isUpdating;

    // Closing because the presenter asked, so it isn't asked again
    private bool _isClosingByPresenter;

    public ZoneEditorForm()
    {
        InitializeComponent();
    }

    public event EventHandler<ZoneEditorPointerModel>? PointerPressed;

    public event EventHandler<ZoneEditorPointerModel>? PointerMoved;

    public event EventHandler<ZoneEditorPointerModel>? PointerReleased;

    public event EventHandler<ZoneEditorPointerModel>? PointerHovered;

    public event EventHandler<ZoneEditorPointerModel>? PointerRightClicked;

    public event EventHandler<ZoneLayoutTemplate>? TemplateSelected;

    public event EventHandler<int>? GapSelected;

    public event EventHandler<int>? TargetSelected;

    public event EventHandler? UseDefaultRequested;

    public event EventHandler? SplitVerticallyRequested;

    public event EventHandler? SplitHorizontallyRequested;

    public event EventHandler? DeleteRequested;

    public event EventHandler? UndoRequested;

    public event EventHandler? RedoRequested;

    public event EventHandler? SaveRequested;

    public event EventHandler? EditAsTextRequested;

    public event EventHandler<CancelEventArgs>? CloseRequested;

    public void ShowTargets(IReadOnlyList<string> names, int selected, Size shape)
    {
        _isUpdating = true;
        MonitorComboBox.Items.Clear();

        foreach (string name in names)
        {
            MonitorComboBox.Items.Add(name);
        }

        MonitorComboBox.SelectedIndex = selected;
        _isUpdating = false;

        _monitor = shape;
        Canvas.Invalidate();
    }

    public void ShowOwnLayout(bool isMonitor, bool isOwn)
    {
        OwnLabel.Text = !isMonitor
            ? "For monitors without their own"
            : isOwn ? "Its own layout" : "Uses the default; a change gives it its own";
        UseDefaultButton.Visible = isMonitor;
        UseDefaultButton.Enabled = isOwn;
    }

    public void ShowGaps(IReadOnlyList<int> gaps, int selected)
    {
        _isUpdating = true;
        GapComboBox.Items.Clear();

        foreach (int gap in gaps)
        {
            GapComboBox.Items.Add(new GapItem(gap));
        }

        GapComboBox.SelectedIndex = gaps.ToList().IndexOf(selected);
        _isUpdating = false;
    }

    public void ShowZones(IReadOnlyList<ZoneModel> zones, int? selected, int gap)
    {
        _zones = zones;
        _selected = selected;
        _gap = gap;
        Canvas.Invalidate();
    }

    public void ShowCursor(ZoneEditorCursor cursor)
    {
        Canvas.Cursor = cursor switch
        {
            ZoneEditorCursor.Move => Cursors.SizeAll,
            ZoneEditorCursor.ResizeLeftRight => Cursors.SizeWE,
            ZoneEditorCursor.ResizeUpDown => Cursors.SizeNS,
            ZoneEditorCursor.ResizeDiagonalDown => Cursors.SizeNWSE,
            ZoneEditorCursor.ResizeDiagonalUp => Cursors.SizeNESW,
            _ => Cursors.Cross,
        };
    }

    public void ShowContextMenu(double x, double y)
    {
        ZoneMenu.Show(Canvas, ToCanvas(x, y));
    }

    public void ShowUndoRedo(bool canUndo, bool canRedo)
    {
        UndoButton.Enabled = canUndo;
        RedoButton.Enabled = canRedo;
    }

    public void ShowUnsaved(bool hasUnsaved)
    {
        Text = hasUnsaved ? BaseTitle + " *" : BaseTitle;
    }

    public void ShowProblem(string message)
    {
        MessageBox.Show(this, message, BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public bool? AskSaveChanges()
    {
        DialogResult answer = MessageBox.Show(
            this,
            "Save the changes to the window zones?",
            BaseTitle,
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);

        return answer switch
        {
            DialogResult.Yes => true,
            DialogResult.No => false,
            _ => null,
        };
    }

    public void CloseView()
    {
        _isClosingByPresenter = true;
        Close();
    }

    // The monitor, as large as fits in the canvas at its own shape, centered
    private Rectangle MonitorArea()
    {
        Size room = Canvas.ClientSize - new Size(2 * MonitorMargin, 2 * MonitorMargin);

        if (room.Width <= 0 || room.Height <= 0 || _monitor.Width <= 0 || _monitor.Height <= 0)
        {
            return Rectangle.Empty;
        }

        double scale = Math.Min((double)room.Width / _monitor.Width, (double)room.Height / _monitor.Height);
        var size = new Size((int)(_monitor.Width * scale), (int)(_monitor.Height * scale));

        return new Rectangle(
            new Point((Canvas.ClientSize.Width - size.Width) / 2, (Canvas.ClientSize.Height - size.Height) / 2),
            size);
    }

    private Point ToCanvas(double x, double y)
    {
        Rectangle area = MonitorArea();

        return new Point(
            area.Left + (int)Math.Round(area.Width * x / 100),
            area.Top + (int)Math.Round(area.Height * y / 100));
    }

    private ZoneEditorPointerModel ToPointer(Point point)
    {
        Rectangle area = MonitorArea();
        double width = Math.Max(1, area.Width);
        double height = Math.Max(1, area.Height);

        return new ZoneEditorPointerModel(
            (point.X - area.Left) * 100 / width,
            (point.Y - area.Top) * 100 / height,
            GrabPixels * 100 / width,
            GrabPixels * 100 / height);
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        Rectangle area = MonitorArea();

        if (area.IsEmpty)
        {
            return;
        }

        using var monitorBrush = new SolidBrush(_monitorColor);
        graphics.FillRectangle(monitorBrush, area);

        // The gap as it will look on the monitor, at the scale it's drawn at
        int gap = (int)Math.Round(_gap * (double)area.Width / _monitor.Width);
        IReadOnlyList<Rectangle> zones = ZoneCalculator.Place(new ZoneLayoutModel(_zones, gap), area);

        graphics.SmoothingMode = SmoothingMode.None;
        using var border = new Pen(Color.White);
        using var numberBrush = new SolidBrush(Color.White);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        foreach (int i in ZoneCalculator.DrawingOrder(zones.Count, _selected))
        {
            Rectangle zone = zones[i];

            using var fill = new SolidBrush(Color.FromArgb(200, i == _selected ? _selectedColor : _zoneColor));
            graphics.FillRectangle(fill, zone);
            graphics.DrawRectangle(border, zone.X, zone.Y, zone.Width - 1, zone.Height - 1);

            float size = Math.Clamp(Math.Min(zone.Width, zone.Height) / 3f, 8f, 48f);
            using var font = new Font(Font.FontFamily, size, FontStyle.Bold, GraphicsUnit.Pixel);
            graphics.DrawString((i + 1).ToString(CultureInfo.InvariantCulture), font, numberBrush, zone, format);
        }

        using var monitorBorder = new Pen(Color.Gray);
        graphics.DrawRectangle(monitorBorder, area.X, area.Y, area.Width - 1, area.Height - 1);

        if (_selected is int selected && selected < _zones.Count)
        {
            DrawHandles(graphics, _zones[selected]);
        }
    }

    // At the zone's own corners and the middles of its sides, where the presenter finds them, not the gap's
    private void DrawHandles(Graphics graphics, ZoneModel zone)
    {
        Point topLeft = ToCanvas(zone.Left, zone.Top);
        Point bottomRight = ToCanvas(zone.Right, zone.Bottom);
        int middleX = (topLeft.X + bottomRight.X) / 2;
        int middleY = (topLeft.Y + bottomRight.Y) / 2;
        Point[] handles =
        [
            topLeft, new(middleX, topLeft.Y), new(bottomRight.X, topLeft.Y), new(bottomRight.X, middleY),
            bottomRight, new(middleX, bottomRight.Y), new(topLeft.X, bottomRight.Y), new(topLeft.X, middleY),
        ];

        using var fill = new SolidBrush(Color.White);
        using var border = new Pen(_selectedColor);

        foreach (Point handle in handles)
        {
            var square = new Rectangle(
                handle.X - (HandleSize / 2),
                handle.Y - (HandleSize / 2),
                HandleSize,
                HandleSize);
            graphics.FillRectangle(fill, square);
            graphics.DrawRectangle(border, square);
        }
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        Canvas.Focus();

        if (e.Button == MouseButtons.Left)
        {
            PointerPressed?.Invoke(this, ToPointer(e.Location));
        }
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            PointerMoved?.Invoke(this, ToPointer(e.Location));
        }
        else if (e.Button == MouseButtons.None)
        {
            PointerHovered?.Invoke(this, ToPointer(e.Location));
        }
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            PointerReleased?.Invoke(this, ToPointer(e.Location));
        }
        else if (e.Button == MouseButtons.Right)
        {
            PointerRightClicked?.Invoke(this, ToPointer(e.Location));
        }
    }

    private void TemplateMenuItem_Click(object? sender, EventArgs e)
    {
        if (sender is ToolStripItem { Tag: string tag })
        {
            TemplateSelected?.Invoke(this, Enum.Parse<ZoneLayoutTemplate>(tag));
        }
    }

    private void GapComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isUpdating && GapComboBox.SelectedItem is GapItem item)
        {
            GapSelected?.Invoke(this, item.Pixels);
        }
    }

    private void MonitorComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isUpdating && MonitorComboBox.SelectedIndex >= 0)
        {
            TargetSelected?.Invoke(this, MonitorComboBox.SelectedIndex);
        }
    }

    private void UseDefaultButton_Click(object? sender, EventArgs e) =>
        UseDefaultRequested?.Invoke(this, EventArgs.Empty);

    private void UndoButton_Click(object? sender, EventArgs e) => UndoRequested?.Invoke(this, EventArgs.Empty);

    private void RedoButton_Click(object? sender, EventArgs e) => RedoRequested?.Invoke(this, EventArgs.Empty);

    private void SaveButton_Click(object? sender, EventArgs e) => SaveRequested?.Invoke(this, EventArgs.Empty);

    private void EditAsTextButton_Click(object? sender, EventArgs e) =>
        EditAsTextRequested?.Invoke(this, EventArgs.Empty);

    private void SplitVerticallyMenuItem_Click(object? sender, EventArgs e) =>
        SplitVerticallyRequested?.Invoke(this, EventArgs.Empty);

    private void SplitHorizontallyMenuItem_Click(object? sender, EventArgs e) =>
        SplitHorizontallyRequested?.Invoke(this, EventArgs.Empty);

    private void DeleteMenuItem_Click(object? sender, EventArgs e) => DeleteRequested?.Invoke(this, EventArgs.Empty);

    private void ZoneEditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        Action? command = (e.Modifiers, e.KeyCode) switch
        {
            (Keys.None, Keys.Delete) => () => DeleteRequested?.Invoke(this, EventArgs.Empty),
            (Keys.Control, Keys.Z) => () => UndoRequested?.Invoke(this, EventArgs.Empty),
            (Keys.Control | Keys.Shift, Keys.Z) or (Keys.Control, Keys.Y) =>
                () => RedoRequested?.Invoke(this, EventArgs.Empty),
            (Keys.Control, Keys.S) => () => SaveRequested?.Invoke(this, EventArgs.Empty),
            (Keys.None, Keys.Escape) => Close,
            _ => null,
        };

        if (command is not null)
        {
            command();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void ZoneEditorForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_isClosingByPresenter || e.CloseReason != CloseReason.UserClosing)
        {
            return;
        }

        var args = new CancelEventArgs();
        CloseRequested?.Invoke(this, args);
        e.Cancel = args.Cancel;
    }

    /// <summary>A gap in the list, shown with its unit.</summary>
    private sealed record GapItem(int Pixels)
    {
        public override string ToString() => Pixels == 0 ? "None" : $"{Pixels} px";
    }
}
