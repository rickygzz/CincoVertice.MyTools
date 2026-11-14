using System.ComponentModel;

namespace CincoVertice.MyTools.Ui.Win.ColorPicker;

/// <summary>A square of one color to click on; the one chosen has a black and white frame, seen on any color.</summary>
public sealed class ColorSwatchControl : Control
{
    private Color _color = Color.Gray;
    private bool _isChosen;
    private bool _isBase;

    public ColorSwatchControl()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
            | ControlStyles.ResizeRedraw,
            true);
        Cursor = Cursors.Hand;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Color
    {
        get => _color;
        set
        {
            _color = value;
            Invalidate();
        }
    }

    /// <summary>Framed: its values are the ones shown.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsChosen
    {
        get => _isChosen;
        set
        {
            _isChosen = value;
            Invalidate();
        }
    }

    /// <summary>Marked with a dot: the color the others are made from.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsBase
    {
        get => _isBase;
        set
        {
            _isBase = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        var whole = new Rectangle(Point.Empty, ClientSize);

        using (var fill = new SolidBrush(_color))
        {
            graphics.FillRectangle(fill, whole);
        }

        if (_isChosen)
        {
            using var black = new Pen(Color.Black, 2);
            using var white = new Pen(Color.White, 2);
            graphics.DrawRectangle(black, 1, 1, whole.Width - 2, whole.Height - 2);
            graphics.DrawRectangle(white, 3, 3, whole.Width - 6, whole.Height - 6);
        }
        else
        {
            graphics.DrawRectangle(SystemPens.ControlDark, 0, 0, whole.Width - 1, whole.Height - 1);
        }

        if (_isBase)
        {
            // Black or white, whichever shows on the color
            Brush dot = _color.GetBrightness() > 0.55f ? Brushes.Black : Brushes.White;
            int size = Math.Max(6, whole.Height / 6);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.FillEllipse(dot, whole.Right - size - 7, whole.Bottom - size - 7, size, size);
        }
    }
}
