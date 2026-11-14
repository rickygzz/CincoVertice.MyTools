using System.Drawing.Drawing2D;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.ColorPicker.Presenters;
using CincoVertice.MyTools.Ui.ColorPicker.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;

namespace CincoVertice.MyTools.Ui.Win.ColorPicker;

/// <summary>
///     Color picker view: covers every monitor with the frozen screenshot, and shows a magnifier by the pointer
///     with the color under it. <see cref="Ui.ColorPicker.Presenters.ColorPickerPresenter"/> decides what it shows.
///     <para>The window must be created per-monitor DPI aware (see ScreenColorPicker), so a client pixel is a
///     screenshot pixel on every monitor.</para>
/// </summary>
public partial class ColorPickerOverlayForm : Form, IColorPickerView
{
    /// <summary>What the keys and buttons do, under the color.</summary>
    public const string HintText = "Click: HEX · Ctrl+click: RGB · Wheel: zoom · Esc";

    private const int LensOffset = 24;
    private const int InfoPadding = 6;
    private const int SwatchSize = 28;

    private readonly Bitmap _screenshot;
    private readonly Font _hintFont;

    private ColorLensModel? _lens;
    private Rectangle _lensBounds;
    private Rectangle _infoBounds;

    /// <param name="screenshot">The screenshot to show; the caller keeps and disposes it.</param>
    /// <param name="screenBounds">Where the screenshot was taken, in screen coordinates.</param>
    public ColorPickerOverlayForm(Bitmap screenshot, Rectangle screenBounds)
    {
        InitializeComponent();

        _screenshot = screenshot;
        _hintFont = new Font(Font.FontFamily, Font.Size * 0.85f);
        Bounds = screenBounds;

        FormExtensions.ActivateWhenShown(this);
    }

    public event EventHandler<Point>? PointerMoved;

    public event EventHandler<int>? ZoomRequested;

    public event EventHandler<Size>? NudgeRequested;

    public event EventHandler<ColorTextFormat>? PickRequested;

    public event EventHandler? CancelRequested;

    public event EventHandler? FocusLost;

    public event EventHandler? ViewClosed;

    public void ShowLens(ColorLensModel lens)
    {
        Rectangle oldArea = PaintedArea();

        _lens = lens;
        PlaceLens(lens);

        // Only what changed: repainting every monitor on each mouse move would lag
        Invalidate(oldArea);
        Invalidate(PaintedArea());
    }

    public void MovePointer(Point pixel)
    {
        Cursor.Position = PointToScreen(pixel);
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

    /// <summary>The arrow keys move the pointer, not the focus.</summary>
    protected override bool IsInputKey(Keys keyData) =>
        (keyData & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);

    protected override void WndProc(ref Message m)
    {
        // The window spans monitors with different scaling; it must keep its bounds and its pixels as they are
        if (m.Msg == (int)WM.WM_DPICHANGED)
        {
            return;
        }

        base.WndProc(ref m);
    }

    /// <summary>
    ///     The magnifier goes below and right of the pointer, with the color under it, or on the other side where
    ///     there's no room.
    /// </summary>
    private void PlaceLens(ColorLensModel lens)
    {
        int side = lens.Cells * lens.Zoom;
        Size hexSize = TextRenderer.MeasureText(lens.Hex, Font);
        Size rgbSize = TextRenderer.MeasureText(lens.Rgb, Font);
        Size hintSize = TextRenderer.MeasureText(HintText, _hintFont);
        int textWidth = Math.Max(Math.Max(hexSize.Width, rgbSize.Width) + SwatchSize + InfoPadding, hintSize.Width);
        int width = Math.Max(side, textWidth + 2 * InfoPadding);
        int infoHeight = Math.Max(SwatchSize, hexSize.Height + rgbSize.Height) + hintSize.Height + 3 * InfoPadding;
        int height = side + infoHeight;

        int x = lens.Pixel.X + LensOffset;
        int y = lens.Pixel.Y + LensOffset;

        if (x + width > ClientSize.Width)
        {
            x = lens.Pixel.X - LensOffset - width;
        }

        if (y + height > ClientSize.Height)
        {
            y = lens.Pixel.Y - LensOffset - height;
        }

        x = Math.Max(0, x);
        y = Math.Max(0, y);

        _lensBounds = new Rectangle(x + (width - side) / 2, y, side, side);
        _infoBounds = new Rectangle(x, y + side, width, infoHeight);
    }

    /// <summary>The part of the window that the magnifier and its color cover.</summary>
    private Rectangle PaintedArea()
    {
        if (_lens is null)
        {
            return Rectangle.Empty;
        }

        Rectangle area = Rectangle.Union(_lensBounds, _infoBounds);
        area.Inflate(3, 3);

        return area;
    }

    private void ColorPickerOverlayForm_Paint(object? sender, PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        Rectangle clip = e.ClipRectangle;

        // GraphicsUnit.Pixel: draw pixel for pixel, never scaled by the bitmap's resolution
        graphics.DrawImage(_screenshot, clip, clip, GraphicsUnit.Pixel);

        if (_lens is ColorLensModel lens)
        {
            DrawLens(graphics, lens);
            DrawInfo(graphics, lens);
        }
    }

    private void DrawLens(Graphics graphics, ColorLensModel lens)
    {
        int half = lens.Cells / 2;
        var source = new Rectangle(lens.Pixel.X - half, lens.Pixel.Y - half, lens.Cells, lens.Cells);

        // Past the screenshot's edges stays black
        graphics.FillRectangle(Brushes.Black, _lensBounds);

        GraphicsState state = graphics.Save();
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.SetClip(_lensBounds);
        graphics.DrawImage(_screenshot, _lensBounds, source, GraphicsUnit.Pixel);
        graphics.Restore(state);

        // The pixel picked: black and white, so it shows on any color
        var middle = new Rectangle(
            _lensBounds.X + half * lens.Zoom,
            _lensBounds.Y + half * lens.Zoom,
            lens.Zoom,
            lens.Zoom);
        graphics.DrawRectangle(Pens.Black, middle.X - 1, middle.Y - 1, middle.Width + 1, middle.Height + 1);
        graphics.DrawRectangle(Pens.White, middle.X - 2, middle.Y - 2, middle.Width + 3, middle.Height + 3);

        graphics.DrawRectangle(Pens.Gray, _lensBounds.X, _lensBounds.Y, _lensBounds.Width - 1, _lensBounds.Height - 1);
    }

    private void DrawInfo(Graphics graphics, ColorLensModel lens)
    {
        using var background = new SolidBrush(Color.FromArgb(32, 32, 32));
        using var swatch = new SolidBrush(lens.Color);

        graphics.FillRectangle(background, _infoBounds);

        var swatchBounds = new Rectangle(
            _infoBounds.X + InfoPadding,
            _infoBounds.Y + InfoPadding,
            SwatchSize,
            SwatchSize);
        graphics.FillRectangle(swatch, swatchBounds);
        graphics.DrawRectangle(Pens.Gray, swatchBounds);

        int textX = swatchBounds.Right + InfoPadding;
        Size hexSize = TextRenderer.MeasureText(lens.Hex, Font);

        TextRenderer.DrawText(graphics, lens.Hex, Font, new Point(textX, swatchBounds.Y), Color.White);
        TextRenderer.DrawText(
            graphics,
            lens.Rgb,
            Font,
            new Point(textX, swatchBounds.Y + hexSize.Height),
            Color.Gainsboro);

        int hintY = Math.Max(swatchBounds.Bottom, swatchBounds.Y + 2 * hexSize.Height) + InfoPadding;
        TextRenderer.DrawText(
            graphics,
            HintText,
            _hintFont,
            new Point(_infoBounds.X + InfoPadding, hintY),
            Color.DarkGray);
    }

    private void ColorPickerOverlayForm_Shown(object? sender, EventArgs e)
    {
        PointerMoved?.Invoke(this, PointToClient(Cursor.Position));
    }

    private void ColorPickerOverlayForm_MouseMove(object? sender, MouseEventArgs e)
    {
        PointerMoved?.Invoke(this, e.Location);
    }

    private void ColorPickerOverlayForm_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            PointerMoved?.Invoke(this, e.Location);
            PickRequested?.Invoke(this, FormatFor(ModifierKeys));
        }
        else if (e.Button == MouseButtons.Right)
        {
            CancelRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ColorPickerOverlayForm_MouseWheel(object? sender, MouseEventArgs e)
    {
        ZoomRequested?.Invoke(this, Math.Sign(e.Delta));
    }

    private void ColorPickerOverlayForm_KeyDown(object? sender, KeyEventArgs e)
    {
        int step = e.Shift ? ColorPickerPresenter.LargeNudge : 1;

        switch (e.KeyCode)
        {
            case Keys.Escape:
                CancelRequested?.Invoke(this, EventArgs.Empty);
                break;
            case Keys.Enter:
                PickRequested?.Invoke(this, FormatFor(e.Modifiers));
                break;
            case Keys.Left:
                NudgeRequested?.Invoke(this, new Size(-step, 0));
                break;
            case Keys.Right:
                NudgeRequested?.Invoke(this, new Size(step, 0));
                break;
            case Keys.Up:
                NudgeRequested?.Invoke(this, new Size(0, -step));
                break;
            case Keys.Down:
                NudgeRequested?.Invoke(this, new Size(0, step));
                break;
            case Keys.Oemplus or Keys.Add:
                ZoomRequested?.Invoke(this, 1);
                break;
            case Keys.OemMinus or Keys.Subtract:
                ZoomRequested?.Invoke(this, -1);
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void ColorPickerOverlayForm_Deactivate(object? sender, EventArgs e)
    {
        FocusLost?.Invoke(this, EventArgs.Empty);
    }

    private void ColorPickerOverlayForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        ViewClosed?.Invoke(this, EventArgs.Empty);
    }

    private static ColorTextFormat FormatFor(Keys modifiers) =>
        modifiers.HasFlag(Keys.Control) ? ColorTextFormat.Rgb : ColorTextFormat.Hex;
}
