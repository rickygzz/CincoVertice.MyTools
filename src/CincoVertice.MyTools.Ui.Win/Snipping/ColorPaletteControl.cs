using System.ComponentModel;

namespace CincoVertice.MyTools.Ui.Win.Snipping;

/// <summary>
///     A grid of color squares, 8 to a row: the fixed colors, and below a line the custom ones, if any. The selected
///     color has a dark frame, the one under the mouse a light one; clicking a square raises
///     <see cref="ColorPicked"/>.
/// </summary>
public sealed class ColorPaletteControl : Control
{
    private const int Columns = 8;
    private const int Inset = 4;

    private IReadOnlyList<Color> _colors = [];
    private IReadOnlyList<Color> _customColors = [];
    private Color _selectedColor = Color.Empty;
    private int _hoveredIndex = -1;

    // Every square with its color, fixed colors first: rebuilt when the colors change
    private List<(Color Color, Rectangle Bounds)> _cells = [];

    public ColorPaletteControl()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
    }

    public event EventHandler<Color>? ColorPicked;

    /// <summary>The fixed colors, in rows of 8.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<Color> Colors
    {
        get => _colors;
        set
        {
            _colors = value;
            LayOut();
        }
    }

    /// <summary>The colors picked with More colors, in a row of their own below the fixed ones.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<Color> CustomColors
    {
        get => _customColors;
        set
        {
            _customColors = value;
            LayOut();
        }
    }

    /// <summary>The color with the dark frame; one that isn't shown frames none.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color SelectedColor
    {
        get => _selectedColor;
        set
        {
            _selectedColor = value;
            Invalidate();
        }
    }

    // 22 pixels at 100% scaling
    private int CellSize => LogicalToDeviceUnits(22);

    // Between the fixed colors and the custom ones, with a line in the middle
    private int SectionGap => LogicalToDeviceUnits(9);

    public override Size GetPreferredSize(Size proposedSize)
    {
        int bottom = _cells.Count == 0 ? 0 : _cells.Max(cell => cell.Bounds.Bottom);

        return new Size(Columns * CellSize + 2 * Inset, bottom + Inset);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (_colors.Count > 0 && _customColors.Count > 0)
        {
            int lineY = _cells[_colors.Count].Bounds.Top - SectionGap / 2;
            e.Graphics.DrawLine(Pens.LightGray, Inset, lineY, Width - Inset, lineY);
        }

        for (int i = 0; i < _cells.Count; i++)
        {
            (Color color, Rectangle cell) = _cells[i];
            Rectangle square = Rectangle.Inflate(cell, -3, -3);
            using var brush = new SolidBrush(color);

            e.Graphics.FillRectangle(brush, square);
            e.Graphics.DrawRectangle(Pens.Gray, square.X, square.Y, square.Width - 1, square.Height - 1);

            if (color.ToArgb() == _selectedColor.ToArgb())
            {
                using var frame = new Pen(Color.FromArgb(40, 40, 40), 2);
                e.Graphics.DrawRectangle(frame, cell.X + 1, cell.Y + 1, cell.Width - 2, cell.Height - 2);
            }
            else if (i == _hoveredIndex)
            {
                e.Graphics.DrawRectangle(Pens.DarkGray, cell.X, cell.Y, cell.Width - 1, cell.Height - 1);
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        int index = IndexAt(e.Location);

        if (index != _hoveredIndex)
        {
            _hoveredIndex = index;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);

        _hoveredIndex = -1;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        int index = IndexAt(e.Location);

        if (e.Button == MouseButtons.Left && index >= 0)
        {
            ColorPicked?.Invoke(this, _cells[index].Color);
        }
    }

    /// <summary>Places every square, then sizes the control to them.</summary>
    private void LayOut()
    {
        _cells = [];
        int top = Inset;

        foreach (IReadOnlyList<Color> section in new[] { _colors, _customColors })
        {
            if (section.Count == 0)
            {
                continue;
            }

            if (_cells.Count > 0)
            {
                top += SectionGap;
            }

            for (int i = 0; i < section.Count; i++)
            {
                var bounds = new Rectangle(
                    Inset + i % Columns * CellSize,
                    top + i / Columns * CellSize,
                    CellSize,
                    CellSize);
                _cells.Add((section[i], bounds));
            }

            top += (section.Count + Columns - 1) / Columns * CellSize;
        }

        _hoveredIndex = -1;
        Size = GetPreferredSize(Size.Empty);
        Invalidate();
    }

    /// <returns>The index of the square under the point; -1 when there's none.</returns>
    private int IndexAt(Point point) => _cells.FindIndex(cell => cell.Bounds.Contains(point));
}
