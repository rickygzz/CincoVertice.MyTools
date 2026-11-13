using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Snipping.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;

namespace CincoVertice.MyTools.Ui.Win.Snipping;

/// <summary>
///     Snip view: covers every monitor with the frozen screenshot, dimmed, and shows the selection undimmed.
///     <see cref="Ui.Snipping.Presenters.SnipPresenter"/> turns the drag into the selection; the form only shows it.
///     <para>The window must be created per-monitor DPI aware (see SnipRegionAction), so a client pixel is a
///     screenshot pixel on every monitor.</para>
/// </summary>
public partial class SnipOverlayForm : Form, ISnipView
{
    private const int LabelPadding = 4;
    private const int LabelMargin = 6;

    private static readonly Color _borderColor = Color.FromArgb(0, 120, 215);

    private readonly Bitmap _screenshot;
    private readonly Bitmap _dimmed;
    private readonly Pen _borderPen = new(_borderColor);

    private Rectangle? _selection;
    private string _sizeText = string.Empty;
    private Rectangle _labelBounds;

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

    public event EventHandler? CancelRequested;

    public event EventHandler? FocusLost;

    public event EventHandler? ViewClosed;

    public void ShowSelection(Rectangle selection, string sizeText)
    {
        Rectangle oldArea = PaintedArea();

        _selection = selection;
        _sizeText = sizeText;
        _labelBounds = PlaceLabel(selection, TextRenderer.MeasureText(sizeText, Font));

        // Only what changed: repainting every monitor on each mouse move would lag
        Invalidate(oldArea);
        Invalidate(PaintedArea());
    }

    public void ClearSelection()
    {
        Invalidate(PaintedArea());
        _selection = null;
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
        area.Inflate(2, 2);

        return area;
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
    }

    private void SnipOverlayForm_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
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
