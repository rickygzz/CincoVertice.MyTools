using System.ComponentModel;
using CincoVertice.MyTools.Ui.ColorPicker.Services;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;

namespace CincoVertice.MyTools.Ui.Win.Diagramming;

/// <summary>
///     The colors of a diagram's color menu, as Office lays them out: the theme's colors with their lighter and darker
///     versions under them, the standard colors, and the colors used lately, each under a heading. Hovering a square
///     says what it is, e.g. "Accent 1, lighter 40% · #9DC3E6"; the selected color has a dark frame; clicking a square
///     raises <see cref="ColorPicked"/>.
/// </summary>
public sealed class ThemeColorPaletteControl : Control
{
    private const int Inset = 4;

    private readonly ToolTip _toolTip = new() { InitialDelay = 300, ReshowDelay = 100 };

    private DiagramPaletteModel? _palette;
    private Color _selectedColor = Color.Empty;
    private int _hoveredIndex = -1;

    // Every square with its swatch, and every heading with where it goes: laid out when the palette changes
    private List<(DiagramSwatchModel Swatch, Rectangle Bounds)> _cells = [];
    private List<(string Text, Point Location)> _headings = [];

    public ThemeColorPaletteControl()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
    }

    public event EventHandler<Color>? ColorPicked;

    /// <summary>What to offer.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiagramPaletteModel? Palette
    {
        get => _palette;
        set
        {
            _palette = value;
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

    /// <summary>What a square shows when hovered: its name and its hex code.</summary>
    internal static string ToolTipOf(DiagramSwatchModel swatch) => $"{swatch.Name} · {ColorText.Hex(swatch.Color)}";

    /// <summary>The swatch under a point, for tests and the mouse.</summary>
    internal DiagramSwatchModel? SwatchAt(Point point) =>
        IndexAt(point) is int index and >= 0 ? _cells[index].Swatch : null;

    /// <summary>The middle of the square of the swatch at that place of all the squares, for tests.</summary>
    internal Point CenterOf(int index)
    {
        Rectangle bounds = _cells[index].Bounds;

        return new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
    }

    // 18 pixels at 100% scaling, so ten fit in a menu
    private int CellSize => LogicalToDeviceUnits(18);

    private int HeadingHeight => Font.Height + LogicalToDeviceUnits(4);

    // Between a theme's own colors and their versions, as Office has it
    private int RowGap => LogicalToDeviceUnits(5);

    public override Size GetPreferredSize(Size proposedSize)
    {
        int bottom = _cells.Count == 0 ? 0 : _cells.Max(cell => cell.Bounds.Bottom);

        return new Size(DiagramThemes.Columns * CellSize + 2 * Inset, bottom + Inset);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        foreach ((string text, Point location) in _headings)
        {
            TextRenderer.DrawText(e.Graphics, text, Font, location, SystemColors.GrayText);
        }

        int inner = LogicalToDeviceUnits(2);

        for (int i = 0; i < _cells.Count; i++)
        {
            (DiagramSwatchModel swatch, Rectangle bounds) = _cells[i];
            Rectangle square = Rectangle.Inflate(bounds, -inner, -inner);
            bool isSelected = swatch.Color.ToArgb() == _selectedColor.ToArgb() && !_selectedColor.IsEmpty;

            using (var brush = new SolidBrush(swatch.Color))
            {
                e.Graphics.FillRectangle(brush, square);
            }

            e.Graphics.DrawRectangle(Pens.Silver, square.X, square.Y, square.Width - 1, square.Height - 1);

            if (isSelected || i == _hoveredIndex)
            {
                using var frame = new Pen(isSelected ? Color.Black : SystemColors.Highlight, LogicalToDeviceUnits(2));

                e.Graphics.DrawRectangle(frame, Rectangle.Inflate(square, 1, 1));
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        int index = IndexAt(e.Location);

        if (index == _hoveredIndex)
        {
            return;
        }

        _hoveredIndex = index;
        _toolTip.SetToolTip(this, index >= 0 ? ToolTipOf(_cells[index].Swatch) : null);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoveredIndex = -1;
        _toolTip.SetToolTip(this, null);
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        if (e.Button == MouseButtons.Left && SwatchAt(e.Location) is { } swatch)
        {
            ColorPicked?.Invoke(this, swatch.Color);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    private int IndexAt(Point point) => _cells.FindIndex(cell => cell.Bounds.Contains(point));

    /// <summary>Each section under its heading; the colors used lately only when there are some.</summary>
    private void LayOut()
    {
        _cells = [];
        _headings = [];
        _hoveredIndex = -1;

        if (_palette is not null)
        {
            int top = Inset;

            top = AddSection($"{_palette.Theme} theme colors", _palette.ThemeColors, top, gapAfterFirstRow: true);
            top = AddSection("Standard colors", _palette.StandardColors, top, gapAfterFirstRow: false);

            if (_palette.RecentColors.Count > 0)
            {
                AddSection("Used lately", _palette.RecentColors, top, gapAfterFirstRow: false);
            }
        }

        Size = GetPreferredSize(Size.Empty);
        Invalidate();
    }

    /// <returns>Where the next section starts.</returns>
    private int AddSection(string heading, IReadOnlyList<DiagramSwatchModel> swatches, int top, bool gapAfterFirstRow)
    {
        _headings.Add((heading, new Point(Inset, top)));
        top += HeadingHeight;

        for (int i = 0; i < swatches.Count; i++)
        {
            int row = i / DiagramThemes.Columns;
            int y = top + row * CellSize + (gapAfterFirstRow && row > 0 ? RowGap : 0);

            int x = Inset + i % DiagramThemes.Columns * CellSize;

            _cells.Add((swatches[i], new Rectangle(x, y, CellSize, CellSize)));
        }

        int rows = (swatches.Count + DiagramThemes.Columns - 1) / DiagramThemes.Columns;

        return top + rows * CellSize + (gapAfterFirstRow && rows > 1 ? RowGap : 0) + LogicalToDeviceUnits(6);
    }
}
