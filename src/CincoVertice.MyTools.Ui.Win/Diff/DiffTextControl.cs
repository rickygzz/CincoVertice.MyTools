using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Text;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Services;

namespace CincoVertice.MyTools.Ui.Win.Diff;

/// <summary>
///     Two texts compared and edited side by side, in rows that scroll together: each side has its line numbers, then
///     its text, colored as the row is (removed red, added green, changed both, the words that changed darker), and a
///     blank side where the other has a line. A strip at the right shows where the changes are in the whole text; a
///     click on it goes there. It draws only the rows showing, so long files scroll as fast as short ones.
///     <para>
///         Each side is a text being edited (<see cref="DiffEditBuffer"/>), with its caret and selection; a click puts
///         the caret on a side. Every change raises <see cref="TextEdited"/>, for the rows to be compared again and
///         given back through <see cref="SetRows"/>, at once, so the rows always show the texts as they are.
///     </para>
/// </summary>
public sealed class DiffTextControl : Control
{
    public const int TabSize = 4;
    public const int OverviewWidth = 14;
    private const int DividerWidth = 4;
    private const int TextMargin = 4;
    private const int GutterPadding = 6;
    private const int CaretWidth = 2;

    private static readonly Color _removedBack = Color.FromArgb(255, 235, 233);
    private static readonly Color _removedWord = Color.FromArgb(255, 193, 192);
    private static readonly Color _removedGutter = Color.FromArgb(255, 215, 213);
    private static readonly Color _addedBack = Color.FromArgb(230, 255, 236);
    private static readonly Color _addedWord = Color.FromArgb(171, 242, 188);
    private static readonly Color _addedGutter = Color.FromArgb(204, 255, 216);
    private static readonly Color _blankBack = Color.FromArgb(240, 240, 240);
    private static readonly Color _gutterBack = Color.FromArgb(246, 246, 246);
    private static readonly Color _gutterFore = Color.FromArgb(120, 120, 120);
    private static readonly Color _selectionBack = Color.FromArgb(173, 214, 255);
    private static readonly Color _otherSelectionBack = Color.FromArgb(218, 218, 218);
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
    private readonly DiffEditBuffer _leftBuffer = new();
    private readonly DiffEditBuffer _rightBuffer = new();
    private readonly System.Windows.Forms.Timer _blink = new() { Interval = 530 };
    private readonly ContextMenuStrip _menu = new();

    private IReadOnlyList<DiffRowModel> _rows = [];
    private IReadOnlyList<DiffHunkModel> _hunks = [];
    private DiffHunkModel? _current;
    private int[] _leftRowOf = [];
    private int[] _rightRowOf = [];
    private int _maxColumns;
    private int _numberDigits = 1;
    private int _rowHeight = 16;
    private float _charWidth = 8;

    // The side being edited; the row Next and Previous go from; the column Up and Down keep to
    private DiffSide _side;
    private int _navigationRow = -1;
    private int _preferredColumn = -1;
    private bool _isCaretOn = true;
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
        Cursor = Cursors.IBeam;
        TabStop = true;

        Controls.Add(_vertical);
        Controls.Add(_horizontal);
        _vertical.Cursor = Cursors.Default;
        _horizontal.Cursor = Cursors.Default;
        _vertical.ValueChanged += (_, _) => Invalidate();
        _horizontal.ValueChanged += (_, _) => Invalidate();

        // The control isn't made by the designer, so its own parts are wired here
        _blink.Tick += (_, _) => Blink();
        BuildMenu();
        ContextMenuStrip = _menu;
        Measure();
    }

    /// <summary>The user changed the text of a side; <see cref="LinesOf"/> has it.</summary>
    public event EventHandler<DiffSide>? TextEdited;

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

    /// <summary>The row the user is at: the caret's, or the change gone to, if showing, else the top one.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CurrentRow =>
        _navigationRow >= TopRow && _navigationRow < TopRow + VisibleRows ? _navigationRow : TopRow;

    /// <summary>The side the caret is on, which the keys type into.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DiffSide EditedSide => _side;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<DiffRowModel> Rows => _rows;

    internal int RowHeight => _rowHeight;

    private DiffEditBuffer Buffer => BufferOf(_side);

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

    private int MaxLeftColumn => Math.Max(0, _maxColumns + 2 - VisibleColumns);

    public IReadOnlyList<string> LinesOf(DiffSide side) => BufferOf(side).Lines;

    /// <summary>Puts a new text on a side, the caret at its start, with nothing to undo.</summary>
    public void SetText(DiffSide side, IReadOnlyList<string> lines)
    {
        BufferOf(side).SetLines(lines);
        Invalidate();
    }

    /// <param name="keepPlace">Whether to stay at the rows showing, else go to the top.</param>
    public void SetRows(IReadOnlyList<DiffRowModel> rows, IReadOnlyList<DiffHunkModel> hunks, bool keepPlace)
    {
        int top = TopRow;
        _rows = rows;
        _hunks = hunks;
        _current = null;
        _maxColumns = 0;
        int lastNumber = 1;
        _leftRowOf = RowsOfLines(rows, DiffSide.Left, _leftBuffer.Lines.Count);
        _rightRowOf = RowsOfLines(rows, DiffSide.Right, _rightBuffer.Lines.Count);

        foreach (DiffRowModel row in rows)
        {
            foreach (DiffLineModel? line in new[] { row.Left, row.Right })
            {
                if (line is not null)
                {
                    _maxColumns = Math.Max(_maxColumns, WidthOf(line.Text));
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
            _navigationRow = -1;
        }

        Invalidate();
    }

    /// <summary>
    ///     Marks a change as the current one, and scrolls it into view, a few rows below the top; the caret goes to
    ///     its first line on the side being edited.
    /// </summary>
    public void ShowHunk(DiffHunkModel? hunk)
    {
        _current = hunk;

        if (hunk is not null)
        {
            _navigationRow = hunk.FirstRow;
            Buffer.MoveTo(new DiffPosition(LineAtRow(_side, hunk.FirstRow).Line, 0), extend: false);
            bool isShowing = hunk.FirstRow >= TopRow && hunk.EndRow <= TopRow + VisibleRows;

            if (!isShowing)
            {
                TopRow = hunk.RowCount >= VisibleRows - 3
                    ? hunk.FirstRow
                    : hunk.FirstRow - Math.Min(3, VisibleRows / 4);
            }

            _horizontal.Value = 0;
            RestartBlink();
        }

        Invalidate();
    }

    /// <returns>The side a point of the control is on, for a drop or a click.</returns>
    public DiffSide SideAt(Point point) => point.X < SideWidth + DividerWidth / 2 ? DiffSide.Left : DiffSide.Right;

    /// <returns>The text selected on the side being edited, with Windows line breaks; empty for none.</returns>
    public string SelectedText() => Buffer.SelectedText;

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

    /// <returns>The line's width in columns, as <see cref="ColumnsOf"/> counts them.</returns>
    public static int WidthOf(string text)
    {
        int column = 0;

        foreach (char c in text)
        {
            column += c == '\t' ? TabSize - column % TabSize : 1;
        }

        return column;
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

    internal DiffEditBuffer BufferOf(DiffSide side) => side == DiffSide.Left ? _leftBuffer : _rightBuffer;

    /// <summary>A press of the left button: on the strip, goes there; on a side, puts the caret there.</summary>
    /// <param name="extend">Shift: the selection grows to it.</param>
    /// <param name="isDouble">A double click: selects the word.</param>
    internal void Press(Point point, bool extend, bool isDouble = false)
    {
        if (point.X >= TextArea.Right && point.X < TextArea.Right + OverviewWidth)
        {
            _isDraggingOverview = true;
            ScrollToOverview(point.Y);

            return;
        }

        if (point.Y >= TextArea.Height)
        {
            return;
        }

        DiffSide side = SideAt(point);
        extend &= side == _side;
        _side = side;
        DiffPosition position = PositionAt(point);

        if (isDouble)
        {
            Buffer.SelectWord(position);
        }
        else
        {
            Buffer.MoveTo(position, extend);
            _isSelecting = true;
        }

        _preferredColumn = -1;
        CaretMoved(scroll: false);
    }

    internal void DragTo(Point point)
    {
        if (_isDraggingOverview)
        {
            ScrollToOverview(point.Y);

            return;
        }

        if (!_isSelecting)
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

        Buffer.MoveTo(PositionAt(point with { X = Math.Max(point.X, TextLeftOf(_side)) }, _side), extend: true);
        CaretMoved(scroll: false);
    }

    internal void Release()
    {
        _isSelecting = false;
        _isDraggingOverview = false;
    }

    /// <summary>Handles a key as typing would, for the keyboard and for tests.</summary>
    /// <returns>Whether the key did something here.</returns>
    internal bool HandleKey(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;
        bool shift = (keyData & Keys.Shift) != 0;
        bool control = (keyData & Keys.Control) != 0;

        if ((keyData & Keys.Alt) != 0)
        {
            return false;
        }

        switch (key)
        {
            case Keys.Z when control && shift:
            case Keys.Y when control:
                Edit(buffer => buffer.Redo());
                return true;
            case Keys.Z when control:
                Edit(buffer => buffer.Undo());
                return true;
            case Keys.C when control:
            case Keys.Insert when control:
                Copy();
                return true;
            case Keys.X when control:
            case Keys.Delete when shift:
                Cut();
                return true;
            case Keys.V when control:
            case Keys.Insert when shift:
                Paste();
                return true;
            case Keys.A when control:
                Buffer.SelectAll();
                CaretMoved(scroll: false);
                return true;
            case Keys.Back:
                Edit(buffer => buffer.DeleteBackward(control));
                return true;
            case Keys.Delete:
                Edit(buffer => buffer.DeleteForward(control));
                return true;
            case Keys.Enter:
                Edit(buffer => buffer.Insert("\n" + IndentOf(buffer)));
                return true;
            case Keys.Tab when !control:
                Edit(buffer => buffer.Insert("\t"));
                return true;
            case Keys.Escape:
                Buffer.MoveTo(Buffer.Caret, extend: false);
                CaretMoved(scroll: true);
                return true;
            case Keys.F6:
                SwitchSide();
                return true;
            case Keys.Left:
                MoveHorizontally(shift, control ? Buffer.WordLeft : Buffer.CharLeft, Buffer.SelectionStart);
                return true;
            case Keys.Right:
                MoveHorizontally(shift, control ? Buffer.WordRight : Buffer.CharRight, Buffer.SelectionEnd);
                return true;
            case Keys.Up when control:
                TopRow--;
                return true;
            case Keys.Down when control:
                TopRow++;
                return true;
            case Keys.Up:
                MoveVertically(-1, shift);
                return true;
            case Keys.Down:
                MoveVertically(1, shift);
                return true;
            case Keys.PageUp:
                MoveVertically(-VisibleRows, shift);
                return true;
            case Keys.PageDown:
                MoveVertically(VisibleRows, shift);
                return true;
            case Keys.Home when control:
                MoveTo(default, shift);
                return true;
            case Keys.End when control:
                MoveTo(Buffer.End, shift);
                return true;
            case Keys.Home:
                MoveTo(Buffer.Caret with { Column = HomeOf(Buffer) }, shift);
                return true;
            case Keys.End:
                MoveTo(Buffer.Caret with { Column = Buffer.Lines[Buffer.Caret.Line].Length }, shift);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Types a character, for the keyboard and for tests.</summary>
    internal void Type(char c)
    {
        Edit(buffer => buffer.Insert(c.ToString()));
    }

    protected override bool IsInputKey(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;

        return key is Keys.Up or Keys.Down or Keys.Left or Keys.Right or Keys.Tab or Keys.Enter or Keys.Escape
            || base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled && HandleKey(e.KeyData))
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);

        // Control characters are the keys handled above, or Ctrl+letters
        if (!e.Handled && e.KeyChar >= ' ' && e.KeyChar != (char)127)
        {
            Type(e.KeyChar);
            e.Handled = true;
        }
    }

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
        RestartBlink();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _blink.Stop();
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        int lines = SystemInformation.MouseWheelScrollLines * e.Delta / 120;

        if ((ModifierKeys & Keys.Shift) != 0)
        {
            _horizontal.Value = Math.Clamp(_horizontal.Value - lines * 2, 0, MaxLeftColumn);
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
            Press(e.Location, (ModifierKeys & Keys.Shift) != 0, e.Clicks == 2);
        }
        else if (e.Button == MouseButtons.Right && e.Y < TextArea.Height && e.X < TextArea.Right)
        {
            // The menu works on the side clicked; a click outside the selection moves the caret there
            DiffSide side = SideAt(e.Location);
            DiffPosition position = PositionAt(e.Location, side);
            DiffEditBuffer buffer = BufferOf(side);
            bool inSelection = side == _side && buffer.HasSelection
                && position >= buffer.SelectionStart && position <= buffer.SelectionEnd;

            if (!inSelection)
            {
                _side = side;
                buffer.MoveTo(position, extend: false);
                CaretMoved(scroll: false);
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Cursor = e.X >= TextArea.Right ? Cursors.Default : Cursors.IBeam;
        DragTo(e.Location);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        Release();
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
        PaintCaret(graphics, _side == DiffSide.Left ? left : right);
        PaintOverview(graphics, new Rectangle(area.Right, 0, OverviewWidth, area.Height));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _blink.Dispose();
            _menu.Dispose();
        }

        base.Dispose(disposing);
    }

    // The row each line of a side is in
    private static int[] RowsOfLines(IReadOnlyList<DiffRowModel> rows, DiffSide side, int lineCount)
    {
        var rowOf = new int[lineCount];
        Array.Fill(rowOf, -1);

        for (int row = 0; row < rows.Count; row++)
        {
            if (rows[row].On(side) is { } line && line.Number - 1 < lineCount)
            {
                rowOf[line.Number - 1] = row;
            }
        }

        return rowOf;
    }

    // The leading spaces of the caret's line, which a new line starts with
    private static string IndentOf(DiffEditBuffer buffer)
    {
        string line = buffer.Lines[buffer.SelectionStart.Line];
        int length = 0;

        while (length < line.Length && length < buffer.SelectionStart.Column && line[length] is ' ' or '\t')
        {
            length++;
        }

        return line[..length];
    }

    // Home goes to the first character that isn't a space, then to the line's start
    private static int HomeOf(DiffEditBuffer buffer)
    {
        string line = buffer.Lines[buffer.Caret.Line];
        int first = 0;

        while (first < line.Length && char.IsWhiteSpace(line[first]))
        {
            first++;
        }

        return buffer.Caret.Column == first ? 0 : first;
    }

    private void BuildMenu()
    {
        ToolStripMenuItem undo = MenuItem("&Undo", "Ctrl+Z", () => Edit(buffer => buffer.Undo()));
        ToolStripMenuItem redo = MenuItem("&Redo", "Ctrl+Y", () => Edit(buffer => buffer.Redo()));
        ToolStripMenuItem cut = MenuItem("Cu&t", "Ctrl+X", Cut);
        ToolStripMenuItem copy = MenuItem("&Copy", "Ctrl+C", Copy);
        ToolStripMenuItem paste = MenuItem("&Paste", "Ctrl+V", Paste);
        ToolStripMenuItem selectAll = MenuItem("Select &all", "Ctrl+A", () => HandleKey(Keys.Control | Keys.A));
        _menu.Items.AddRange(
            [undo, redo, new ToolStripSeparator(), cut, copy, paste, new ToolStripSeparator(), selectAll]);
        _menu.Opening += (_, _) =>
        {
            undo.Enabled = Buffer.CanUndo;
            redo.Enabled = Buffer.CanRedo;
            cut.Enabled = Buffer.HasSelection;
            copy.Enabled = Buffer.HasSelection;
            paste.Enabled = Clipboard.ContainsText();
        };
    }

    private static ToolStripMenuItem MenuItem(string text, string shortcut, Action click) =>
        new(text, null, (_, _) => click()) { ShortcutKeyDisplayString = shortcut };

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
        _horizontal.Maximum = Math.Max(0, _maxColumns + 1);
        _horizontal.Enabled = MaxLeftColumn > 0;
        _horizontal.Value = Math.Clamp(_horizontal.Value, 0, MaxLeftColumn);
    }

    private int RowOfLine(DiffSide side, int line)
    {
        int[] rowOf = side == DiffSide.Left ? _leftRowOf : _rightRowOf;

        // A line not in the rows yet, as on a side still empty: as far down as it is in its text
        return line < rowOf.Length && rowOf[line] >= 0 ? rowOf[line] : line;
    }

    /// <returns>The line of a side at a row; at a blank row, the next line below, or the end.</returns>
    private (int Line, bool IsInRow) LineAtRow(DiffSide side, int row)
    {
        DiffEditBuffer buffer = BufferOf(side);

        if (_rows.Count == 0)
        {
            return (Math.Clamp(row, 0, buffer.Lines.Count - 1), true);
        }

        row = Math.Clamp(row, 0, _rows.Count - 1);

        if (_rows[row].On(side) is { } line)
        {
            return (Math.Min(line.Number - 1, buffer.Lines.Count - 1), true);
        }

        for (int below = row + 1; below < _rows.Count; below++)
        {
            if (_rows[below].On(side) is { } next)
            {
                return (Math.Min(next.Number - 1, buffer.Lines.Count - 1), false);
            }
        }

        return (buffer.Lines.Count - 1, false);
    }

    private DiffPosition PositionAt(Point point) => PositionAt(point, SideAt(point));

    private DiffPosition PositionAt(Point point, DiffSide side)
    {
        DiffEditBuffer buffer = BufferOf(side);
        int row = TopRow + Math.Clamp(point.Y, 0, Math.Max(0, TextArea.Height - 1)) / _rowHeight;

        if (_rows.Count > 0 && row >= _rows.Count)
        {
            return buffer.End;
        }

        (int line, bool isInRow) = LineAtRow(side, row);

        if (!isInRow)
        {
            return new DiffPosition(line, 0);
        }

        // The character boundary nearest the point
        string text = buffer.Lines[line];
        int[] columns = ColumnsOf(text);
        float column = (point.X - TextLeftOf(side)) / _charWidth + _horizontal.Value;

        for (int i = 0; i < text.Length; i++)
        {
            if ((columns[i] + columns[i + 1]) / 2f > column)
            {
                return new DiffPosition(line, i);
            }
        }

        return new DiffPosition(line, text.Length);
    }

    private int TextLeftOf(DiffSide side) =>
        (side == DiffSide.Left ? 0 : SideWidth + DividerWidth) + GutterWidth + TextMargin;

    private void Edit(Func<DiffEditBuffer, bool> edit) => Edit(buffer => { edit(buffer); });

    private void Edit(Action<DiffEditBuffer> edit)
    {
        DiffEditBuffer buffer = Buffer;
        int version = buffer.Version;
        edit(buffer);

        // The rows come back compared again, before the caret is brought into view
        if (buffer.Version != version)
        {
            TextEdited?.Invoke(this, _side);
        }

        _preferredColumn = -1;
        CaretMoved(scroll: true);
    }

    private void Copy()
    {
        string text = Buffer.SelectedText;

        if (text.Length > 0)
        {
            Clipboard.SetText(text);
        }
    }

    private void Cut()
    {
        if (Buffer.HasSelection)
        {
            Copy();
            Edit(buffer => buffer.DeleteForward(word: false));
        }
    }

    private void Paste()
    {
        if (Clipboard.ContainsText())
        {
            string text = Clipboard.GetText();
            Edit(buffer => buffer.Insert(text));
        }
    }

    private void SwitchSide()
    {
        int row = RowOfLine(_side, Buffer.Caret.Line);
        _side = _side == DiffSide.Left ? DiffSide.Right : DiffSide.Left;
        Buffer.MoveTo(new DiffPosition(LineAtRow(_side, row).Line, 0), extend: false);
        CaretMoved(scroll: true);
    }

    private void MoveTo(DiffPosition position, bool extend)
    {
        Buffer.MoveTo(position, extend);
        _preferredColumn = -1;
        CaretMoved(scroll: true);
    }

    // Left or Right: by a character or a word; without Shift, a selection just ends at its side
    private void MoveHorizontally(bool extend, Func<DiffPosition, DiffPosition> step, DiffPosition selectionSide)
    {
        MoveTo(Buffer.HasSelection && !extend ? selectionSide : step(Buffer.Caret), extend);
    }

    // Up, Down, Page Up, Page Down: by lines of the side, keeping to the column where it started
    private void MoveVertically(int lines, bool extend)
    {
        DiffEditBuffer buffer = Buffer;
        DiffPosition caret = buffer.Caret;

        if (_preferredColumn < 0)
        {
            _preferredColumn = ColumnsOf(buffer.Lines[caret.Line])[caret.Column];
        }

        int line = Math.Clamp(caret.Line + lines, 0, buffer.Lines.Count - 1);
        string text = buffer.Lines[line];
        int[] columns = ColumnsOf(text);
        int column = 0;

        while (column < text.Length && columns[column + 1] <= _preferredColumn)
        {
            column++;
        }

        buffer.MoveTo(new DiffPosition(line, column), extend);
        CaretMoved(scroll: true);
    }

    private void CaretMoved(bool scroll)
    {
        DiffPosition caret = Buffer.Caret;
        _navigationRow = RowOfLine(_side, caret.Line);

        if (scroll)
        {
            if (_navigationRow < TopRow)
            {
                TopRow = _navigationRow;
            }
            else if (_navigationRow >= TopRow + VisibleRows)
            {
                TopRow = _navigationRow - VisibleRows + 1;
            }

            int column = ColumnsOf(Buffer.Lines[caret.Line])[caret.Column];

            if (column < _horizontal.Value)
            {
                _horizontal.Value = Math.Clamp(column - 4, 0, MaxLeftColumn);
            }
            else if (column >= _horizontal.Value + VisibleColumns)
            {
                _horizontal.Value = Math.Clamp(column - VisibleColumns + 4, 0, MaxLeftColumn);
            }
        }

        RestartBlink();
    }

    private void RestartBlink()
    {
        _isCaretOn = true;
        _blink.Stop();

        if (Focused)
        {
            _blink.Start();
        }

        Invalidate();
    }

    private void Blink()
    {
        _isCaretOn = !_isCaretOn;
        Invalidate();
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

        using (var backBrush = new SolidBrush(back))
        {
            graphics.FillRectangle(backBrush, side.X, y, side.Width, _rowHeight);
        }

        // Line number and +/-, in the gutter
        var gutter = new Rectangle(side.X, y, GutterWidth, _rowHeight);
        Color gutterBack = line is null ? _blankBack : !isChange ? _gutterBack : isLeft ? _removedGutter : _addedGutter;

        using (var gutterBrush = new SolidBrush(gutterBack))
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

        PaintSelection(graphics, text, y, which, line.Number - 1, columns);
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

    // The part of a line selected, with a bit past its end when the selection goes on to the next line
    private void PaintSelection(Graphics graphics, Rectangle text, int y, DiffSide side, int line, int[] columns)
    {
        DiffEditBuffer buffer = BufferOf(side);

        if (!buffer.HasSelection || line < buffer.SelectionStart.Line || line > buffer.SelectionEnd.Line)
        {
            return;
        }

        int length = columns.Length - 1;
        int start = line == buffer.SelectionStart.Line ? Math.Min(buffer.SelectionStart.Column, length) : 0;
        int end = line == buffer.SelectionEnd.Line ? Math.Min(buffer.SelectionEnd.Column, length) : length;
        float from = text.X + (columns[start] - _horizontal.Value) * _charWidth;
        float to = text.X + (columns[end] - _horizontal.Value) * _charWidth;

        if (line < buffer.SelectionEnd.Line)
        {
            to += _charWidth / 2;
        }

        using var brush = new SolidBrush(side == _side && Focused ? _selectionBack : _otherSelectionBack);
        graphics.FillRectangle(brush, from, y, Math.Max(0, to - from), _rowHeight);
    }

    private void PaintCaret(Graphics graphics, Rectangle side)
    {
        if (!Focused || !_isCaretOn)
        {
            return;
        }

        DiffPosition caret = Buffer.Caret;
        int row = RowOfLine(_side, caret.Line);

        if (row < TopRow || row >= TopRow + VisibleRows + 1)
        {
            return;
        }

        int column = ColumnsOf(Buffer.Lines[caret.Line])[caret.Column] - _horizontal.Value;
        int x = side.X + GutterWidth + TextMargin + (int)(column * _charWidth);

        if (x < side.X + GutterWidth || x > side.Right)
        {
            return;
        }

        using var brush = new SolidBrush(ForeColor);
        graphics.FillRectangle(brush, x - CaretWidth / 2, (row - TopRow) * _rowHeight, CaretWidth, _rowHeight);
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
