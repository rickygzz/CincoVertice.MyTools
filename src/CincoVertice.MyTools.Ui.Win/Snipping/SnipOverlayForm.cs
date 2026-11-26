using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;

namespace CincoVertice.MyTools.Ui.Win.Snipping;

/// <summary>
///     Snip view: covers every monitor with the frozen screenshot, dimmed, and shows the selection undimmed.
///     <see cref="Ui.Snipping.Presenters.SnipPresenter"/> turns the drag into the selection; the form only shows it, with
///     its handles when it can be adjusted, and says where the pointer is and when Enter is pressed.
///     <para>The window must be created per-monitor DPI aware (see SnipRegionAction), so a client pixel is a
///     screenshot pixel on every monitor.</para>
/// </summary>
public partial class SnipOverlayForm : Form, ISnipView
{
    private const int LabelPadding = 4;
    private const int LabelMargin = 6;

    // The squares drawn at an adjustable selection's handles, in screen pixels
    private const int HandleSize = 8;

    // The round Snip and Cancel buttons by an adjustable selection, at 100%; apart from the handles, which reach 10
    private const int ButtonSize = 28;
    private const int ButtonGap = 6;
    private const int ButtonMargin = 12;

    private static readonly Color _borderColor = Color.FromArgb(0, 120, 215);
    private static readonly Color _confirmColor = Color.FromArgb(16, 124, 16);
    private static readonly Color _confirmHoverColor = Color.FromArgb(34, 160, 34);
    private static readonly Color _cancelColor = Color.FromArgb(220, 48, 48, 48);
    private static readonly Color _cancelHoverColor = Color.FromArgb(196, 43, 28);

    private readonly Bitmap _screenshot;
    private readonly Bitmap _dimmed;
    private readonly Pen _borderPen = new(_borderColor);

    private Rectangle? _selection;
    private string _sizeText = string.Empty;
    private Rectangle _labelBounds;
    private IReadOnlyList<Point> _handles = [];

    // The presenter's pointer, shown again once the pointer leaves a button; and the button under it
    private Cursor _cursor = Cursors.Cross;
    private Rectangle _hovered;

    /// <param name="screenshot">The screenshot to show; the caller keeps and disposes it.</param>
    /// <param name="screenBounds">Where the screenshot was taken, in screen coordinates.</param>
    public SnipOverlayForm(Bitmap screenshot, Rectangle screenBounds)
    {
        InitializeComponent();

        _screenshot = screenshot;
        _dimmed = CreateDimmed(screenshot);
        Bounds = screenBounds;

        FormExtensions.ActivateWhenShown(this);
    }

    public event EventHandler<Point>? SelectionStarted;

    public event EventHandler<Point>? SelectionMoved;

    public event EventHandler<Point>? SelectionEnded;

    public event EventHandler<Point>? PointerMoved;

    public event EventHandler? ConfirmRequested;

    public event EventHandler? CancelRequested;

    public event EventHandler? FocusLost;

    public event EventHandler? ViewClosed;

    public void ShowSelection(Rectangle selection, string sizeText, IReadOnlyList<Point> handles)
    {
        Rectangle oldArea = PaintedArea();

        _selection = selection;
        _sizeText = sizeText;
        _handles = handles;
        _labelBounds = PlaceLabel(selection, TextRenderer.MeasureText(sizeText, Font));

        // Only what changed: repainting every monitor on each mouse move would lag
        Invalidate(oldArea);
        Invalidate(PaintedArea());
    }

    public void ClearSelection()
    {
        Invalidate(PaintedArea());
        _selection = null;
        _handles = [];
    }

    /// <summary>The green button that takes the snip, as Enter does; empty when the selection can't be adjusted.</summary>
    internal Rectangle ConfirmButtonBounds => Buttons().Confirm;

    /// <summary>The button that gives up, as Esc does; empty when the selection can't be adjusted.</summary>
    internal Rectangle CancelButtonBounds => Buttons().Cancel;

    public void ShowCursor(SnipCursor cursor)
    {
        _cursor = cursor switch
        {
            SnipCursor.Move => Cursors.SizeAll,
            SnipCursor.SizeVertical => Cursors.SizeNS,
            SnipCursor.SizeHorizontal => Cursors.SizeWE,
            SnipCursor.SizeTopLeftBottomRight => Cursors.SizeNWSE,
            SnipCursor.SizeTopRightBottomLeft => Cursors.SizeNESW,
            _ => Cursors.Cross,
        };

        if (_hovered.IsEmpty)
        {
            Cursor = _cursor;
        }
    }

    public void TakeFocus()
    {
        // Later: this comes from losing the focus, and Windows is still in the middle of changing it
        BeginInvoke(() =>
        {
            if (!IsDisposed)
            {
                WindowHelper.ForceForeground(Handle);
                Activate();
            }
        });
    }

    protected override void WndProc(ref Message m)
    {
        // The window spans monitors with different scaling; it must keep its bounds and its pixels as they are
        if (m.Msg == (int)WM.WM_DPICHANGED)
        {
            return;
        }

        base.WndProc(ref m);
    }

    private static Bitmap CreateDimmed(Bitmap screenshot)
    {
        var dimmed = new Bitmap(screenshot.Width, screenshot.Height, PixelFormat.Format32bppPArgb);

        using var graphics = Graphics.FromImage(dimmed);
        using var shade = new SolidBrush(Color.FromArgb(120, Color.Black));
        var whole = new Rectangle(Point.Empty, screenshot.Size);

        graphics.DrawImage(screenshot, whole, whole, GraphicsUnit.Pixel);
        graphics.FillRectangle(shade, whole);

        return dimmed;
    }

    /// <summary>
    ///     The size label goes above the selection's top left corner, or inside it when there's no room above.
    /// </summary>
    private Rectangle PlaceLabel(Rectangle selection, Size textSize)
    {
        var size = new Size(textSize.Width + 2 * LabelPadding, textSize.Height + 2 * LabelPadding);
        int y = selection.Top - size.Height - LabelMargin;

        if (y < 0)
        {
            y = selection.Top + LabelMargin;
        }

        int x = Math.Min(selection.Left, _screenshot.Width - size.Width);

        return new Rectangle(new Point(Math.Max(0, x), y), size);
    }

    /// <summary>The part of the window that the selection and its label cover.</summary>
    private Rectangle PaintedArea()
    {
        if (_selection is not Rectangle selection)
        {
            return Rectangle.Empty;
        }

        Rectangle area = Rectangle.Union(selection, _labelBounds);
        (Rectangle confirm, Rectangle cancel) = Buttons();

        if (!confirm.IsEmpty)
        {
            area = Rectangle.Union(area, Rectangle.Union(confirm, cancel));
        }

        area.Inflate(2 + HandleSize, 2 + HandleSize);

        return area;
    }

    /// <summary>
    ///     With handles, the Snip button below the selection's bottom right corner and Cancel left of it, or inside the
    ///     selection when there's no room below; none without handles, when letting go takes the snip.
    /// </summary>
    private (Rectangle Confirm, Rectangle Cancel) Buttons()
    {
        if (_selection is not Rectangle selection || _handles.Count == 0)
        {
            return (Rectangle.Empty, Rectangle.Empty);
        }

        int size = LogicalToDeviceUnits(ButtonSize);
        int gap = LogicalToDeviceUnits(ButtonGap);
        int margin = LogicalToDeviceUnits(ButtonMargin);
        int y = selection.Bottom + margin;

        if (y + size > _screenshot.Height)
        {
            y = Math.Max(0, selection.Bottom - margin - size);
        }

        // Both on the screenshot, also for a selection at its left edge
        int x = Math.Clamp(selection.Right - size, size + gap, Math.Max(size + gap, _screenshot.Width - size));

        return (new Rectangle(x, y, size, size), new Rectangle(x - size - gap, y, size, size));
    }

    /// <summary>A round button: a check mark on green, or a cross; brighter under the pointer.</summary>
    private void DrawButton(Graphics graphics, Rectangle bounds, bool isConfirm)
    {
        bool isHovered = bounds == _hovered;
        Color fill = isConfirm
            ? isHovered ? _confirmHoverColor : _confirmColor
            : isHovered ? _cancelHoverColor : _cancelColor;

        using var brush = new SolidBrush(fill);
        using var ring = new Pen(Color.FromArgb(200, Color.White), Math.Max(1, bounds.Width / 14f));
        using var mark = new Pen(Color.White, Math.Max(2, bounds.Width / 9f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round,
        };

        graphics.FillEllipse(brush, bounds);
        graphics.DrawEllipse(ring, bounds);

        float left = bounds.X;
        float top = bounds.Y;
        float side = bounds.Width;

        if (isConfirm)
        {
            graphics.DrawLines(
                mark,
                [
                    new PointF(left + side * 0.28f, top + side * 0.52f),
                    new PointF(left + side * 0.44f, top + side * 0.68f),
                    new PointF(left + side * 0.73f, top + side * 0.36f),
                ]);
        }
        else
        {
            graphics.DrawLine(mark, left + side * 0.34f, top + side * 0.34f, left + side * 0.66f, top + side * 0.66f);
            graphics.DrawLine(mark, left + side * 0.66f, top + side * 0.34f, left + side * 0.34f, top + side * 0.66f);
        }
    }

    /// <summary>The button under the point, brightened, with the hand pointer; the presenter's pointer elsewhere.</summary>
    private void HoverButton(Point point)
    {
        (Rectangle confirm, Rectangle cancel) = Buttons();
        Rectangle hovered = confirm.Contains(point) ? confirm : cancel.Contains(point) ? cancel : Rectangle.Empty;

        if (hovered == _hovered)
        {
            return;
        }

        Invalidate(_hovered);
        Invalidate(hovered);
        _hovered = hovered;
        Cursor = hovered.IsEmpty ? _cursor : Cursors.Hand;
    }

    private void SnipOverlayForm_Paint(object? sender, PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        Rectangle clip = e.ClipRectangle;

        // GraphicsUnit.Pixel: draw pixel for pixel, never scaled by the bitmap's resolution
        graphics.DrawImage(_dimmed, clip, clip, GraphicsUnit.Pixel);

        if (_selection is not Rectangle selection || selection.Width == 0 || selection.Height == 0)
        {
            return;
        }

        graphics.DrawImage(_screenshot, selection, selection, GraphicsUnit.Pixel);
        graphics.DrawRectangle(_borderPen, selection.X, selection.Y, selection.Width - 1, selection.Height - 1);

        foreach (Point handle in _handles)
        {
            var square = new Rectangle(handle.X - HandleSize / 2, handle.Y - HandleSize / 2, HandleSize, HandleSize);

            graphics.FillRectangle(Brushes.White, square);
            graphics.DrawRectangle(_borderPen, square.X, square.Y, square.Width - 1, square.Height - 1);
        }

        using var labelPath = new GraphicsPath();
        using var labelBrush = new SolidBrush(Color.FromArgb(220, 32, 32, 32));

        labelPath.AddRoundedRectangle(_labelBounds, new Size(8, 8));
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.FillPath(labelBrush, labelPath);
        TextRenderer.DrawText(
            graphics,
            _sizeText,
            Font,
            _labelBounds,
            Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        (Rectangle confirm, Rectangle cancel) = Buttons();

        if (!confirm.IsEmpty)
        {
            DrawButton(graphics, cancel, isConfirm: false);
            DrawButton(graphics, confirm, isConfirm: true);
        }
    }

    private void SnipOverlayForm_MouseDown(object? sender, MouseEventArgs e)
    {
        (Rectangle confirm, Rectangle cancel) = Buttons();

        if (e.Button == MouseButtons.Left && confirm.Contains(e.Location))
        {
            ConfirmRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (e.Button == MouseButtons.Left && cancel.Contains(e.Location))
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (e.Button == MouseButtons.Left)
        {
            SelectionStarted?.Invoke(this, e.Location);
        }
        else if (e.Button == MouseButtons.Right)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SnipOverlayForm_MouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            SelectionMoved?.Invoke(this, e.Location);
        }
        else if (e.Button == MouseButtons.None)
        {
            PointerMoved?.Invoke(this, e.Location);
            HoverButton(e.Location);
        }
    }

    private void SnipOverlayForm_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            SelectionEnded?.Invoke(this, e.Location);
        }
    }

    private void SnipOverlayForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (e.KeyCode == Keys.Enter)
        {
            ConfirmRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SnipOverlayForm_Deactivate(object? sender, EventArgs e)
    {
        FocusLost?.Invoke(this, EventArgs.Empty);
    }

    private void SnipOverlayForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        ViewClosed?.Invoke(this, EventArgs.Empty);
    }
}
