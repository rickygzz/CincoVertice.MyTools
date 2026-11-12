using System.ComponentModel;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;

namespace CincoVertice.MyTools.Ui.Win.Snipping;

/// <summary>
///     A grid of emoji, in color, 8 to a row. The one chosen has a dark frame, the one under the mouse a light one;
///     clicking one raises <see cref="EmojiPicked"/>.
/// </summary>
public sealed class EmojiPaletteControl : Control
{
    private const int Columns = 8;
    private const int Inset = 4;

    private IReadOnlyList<string> _emojis = [];
    private string? _selectedEmoji;
    private int _hoveredIndex = -1;

    public EmojiPaletteControl()
    {
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        Size = GetPreferredSize(Size.Empty);
    }

    public event EventHandler<string>? EmojiPicked;

    /// <summary>The emoji to choose from, in rows of 8.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<string> Emojis
    {
        get => _emojis;
        set
        {
            _emojis = value;
            _hoveredIndex = -1;
            Size = GetPreferredSize(Size.Empty);
            Invalidate();
        }
    }

    /// <summary>The emoji with the dark frame; one that isn't shown frames none.</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? SelectedEmoji
    {
        get => _selectedEmoji;
        set
        {
            _selectedEmoji = value;
            Invalidate();
        }
    }

    // 30 pixels at 100% scaling, with the emoji 22 of them
    private int CellSize => LogicalToDeviceUnits(30);

    private int EmojiSize => LogicalToDeviceUnits(22);

    public override Size GetPreferredSize(Size proposedSize)
    {
        int rows = Math.Max(1, (_emojis.Count + Columns - 1) / Columns);

        return new Size(Columns * CellSize + 2 * Inset, rows * CellSize + 2 * Inset);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        for (int i = 0; i < _emojis.Count; i++)
        {
            Rectangle cell = CellOf(i);
            int margin = (CellSize - EmojiSize) / 2;
            var square = new Rectangle(cell.X + margin, cell.Y + margin, EmojiSize, EmojiSize);

            if (_emojis[i] == _selectedEmoji)
            {
                using var frame = new Pen(Color.FromArgb(40, 40, 40), 2);
                e.Graphics.DrawRectangle(frame, cell.X + 1, cell.Y + 1, cell.Width - 2, cell.Height - 2);
            }
            else if (i == _hoveredIndex)
            {
                e.Graphics.FillRectangle(SystemBrushes.ControlLight, cell);
            }

            e.Graphics.DrawImage(ColorEmojiRenderer.Render(_emojis[i], EmojiSize), square);
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
            EmojiPicked?.Invoke(this, _emojis[index]);
        }
    }

    private Rectangle CellOf(int index) =>
        new(Inset + index % Columns * CellSize, Inset + index / Columns * CellSize, CellSize, CellSize);

    /// <returns>The index of the emoji under the point; -1 when there's none.</returns>
    private int IndexAt(Point point)
    {
        for (int i = 0; i < _emojis.Count; i++)
        {
            if (CellOf(i).Contains(point))
            {
                return i;
            }
        }

        return -1;
    }
}
