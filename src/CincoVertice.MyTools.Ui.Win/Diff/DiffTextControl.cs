using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Text;
using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.Win.Diff;

/// <summary>
///     Two texts compared, side by side, in rows that scroll together: each side has its line numbers, then its text,
///     colored as the row is (removed red, added green, changed both, the words that changed darker), and a blank
///     side where the other has a line. A strip at the right shows where the changes are in the whole text; a click on
///     it goes there. It draws only the rows showing, so long files scroll as fast as short ones.
///     <para>
///         A click selects a row on its side, Shift+click or a drag more rows; Ctrl+C copies their lines, Ctrl+A
///         selects every row of the side. It only shows: the rows come from <see cref="SetRows"/>.
///     </para>
/// </summary>
public sealed class DiffTextControl : Control
{
    public const int TabSize = 4;
    public const int OverviewWidth = 14;
    private const int DividerWidth = 4;
    private const int TextMargin = 4;
    private const int GutterPadding = 6;

    private static readonly Color _removedBack = Color.FromArgb(255, 235, 233);
    private static readonly Color _removedWord = Color.FromArgb(255, 193, 192);
    private static readonly Color _removedGutter = Color.FromArgb(255, 215, 213);
    private static readonly Color _addedBack = Color.FromArgb(230, 255, 236);
    private static readonly Color _addedWord = Color.FromArgb(171, 242, 188);
    private static readonly Color _addedGutter = Color.FromArgb(204, 255, 216);
    private static readonly Color _blankBack = Color.FromArgb(240, 240, 240);
    private static readonly Color _gutterBack = Color.FromArgb(246, 246, 246);
    private static readonly Color _gutterFore = Color.FromArgb(120, 120, 120);
    private static readonly Color _selectedBack = Color.FromArgb(204, 228, 247);
    private static readonly Color _dividerColor = Color.FromArgb(210, 210, 210);
    private static readonly Color _currentColor = Color.FromArgb(0, 120, 215);
    private static readonly Color _overviewBack = Color.FromArgb(250, 250, 250);
    private static readonly Color _overviewAdded = Color.FromArgb(45, 164, 78);
    private static readonly Color _overviewRemoved = Color.FromArgb(207, 34, 46);
    private static readonly Color _overviewChanged = Color.FromArgb(191, 135, 0);
    private static readonly Color _overviewView = Color.FromArgb(60, 0, 0, 0);

    private const TextFormatFlags TextFlags =
        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine
        | TextFormatFlags.PreserveGraphicsClipping;

    private readonly VScrollBar _vertical = new() { Dock = DockStyle.Right, TabStop = false };
    private readonly HScrollBar _horizontal = new() { Dock = DockStyle.Bottom, TabStop = false };

    private IReadOnlyList<DiffRowModel> _rows = [];
    private IReadOnlyList<DiffHunkModel> _hunks = [];
    private DiffHunkModel? _current;
    private int _maxColumns;
    private int _numberDigits = 1;
    private int _rowHeight = 16;
    private float _charWidth = 8;

    // The rows selected, from the anchor to the caret, on one side; -1 for none
    private DiffSide _selectionSide;
    private int _anchor = -1;
    private int _caret = -1;
    private bool _isSelecting;
    private bool _isDraggingOverview;

    public DiffTextControl()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw | ControlStyles.Selectable,
            true);
        BackColor = Color.White;
        ForeColor = Color.Black;
        Font = new Font("Consolas", 10);
        TabStop = true;

        Controls.Add(_vertical);
        Controls.Add(_horizontal);
        _vertical.ValueChanged += (_, _) => Invalidate();
        _horizontal.ValueChanged += (_, _) => Invalidate();
        Measure();
    }

    /// <summary>The width at its right that isn't text, the strip and the scroll bar, for headers to line up.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int RightMargin => OverviewWidth + _vertical.Width;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int TopRow
    {
        get => _vertical.Value;
        set => _vertical.Value = Math.Clamp(value, 0, MaxTopRow);
    }

    /// <summary>The row the user is at: the one last clicked or moved to, if showing, else the first showing.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CurrentRow => _caret >= TopRow && _caret < TopRow + VisibleRows ? _caret : TopRow;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<DiffRowModel> Rows => _rows;

    internal int RowHeight => _rowHeight;

    private int VisibleRows => Math.Max(1, TextArea.Height / _rowHeight);

    private int MaxTopRow => Math.Max(0, _rows.Count - VisibleRows);

    private Rectangle TextArea => new(
        0,
        0,
        Math.Max(0, ClientSize.Width - RightMargin),
        Math.Max(0, ClientSize.Height - _horizontal.Height));

    private int SideWidth => Math.Max(0, (TextArea.Width - DividerWidth) / 2);

    private int GutterWidth => (int)Math.Ceiling((_numberDigits + 2) * _charWidth) + GutterPadding;

    private int VisibleColumns => Math.Max(1, (int)((SideWidth - GutterWidth - TextMargin) / _charWidth));

    /// <param name="keepPlace">Whether to stay at the rows showing, else go to the top.</param>
    public void SetRows(IReadOnlyList<DiffRowModel> rows, IReadOnlyList<DiffHunkModel> hunks, bool keepPlace)
    {
        int top = TopRow;
        _rows = rows;
        _hunks = hunks;
        _current = null;
        _anchor = -1;
        _caret = -1;
        _maxColumns = 0;
        int lastNumber = 1;

        foreach (DiffRowModel row in rows)
        {
            foreach (DiffLineModel? line in new[] { row.Left, row.Right })
            {
                if (line is not null)
                {
                    _maxColumns = Math.Max(_maxColumns, ColumnsOf(line.Text)[^1]);
                    lastNumber = Math.Max(lastNumber, line.Number);
                }
            }
        }

        _numberDigits = lastNumber.ToString().Length;
        UpdateScrollBars();
        TopRow = keepPlace ? top : 0;

        if (!keepPlace)
        {
            _horizontal.Value = 0;
        }

        Invalidate();
    }

    /// <summary>Marks a change as the current one, and scrolls it into view, a few rows below the top.</summary>
    public void ShowHunk(DiffHunkModel? hunk)
    {
        _current = hunk;

        if (hunk is not null)
        {
            _caret = hunk.FirstRow;
            _anchor = -1;
            bool isShowing = hunk.FirstRow >= TopRow && hunk.EndRow <= TopRow + VisibleRows;

            if (!isShowing)
            {
                TopRow = hunk.RowCount >= VisibleRows - 3
                    ? hunk.FirstRow
                    : hunk.FirstRow - Math.Min(3, VisibleRows / 4);
            }
        }

        Invalidate();
    }

    /// <returns>The side a point of the control is on, for a drop.</returns>
    public DiffSide SideAt(Point point) => point.X < SideWidth + DividerWidth / 2 ? DiffSide.Left : DiffSide.Right;

    /// <returns>The lines of the rows selected, on their side, blank rows left out; empty for none.</returns>
    public string SelectedText()
    {
        if (_anchor < 0 || _caret < 0)
        {
            return string.Empty;
        }

        int first = Math.Min(_anchor, _caret);
        int last = Math.Max(_anchor, _caret);
        var text = new StringBuilder();

        for (int row = Math.Max(0, first); row <= last && row < _rows.Count; row++)
        {
            if (_rows[row].On(_selectionSide) is { } line)
            {
                text.Append(line.Text).Append(Environment.NewLine);
            }
        }

        return text.ToString();
    }

    /// <returns>
    ///     The column each character of a line starts at, tabs going to the next multiple of <see cref="TabSize"/>,
    ///     and, last, the line's width in columns.
    /// </returns>
    public static int[] ColumnsOf(string text)
    {
        var columns = new int[text.Length + 1];
        int column = 0;

        for (int i = 0; i < text.Length; i++)
        {
            columns[i] = column;
            column += text[i] == '\t' ? TabSize - column % TabSize : 1;
        }

        columns[text.Length] = column;

        return columns;
    }

    /// <returns>The line as drawn: tabs as spaces, one character per column.</returns>
    public static string Expanded(string text)
    {
        if (!text.Contains('\t'))
        {
            return text;
        }

        var expanded = new StringBuilder(text.Length + 16);

        foreach (char c in text)
        {
            if (c == '\t')
            {
                expanded.Append(' ', TabSize - expanded.Length % TabSize);
            }
            else
            {
                expanded.Append(c);
            }
        }

        return expanded.ToString();
    }

    protected override bool IsInputKey(Keys keyData) =>
        (keyData & Keys.KeyCode) is Keys.Up or Keys.Down or Keys.Left or Keys.Right || base.IsInputKey(keyData);

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        Measure();
        UpdateScrollBars();
        Invalidate();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        UpdateScrollBars();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        int lines = SystemInformation.MouseWheelScrollLines * e.Delta / 120;

        if ((ModifierKeys & Keys.Shift) != 0)
        {
            ScrollColumns(-lines * 2);
        }
        else
        {
            TopRow -= lines;
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.Button == MouseButtons.Left)
        {
            Press(e.Location, (ModifierKeys & Keys.Shift) != 0);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        DragTo(e.Location);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        Release();
    }

    /// <summary>A press of the left button: on the strip, goes there; on a row, selects it, or up to it.</summary>
    internal void Press(Point point, bool extend)
    {
        if (point.X >= TextArea.Right && point.X < TextArea.Right + OverviewWidth)
        {
            _isDraggingOverview = true;
            ScrollToOverview(point.Y);

            return;
        }

        int row = RowAt(point.Y);

        if (row < 0)
        {
            return;
        }

        DiffSide side = SideAt(point);

        if (!extend || _caret < 0 || side != _selectionSide)
        {
            _anchor = row;
            _selectionSide = side;
        }
        else if (_anchor < 0)
        {
            _anchor = _caret;
        }

        _caret = row;
        _isSelecting = true;
        Invalidate();
    }

    internal void DragTo(Point point)
    {
        if (_isDraggingOverview)
        {
            ScrollToOverview(point.Y);

            return;
        }

        if (!_isSelecting || _rows.Count == 0)
        {
            return;
        }

        // Beyond the top or bottom, the rows scroll on
        if (point.Y < 0)
        {
            TopRow--;
        }
        else if (point.Y >= TextArea.Height)
        {
            TopRow++;
        }

        int y = Math.Clamp(point.Y, 0, Math.Max(0, TextArea.Height - 1));
        int row = Math.Clamp(TopRow + y / _rowHeight, 0, _rows.Count - 1);

        if (row != _caret)
        {
            _caret = row;
            Invalidate();
        }
    }

    internal void Release()
    {
        _isSelecting = false;
        _isDraggingOverview = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        bool shift = e.Shift;

        switch (e.KeyCode)
        {
            case Keys.C when e.Control:
            case Keys.Insert when e.Control:
                Copy();
                break;
            case Keys.A when e.Control:
                _anchor = 0;
                _caret = _rows.Count - 1;
                Invalidate();
                break;
            case Keys.Up:
                MoveCaret(-1, shift);
                break;
            case Keys.Down:
                MoveCaret(1, shift);
                break;
            case Keys.PageUp:
                MoveCaret(-VisibleRows, shift);
                break;
            case Keys.PageDown:
                MoveCaret(VisibleRows, shift);
                break;
            case Keys.Home when e.Control:
                MoveCaret(-_rows.Count, shift);
                break;
            case Keys.End when e.Control:
                MoveCaret(_rows.Count, shift);
                break;
            case Keys.Home:
                _horizontal.Value = 0;
                break;
            case Keys.Left:
                ScrollColumns(-4);
                break;
            case Keys.Right:
                ScrollColumns(4);
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.Clear(BackColor);
        Rectangle area = TextArea;
        int sideWidth = SideWidth;
        var left = new Rectangle(0, 0, sideWidth, area.Height);
        var right = new Rectangle(sideWidth + DividerWidth, 0, area.Width - sideWidth - DividerWidth, area.Height);
        int last = Math.Min(_rows.Count, TopRow + VisibleRows + 1);

        for (int row = TopRow; row < last; row++)
        {
            int y = (row - TopRow) * _rowHeight;
            PaintLine(graphics, left, y, row, DiffSide.Left);
            PaintLine(graphics, right, y, row, DiffSide.Right);
        }

        using (var divider = new SolidBrush(_dividerColor))
        {
            graphics.FillRectangle(divider, sideWidth, 0, DividerWidth, area.Height);
        }

        PaintCurrent(graphics, area);
        PaintOverview(graphics, new Rectangle(area.Right, 0, OverviewWidth, area.Height));
    }

    private void Measure()
    {
        _rowHeight = Math.Max(1, TextRenderer.MeasureText("Mg", Font, Size.Empty, TextFlags).Height + 2);
        Size hundred = TextRenderer.MeasureText(new string('M', 100), Font, Size.Empty, TextFlags);
        _charWidth = Math.Max(1, hundred.Width / 100f);
    }

    private void UpdateScrollBars()
    {
        _vertical.Minimum = 0;
        _vertical.LargeChange = VisibleRows;
        _vertical.SmallChange = 1;
        _vertical.Maximum = Math.Max(0, _rows.Count - 1);
        _vertical.Enabled = _rows.Count > VisibleRows;
        TopRow = TopRow;

        _horizontal.Minimum = 0;
        _horizontal.LargeChange = VisibleColumns;
        _horizontal.SmallChange = 1;
        _horizontal.Maximum = Math.Max(0, _maxColumns);
        _horizontal.Enabled = _maxColumns > VisibleColumns;
        _horizontal.Value = Math.Clamp(_horizontal.Value, 0, Math.Max(0, _maxColumns - VisibleColumns + 1));
    }

    private int RowAt(int y)
    {
        int row = TopRow + y / _rowHeight;

        return y >= 0 && y < TextArea.Height && row < _rows.Count ? row : -1;
    }

    private void ScrollColumns(int columns)
    {
        int max = Math.Max(0, _maxColumns - VisibleColumns + 1);
        _horizontal.Value = Math.Clamp(_horizontal.Value + columns, 0, max);
    }

    private void ScrollToOverview(int y)
    {
        if (_rows.Count == 0 || TextArea.Height == 0)
        {
            return;
        }

        int row = (int)((long)Math.Clamp(y, 0, TextArea.Height) * _rows.Count / TextArea.Height);
        TopRow = row - VisibleRows / 2;
    }

    private void MoveCaret(int rows, bool extend)
    {
        if (_rows.Count == 0)
        {
            return;
        }

        int from = _caret < 0 ? TopRow : _caret;

        if (extend && _anchor < 0)
        {
            _anchor = from;
        }

        _caret = Math.Clamp(from + rows, 0, _rows.Count - 1);

        if (!extend)
        {
            _anchor = _caret;
        }

        if (_caret < TopRow)
        {
            TopRow = _caret;
        }
        else if (_caret >= TopRow + VisibleRows)
        {
            TopRow = _caret - VisibleRows + 1;
        }

        Invalidate();
    }

    private void Copy()
    {
        string text = SelectedText();

        if (text.Length > 0)
        {
            Clipboard.SetText(text);
        }
    }

    private bool IsSelected(int row, DiffSide side)
    {
        if (side != _selectionSide || _anchor < 0 || _caret < 0)
        {
            return false;
        }

        return row >= Math.Min(_anchor, _caret) && row <= Math.Max(_anchor, _caret);
    }

    private void PaintLine(Graphics graphics, Rectangle side, int y, int rowIndex, DiffSide which)
    {
        DiffRowModel row = _rows[rowIndex];
        DiffLineModel? line = row.On(which);
        bool isLeft = which == DiffSide.Left;
        bool isChange = row.Kind != DiffRowKind.Same && line is not null;

        Color back = line is null ? _blankBack
            : !isChange ? BackColor
            : isLeft ? _removedBack
            : _addedBack;

        if (IsSelected(rowIndex, which))
        {
            back = _selectedBack;
        }

        var rowBounds = new Rectangle(side.X, y, side.Width, _rowHeight);
        using (var backBrush = new SolidBrush(back))
        {
            graphics.FillRectangle(backBrush, rowBounds);
        }

        // Line number and +/-, in the gutter
        var gutter = new Rectangle(side.X, y, GutterWidth, _rowHeight);
        Color gutterBack = !isChange ? _gutterBack : isLeft ? _removedGutter : _addedGutter;

        using (var gutterBrush = new SolidBrush(line is null ? _blankBack : gutterBack))
        {
            graphics.FillRectangle(gutterBrush, gutter);
        }

        if (line is null)
        {
            return;
        }

        var number = new Rectangle(gutter.X, y + 1, (int)(_numberDigits * _charWidth) + GutterPadding / 2, _rowHeight);
        TextRenderer.DrawText(
            graphics,
            line.Number.ToString(),
            Font,
            number,
            _gutterFore,
            TextFlags | TextFormatFlags.Right);

        if (isChange)
        {
            var sign = new Point(number.Right + (int)(_charWidth / 2), y + 1);
            TextRenderer.DrawText(graphics, isLeft ? "-" : "+", Font, sign, _gutterFore, TextFlags);
        }

        // The text, from the first column showing
        var text = Rectangle.FromLTRB(gutter.Right + TextMargin, y, side.Right, y + _rowHeight);
        int firstColumn = _horizontal.Value;
        int[] columns = ColumnsOf(line.Text);
        GraphicsState state = graphics.Save();
        graphics.SetClip(text);

        if (row.Kind == DiffRowKind.Changed && line.Changes.Count > 0)
        {
            using var wordBrush = new SolidBrush(isLeft ? _removedWord : _addedWord);

            foreach (DiffRange change in line.Changes)
            {
                float from = text.X + (columns[change.Start] - firstColumn) * _charWidth;
                float to = text.X + (columns[Math.Min(change.End, line.Text.Length)] - firstColumn) * _charWidth;
                graphics.FillRectangle(wordBrush, from, y, Math.Max(to - from, 2), _rowHeight);
            }
        }

        string expanded = Expanded(line.Text);

        if (firstColumn < expanded.Length)
        {
            int length = Math.Min(expanded.Length - firstColumn, VisibleColumns + 2);
            TextRenderer.DrawText(
                graphics,
                expanded.Substring(firstColumn, length),
                Font,
                new Point(text.X, y + 1),
                ForeColor,
                TextFlags);
        }

        graphics.Restore(state);
    }

    // A line above and below the current change, across both sides
    private void PaintCurrent(Graphics graphics, Rectangle area)
    {
        if (_current is null)
        {
            return;
        }

        using var pen = new Pen(_currentColor, 2);
        int top = (_current.FirstRow - TopRow) * _rowHeight;
        int bottom = (_current.EndRow - TopRow) * _rowHeight;

        if (top >= 0 && top <= area.Height)
        {
            graphics.DrawLine(pen, 0, top + 1, area.Width, top + 1);
        }

        if (bottom >= 0 && bottom <= area.Height)
        {
            graphics.DrawLine(pen, 0, bottom - 1, area.Width, bottom - 1);
        }
    }

    private void PaintOverview(Graphics graphics, Rectangle strip)
    {
        using (var back = new SolidBrush(_overviewBack))
        {
            graphics.FillRectangle(back, strip);
        }

        if (_rows.Count == 0 || strip.Height == 0)
        {
            return;
        }

        float scale = (float)strip.Height / _rows.Count;

        foreach (DiffHunkModel hunk in _hunks)
        {
            Color color = KindsOf(hunk) switch
            {
                (true, false) => _overviewAdded,
                (false, true) => _overviewRemoved,
                _ => _overviewChanged,
            };
            using var brush = new SolidBrush(color);
            float height = Math.Max(2, hunk.RowCount * scale);
            graphics.FillRectangle(brush, strip.X + 3, hunk.FirstRow * scale, strip.Width - 6, height);
        }

        if (_rows.Count > VisibleRows)
        {
            using var view = new SolidBrush(_overviewView);
            graphics.FillRectangle(view, strip.X, TopRow * scale, strip.Width, Math.Max(4, VisibleRows * scale));
        }
    }

    // Whether a change only adds lines, or only removes them
    private (bool OnlyAdded, bool OnlyRemoved) KindsOf(DiffHunkModel hunk)
    {
        bool added = true;
        bool removed = true;

        for (int row = hunk.FirstRow; row < hunk.EndRow; row++)
        {
            added &= _rows[row].Kind == DiffRowKind.Added;
            removed &= _rows[row].Kind == DiffRowKind.Removed;
        }

        return (added, removed);
    }
}
