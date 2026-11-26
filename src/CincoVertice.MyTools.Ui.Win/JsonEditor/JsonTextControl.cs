using System.ComponentModel;
using System.Drawing.Drawing2D;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Services;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Services;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.Diff;

namespace CincoVertice.MyTools.Ui.Win.JsonEditor;

/// <summary>
///     The JSON editor's text: drawn here, line numbers and fold markers in its gutter, each piece in its color, the
///     search's matches marked, the first error underlined. A section collapses to its first line with a click on its
///     marker; a collapsed one shows how much it holds, and opens again with a click there or when the caret goes in.
///     Editing goes through a <see cref="DiffEditBuffer"/>, as Compare files' does.
/// </summary>
public sealed class JsonTextControl : Control
{
    public const string IndentUnit = "  ";

    private const int TextMargin = 6;
    private const int GutterPadding = 6;
    private const int CaretWidth = 2;

    private static readonly Dictionary<JsonTokenKind, Color> _lightColors = new()
    {
        [JsonTokenKind.Punctuation] = Color.FromArgb(60, 60, 60),
        [JsonTokenKind.Key] = Color.FromArgb(4, 81, 165),
        [JsonTokenKind.String] = Color.FromArgb(163, 21, 21),
        [JsonTokenKind.Number] = Color.FromArgb(9, 134, 88),
        [JsonTokenKind.Literal] = Color.FromArgb(0, 0, 255),
        [JsonTokenKind.Comment] = Color.FromArgb(0, 128, 0),
        [JsonTokenKind.Invalid] = Color.FromArgb(220, 40, 40),
    };

    private static readonly Dictionary<JsonTokenKind, Color> _darkColors = new()
    {
        [JsonTokenKind.Punctuation] = Color.FromArgb(212, 212, 212),
        [JsonTokenKind.Key] = Color.FromArgb(156, 220, 254),
        [JsonTokenKind.String] = Color.FromArgb(206, 145, 120),
        [JsonTokenKind.Number] = Color.FromArgb(181, 206, 168),
        [JsonTokenKind.Literal] = Color.FromArgb(86, 156, 214),
        [JsonTokenKind.Comment] = Color.FromArgb(106, 153, 85),
        [JsonTokenKind.Invalid] = Color.FromArgb(244, 71, 71),
    };

    // Set by ShowTheme: light at first
    private Dictionary<JsonTokenKind, Color> _colors = _lightColors;
    private Color _gutterBack;
    private Color _gutterFore;
    private Color _markerColor;
    private Color _selectionBack;
    private Color _matchBack;
    private Color _currentMatchBack;
    private Color _summaryBack;
    private Color _errorColor;

    private const TextFormatFlags TextFlags =
        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine | TextFormatFlags.NoClipping;

    private readonly VScrollBar _vertical = new() { Dock = DockStyle.Right, TabStop = false };
    private readonly HScrollBar _horizontal = new() { Dock = DockStyle.Bottom, TabStop = false };
    private readonly DiffEditBuffer _buffer = new();
    private readonly System.Windows.Forms.Timer _blink = new() { Interval = 530 };
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _copyPathItem = new() { Enabled = false };
    private readonly ToolStripMenuItem _copyValueItem = new("Copy &value");
    private readonly ToolStripMenuItem _copyNameAndValueItem = new("Copy &name and value");
    private readonly ToolStripMenuItem _sectionItem = new("Co&llapse section");

    private IReadOnlyList<IReadOnlyList<JsonTokenModel>> _tokens = [];
    private Dictionary<int, JsonFoldModel> _folds = [];
    private readonly HashSet<int> _collapsed = [];
    private DiffPosition? _errorAt;
    private IReadOnlyList<JsonMatchModel> _matches = [];
    private int _currentMatch = -1;

    // The lines showing, in order, and each line's row; -1 for a line in a collapsed section
    private List<int> _rows = [0];
    private int[] _rowOf = [0];

    private int _maxColumns;
    private int _rowHeight = 16;
    private float _charWidth = 8;
    private int _preferredColumn = -1;
    private bool _isCaretOn = true;
    private bool _isSelecting;
    private DiffPosition _menuPosition;
    private string? _menuPath;
    private string? _menuValue;
    private string? _menuNameAndValue;
    private Action _prepareMenu = () => { };

    public JsonTextControl()
    {
        SetStyle(
            ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw | ControlStyles.Selectable,
            true);
        ShowTheme(isDark: false);
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

    /// <summary>The user changed the text; <see cref="Lines"/> has it.</summary>
    public event EventHandler? TextEdited;

    public event EventHandler<DiffPosition>? CaretMoved;

    /// <summary>The menu opens on a right click: the path and the value there, answered in the request.</summary>
    public event EventHandler<JsonPathRequestModel>? PathRequested;

    public IReadOnlyList<string> Lines => _buffer.Lines;

    public DiffPosition Caret => _buffer.Caret;

    /// <summary>The first line showing.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int TopRow
    {
        get => _vertical.Value;
        set => _vertical.Value = Math.Clamp(value, 0, MaxTopRow);
    }

    internal IReadOnlyList<int> ShownLines => _rows;

    internal IReadOnlyCollection<int> Collapsed => _collapsed;

    private int VisibleRows => Math.Max(1, TextArea.Height / _rowHeight);

    private int MaxTopRow => Math.Max(0, _rows.Count - VisibleRows);

    private Rectangle TextArea => new(
        0,
        0,
        Math.Max(0, ClientSize.Width - _vertical.Width),
        Math.Max(0, ClientSize.Height - _horizontal.Height));

    private int NumberWidth => (int)Math.Ceiling(Math.Max(3, _buffer.Lines.Count.ToString().Length) * _charWidth);

    private int MarkerWidth => (int)Math.Ceiling(_charWidth * 1.6f);

    private int GutterWidth => NumberWidth + MarkerWidth + GutterPadding;

    private int TextLeft => GutterWidth + TextMargin;

    private int VisibleColumns => Math.Max(1, (int)((TextArea.Width - TextLeft) / _charWidth));

    private int MaxLeftColumn => Math.Max(0, _maxColumns + 2 - VisibleColumns);

    /// <summary>Shows a new text, with no undo before it, every section open.</summary>
    public void SetText(IReadOnlyList<string> lines)
    {
        _buffer.SetLines(lines.Count == 0 ? [string.Empty] : lines);
        _collapsed.Clear();
        _vertical.Value = 0;
        _horizontal.Value = 0;
        TextChangedHere();
        CaretChanged(scroll: true);
    }

    /// <summary>Replaces the whole text as one edit, which Undo takes back.</summary>
    public void ReplaceAll(string text)
    {
        Edit(buffer =>
        {
            buffer.SelectAll();
            buffer.Insert(text);
            buffer.MoveTo(default, extend: false);
        });
    }

    /// <summary>The text's colors, sections and first error, as read again after each change.</summary>
    public void ShowDocument(JsonDocumentModel document)
    {
        _tokens = document.Tokens;
        _folds = document.Folds
            .GroupBy(fold => fold.StartLine)
            .ToDictionary(group => group.Key, group => group.First());
        _collapsed.RemoveWhere(line => !_folds.ContainsKey(line));
        _errorAt = document.Error is null ? null : document.ErrorAt;
        RebuildRows();
        Invalidate();
    }

    public void ShowMatches(IReadOnlyList<JsonMatchModel> matches, int current)
    {
        _matches = matches;
        _currentMatch = current;
        Invalidate();
    }

    /// <summary>Selects a match and brings it into view, opening the sections it's in.</summary>
    public void Select(JsonMatchModel match)
    {
        DiffPosition start = _buffer.Clamp(match.Start);
        DiffPosition end = _buffer.Clamp(match.End);
        _buffer.Select(start, start.Line == end.Line ? end : start);
        CaretChanged(scroll: true);
    }

    /// <summary>The text's colors, its menu's, and its scroll bars', dark or light.</summary>
    public void ShowTheme(bool isDark)
    {
        IsDark = isDark;
        BackColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;
        ForeColor = isDark ? Color.FromArgb(212, 212, 212) : Color.Black;
        _colors = isDark ? _darkColors : _lightColors;
        _gutterBack = isDark ? Color.FromArgb(37, 37, 38) : Color.FromArgb(246, 246, 246);
        _gutterFore = isDark ? Color.FromArgb(133, 133, 133) : Color.FromArgb(130, 130, 130);
        _markerColor = isDark ? Color.FromArgb(170, 170, 170) : Color.FromArgb(110, 110, 110);
        _selectionBack = isDark ? Color.FromArgb(38, 79, 120) : Color.FromArgb(173, 214, 255);
        _matchBack = isDark ? Color.FromArgb(98, 82, 30) : Color.FromArgb(255, 236, 140);
        _currentMatchBack = isDark ? Color.FromArgb(160, 95, 20) : Color.FromArgb(255, 170, 60);
        _summaryBack = isDark ? Color.FromArgb(55, 55, 55) : Color.FromArgb(232, 232, 232);
        _errorColor = isDark ? Color.FromArgb(244, 71, 71) : Color.FromArgb(220, 40, 40);

        if (isDark)
        {
            _menu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable()) { RoundedEdges = false };
        }
        else
        {
            _menu.RenderMode = ToolStripRenderMode.ManagerRenderMode;
        }

        _menu.ForeColor = isDark ? DarkColorTable.Text : SystemColors.ControlText;

        if (IsHandleCreated)
        {
            DarkMode.UseDarkScrollBars(_vertical.Handle, isDark);
            DarkMode.UseDarkScrollBars(_horizontal.Handle, isDark);
        }

        Invalidate();
    }

    /// <summary>Whether <see cref="ShowTheme"/> made it dark.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsDark { get; private set; }

    /// <summary>The menu as a right click there makes it, for tests: each item enabled, by its text.</summary>
    internal IReadOnlyList<string> MenuAt(DiffPosition position)
    {
        _menuPosition = position;
        _prepareMenu();

        return [.. _menu.Items.OfType<ToolStripMenuItem>().Where(item => item.Enabled).Select(item => item.Text!)];
    }

    /// <summary>Chooses an item of the menu <see cref="MenuAt"/> made, by its text.</summary>
    internal void ClickMenu(string text) =>
        _menu.Items.OfType<ToolStripMenuItem>().Single(item => item.Text == text).PerformClick();

    /// <summary>Collapses the section the line is in, or opens it when it's collapsed, as the menu does.</summary>
    internal void ToggleSectionAt(int line)
    {
        if (IsCollapsedStart(line))
        {
            ExpandSectionAt(line);
        }
        else
        {
            CollapseSectionAt(line);
        }
    }

    /// <summary>Collapses the innermost open section the line is in (Ctrl+-); again, the one around it.</summary>
    public void CollapseSectionAt(int line)
    {
        if (JsonSections.OpenSectionAt(_folds.Values, _collapsed, line) is JsonFoldModel section)
        {
            Toggle(section.StartLine);
        }
    }

    /// <summary>Opens the collapsed section starting on the line (Ctrl++); nothing for another line.</summary>
    public void ExpandSectionAt(int line)
    {
        if (IsCollapsedStart(line))
        {
            Toggle(line);
        }
    }

    private bool IsCollapsedStart(int line) => _collapsed.Contains(line) && _folds.ContainsKey(line);

    public void CollapseAll()
    {
        // The outermost section stays open, or there would be only its first line
        int outermost = _folds.Count == 0 ? -1 : _folds.Values.MaxBy(fold => fold.EndLine - fold.StartLine)!.StartLine;
        _collapsed.UnionWith(_folds.Keys.Where(line => line != outermost));
        OpenAt(_buffer.Caret.Line);
        RebuildRows();
        Invalidate();
    }

    public void ExpandAll()
    {
        _collapsed.Clear();
        RebuildRows();
        Invalidate();
    }

    /// <summary>Collapses or opens the section starting on a line, as a click on its marker does.</summary>
    internal void Toggle(int line)
    {
        if (!_folds.ContainsKey(line) || !_collapsed.Add(line))
        {
            _collapsed.Remove(line);
        }

        if (_rowOf.Length > _buffer.Caret.Line && IsInside(_buffer.Caret.Line, line))
        {
            _buffer.MoveTo(new DiffPosition(line, _buffer.Lines[line].Length), extend: false);
        }

        RebuildRows();
        Invalidate();
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
            case Keys.OemMinus or Keys.Subtract when control && shift:
                CollapseAll();
                return true;
            case Keys.Oemplus or Keys.Add when control && shift:
                ExpandAll();
                return true;
            case Keys.OemMinus or Keys.Subtract when control:
                CollapseSectionAt(_buffer.Caret.Line);
                return true;
            case Keys.Oemplus or Keys.Add when control:
                ExpandSectionAt(_buffer.Caret.Line);
                return true;
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
                _buffer.SelectAll();
                CaretChanged(scroll: false);
                return true;
            case Keys.Back:
                Edit(buffer => buffer.DeleteBackward(control));
                return true;
            case Keys.Delete:
                Edit(buffer => buffer.DeleteForward(control));
                return true;
            case Keys.Enter:
                Edit(buffer => buffer.Insert("\n" + IndentAt(buffer)));
                return true;
            case Keys.Tab when !control:
                Edit(buffer => buffer.Insert(IndentUnit));
                return true;
            case Keys.Left:
                MoveTo(_buffer.HasSelection && !shift ? _buffer.SelectionStart
                    : control ? _buffer.WordLeft(_buffer.Caret) : _buffer.CharLeft(_buffer.Caret), shift);
                return true;
            case Keys.Right:
                MoveTo(_buffer.HasSelection && !shift ? _buffer.SelectionEnd
                    : control ? _buffer.WordRight(_buffer.Caret) : _buffer.CharRight(_buffer.Caret), shift);
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
                MoveTo(_buffer.End, shift);
                return true;
            case Keys.Home:
                DiffPosition caret = _buffer.Caret;
                MoveTo(caret with { Column = HomeOf(_buffer.Lines[caret.Line], caret.Column) }, shift);
                return true;
            case Keys.End:
                MoveTo(_buffer.Caret with { Column = _buffer.Lines[_buffer.Caret.Line].Length }, shift);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Types a character, for the keyboard and for tests.</summary>
    internal void Type(char c) => Edit(buffer => buffer.Insert(c.ToString()));

    /// <summary>A press of the left button: on a marker or a summary, the section; else the caret goes there.</summary>
    internal void Press(Point point, bool extend, bool isDouble = false)
    {
        if (point.Y >= TextArea.Height || point.X >= TextArea.Width)
        {
            return;
        }

        int row = TopRow + point.Y / _rowHeight;

        if (row < _rows.Count)
        {
            int line = _rows[row];

            if (_folds.ContainsKey(line)
                && ((point.X >= NumberWidth && point.X < GutterWidth) || IsOnSummary(line, point.X)))
            {
                Toggle(line);

                return;
            }
        }

        DiffPosition position = PositionAt(point);

        if (isDouble)
        {
            _buffer.SelectWord(position);
        }
        else
        {
            _buffer.MoveTo(position, extend);
            _isSelecting = true;
        }

        _preferredColumn = -1;
        CaretChanged(scroll: false);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        Keys key = keyData & Keys.KeyCode;

        return key is Keys.Up or Keys.Down or Keys.Left or Keys.Right or Keys.Tab or Keys.Enter
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
        else if (e.Button == MouseButtons.Right && e.Y < TextArea.Height && e.X < TextArea.Width)
        {
            // The menu works where the click was; a click outside the selection moves the caret there
            DiffPosition position = PositionAt(e.Location);
            _menuPosition = position;

            if (!_buffer.HasSelection || position < _buffer.SelectionStart || position > _buffer.SelectionEnd)
            {
                _buffer.MoveTo(position, extend: false);
                CaretChanged(scroll: false);
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Cursor = e.X < GutterWidth || e.X >= TextArea.Width ? Cursors.Default : Cursors.IBeam;

        if (!_isSelecting)
        {
            return;
        }

        // Beyond the top or bottom, the rows scroll on
        if (e.Y < 0)
        {
            TopRow--;
        }
        else if (e.Y >= TextArea.Height)
        {
            TopRow++;
        }

        _buffer.MoveTo(PositionAt(e.Location with { X = Math.Max(e.X, TextLeft) }), extend: true);
        CaretChanged(scroll: false);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _isSelecting = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;
        graphics.Clear(BackColor);
        Rectangle area = TextArea;

        using (var gutter = new SolidBrush(_gutterBack))
        {
            graphics.FillRectangle(gutter, 0, 0, GutterWidth, area.Height);
        }

        int last = Math.Min(_rows.Count, TopRow + VisibleRows + 1);

        for (int row = TopRow; row < last; row++)
        {
            PaintLine(graphics, (row - TopRow) * _rowHeight, _rows[row]);
        }

        PaintCaret(graphics);
    }

    /// <summary>Windows draws the scroll bars; dark, they're told so once they're there.</summary>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        if (IsDark)
        {
            DarkMode.UseDarkScrollBars(_vertical.Handle, isDark: true);
            DarkMode.UseDarkScrollBars(_horizontal.Handle, isDark: true);
        }
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

    // The indentation of the line, and a step more after an opening bracket
    private static string IndentAt(DiffEditBuffer buffer)
    {
        DiffPosition at = buffer.SelectionStart;
        string line = buffer.Lines[at.Line];
        int length = 0;

        while (length < line.Length && length < at.Column && line[length] is ' ' or '\t')
        {
            length++;
        }

        string before = line[..at.Column].TrimEnd();

        return line[..length] + (before.EndsWith('{') || before.EndsWith('[') ? IndentUnit : string.Empty);
    }

    // Home goes to the first character that isn't a space, then to the line's start
    private static int HomeOf(string line, int column)
    {
        int first = 0;

        while (first < line.Length && char.IsWhiteSpace(line[first]))
        {
            first++;
        }

        return column == first ? 0 : first;
    }

    private void BuildMenu()
    {
        ToolStripMenuItem undo = MenuItem("&Undo", "Ctrl+Z", () => Edit(buffer => buffer.Undo()));
        ToolStripMenuItem redo = MenuItem("&Redo", "Ctrl+Y", () => Edit(buffer => buffer.Redo()));
        ToolStripMenuItem cut = MenuItem("Cu&t", "Ctrl+X", Cut);
        ToolStripMenuItem copy = MenuItem("&Copy", "Ctrl+C", Copy);
        ToolStripMenuItem paste = MenuItem("&Paste", "Ctrl+V", Paste);
        ToolStripMenuItem collapseAll = MenuItem("Collapse &all", "Ctrl+Shift+-", CollapseAll);
        ToolStripMenuItem expandAll = MenuItem("&Expand all", "Ctrl+Shift++", ExpandAll);
        _copyPathItem.Click += (_, _) => CopyText(_menuPath);
        _copyValueItem.Click += (_, _) => CopyText(_menuValue);
        _copyNameAndValueItem.Click += (_, _) => CopyText(_menuNameAndValue);
        _sectionItem.Click += (_, _) => ToggleSectionAt(_menuPosition.Line);
        _menu.Items.AddRange(
        [
            _copyPathItem, _copyValueItem, _copyNameAndValueItem, new ToolStripSeparator(),
            _sectionItem, collapseAll, expandAll, new ToolStripSeparator(),
            undo, redo, new ToolStripSeparator(), cut, copy, paste,
        ]);
        _prepareMenu = () =>
        {
            // A click in the selection works on the values it holds; one outside it has cleared it
            var request = new JsonPathRequestModel(
                _menuPosition,
                _buffer.HasSelection ? (_buffer.SelectionStart, _buffer.SelectionEnd) : null);
            PathRequested?.Invoke(this, request);
            _menuPath = request.Path;
            _menuValue = request.Value;
            _menuNameAndValue = request.NameAndValue;
            _copyPathItem.Text = request.Path is null ? "Copy JMESPath" : $"Copy JMESPath: {Shortened(request.Path)}";
            _copyPathItem.Enabled = request.Path is not null;
            _copyValueItem.Text = request.ValueCount > 1 ? $"Copy &values ({request.ValueCount})" : "Copy &value";
            _copyValueItem.Enabled = request.Value is not null;
            _copyNameAndValueItem.Text = request.NameAndValueCount > 1
                ? $"Copy &names and values ({request.NameAndValueCount})"
                : "Copy &name and value";
            _copyNameAndValueItem.Enabled = request.NameAndValue is not null;

            int line = _menuPosition.Line;
            bool isCollapsed = _collapsed.Contains(line) && _folds.ContainsKey(line);
            JsonFoldModel? section = isCollapsed
                ? _folds[line]
                : JsonSections.OpenSectionAt(_folds.Values, _collapsed, line);
            _sectionItem.Text = isCollapsed ? "E&xpand section" : "Co&llapse section";
            _sectionItem.ShortcutKeyDisplayString = isCollapsed ? "Ctrl++" : "Ctrl+-";
            _sectionItem.Enabled = section is not null;
            collapseAll.Enabled = _folds.Count > 0;
            expandAll.Enabled = _collapsed.Count > 0;
            undo.Enabled = _buffer.CanUndo;
            redo.Enabled = _buffer.CanRedo;
            cut.Enabled = _buffer.HasSelection;
            copy.Enabled = _buffer.HasSelection;
            paste.Enabled = Clipboard.ContainsText();
        };
        _menu.Opening += (_, _) => _prepareMenu();
    }

    private static string Shortened(string path) => path.Length <= 60 ? path : path[..59] + "…";

    private static ToolStripMenuItem MenuItem(string text, string shortcut, Action click) =>
        new(text, null, (_, _) => click()) { ShortcutKeyDisplayString = shortcut };

    private static void CopyText(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Clipboard.SetText(text);
        }
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
        _horizontal.Maximum = Math.Max(0, _maxColumns + 1);
        _horizontal.Enabled = MaxLeftColumn > 0;
        _horizontal.Value = Math.Clamp(_horizontal.Value, 0, MaxLeftColumn);
    }

    private void TextChangedHere()
    {
        _maxColumns = _buffer.Lines.Count == 0 ? 0 : _buffer.Lines.Max(DiffTextControl.WidthOf);
        RebuildRows();
    }

    // The lines showing: all but those inside a collapsed section, its last one included
    private void RebuildRows()
    {
        int count = _buffer.Lines.Count;
        _rowOf = new int[count];
        _rows = new List<int>(count);
        int hiddenUntil = -1;

        for (int line = 0; line < count; line++)
        {
            if (line <= hiddenUntil)
            {
                _rowOf[line] = -1;

                continue;
            }

            _rowOf[line] = _rows.Count;
            _rows.Add(line);

            if (_collapsed.Contains(line) && _folds.TryGetValue(line, out JsonFoldModel? fold))
            {
                hiddenUntil = Math.Min(fold.EndLine, count - 1);
            }
        }

        UpdateScrollBars();
    }

    private bool IsInside(int line, int foldStart) =>
        _folds.TryGetValue(foldStart, out JsonFoldModel? fold) && line > foldStart && line <= fold.EndLine;

    // The caret in a collapsed section opens it, and those around it
    private void OpenAt(int line)
    {
        _collapsed.RemoveWhere(start => IsInside(line, start));
    }

    private bool IsOnSummary(int line, int x)
    {
        if (!_collapsed.Contains(line))
        {
            return false;
        }

        float start = TextLeft + (DiffTextControl.WidthOf(_buffer.Lines[line]) + 1 - _horizontal.Value) * _charWidth;

        return x >= start;
    }

    private DiffPosition PositionAt(Point point)
    {
        int y = Math.Clamp(point.Y, 0, Math.Max(0, TextArea.Height - 1));
        int row = Math.Clamp(TopRow + y / _rowHeight, 0, _rows.Count - 1);
        int line = _rows.Count == 0 ? 0 : _rows[row];
        string text = _buffer.Lines[line];
        int[] columns = DiffTextControl.ColumnsOf(text);
        float column = (point.X - TextLeft) / _charWidth + _horizontal.Value;

        for (int i = 0; i < text.Length; i++)
        {
            if ((columns[i] + columns[i + 1]) / 2f > column)
            {
                return new DiffPosition(line, i);
            }
        }

        return new DiffPosition(line, text.Length);
    }

    private void Edit(Func<DiffEditBuffer, bool> edit) => Edit(buffer => { edit(buffer); });

    private void Edit(Action<DiffEditBuffer> edit)
    {
        int version = _buffer.Version;
        int linesBefore = _buffer.Lines.Count;
        int editLine = _buffer.SelectionStart.Line;
        edit(_buffer);

        if (_buffer.Version != version)
        {
            // Sections below the edit move with their lines
            int moved = _buffer.Lines.Count - linesBefore;

            if (moved != 0)
            {
                List<int> shifted = [.. _collapsed.Where(line => line > editLine).Select(line => line + moved)];
                _collapsed.RemoveWhere(line => line > editLine);
                _collapsed.UnionWith(shifted.Where(line => line >= 0));
            }

            TextChangedHere();
            TextEdited?.Invoke(this, EventArgs.Empty);
        }

        _preferredColumn = -1;
        CaretChanged(scroll: true);
    }

    private void Copy() => CopyText(_buffer.SelectedText);

    private void Cut()
    {
        if (_buffer.HasSelection)
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

    private void MoveTo(DiffPosition position, bool extend)
    {
        _buffer.MoveTo(position, extend);
        _preferredColumn = -1;
        CaretChanged(scroll: true);
    }

    // Up, Down, Page Up, Page Down: by the lines showing, keeping to the column where it started
    private void MoveVertically(int rows, bool extend)
    {
        DiffPosition caret = _buffer.Caret;

        if (_preferredColumn < 0)
        {
            _preferredColumn = DiffTextControl.ColumnsOf(_buffer.Lines[caret.Line])[caret.Column];
        }

        int row = Math.Clamp(RowOf(caret.Line) + rows, 0, _rows.Count - 1);
        int line = _rows[row];
        string text = _buffer.Lines[line];
        int[] columns = DiffTextControl.ColumnsOf(text);
        int column = 0;

        while (column < text.Length && columns[column + 1] <= _preferredColumn)
        {
            column++;
        }

        _buffer.MoveTo(new DiffPosition(line, column), extend);
        CaretChanged(scroll: true);
    }

    private int RowOf(int line) =>
        line < _rowOf.Length && _rowOf[line] >= 0 ? _rowOf[line] : Math.Min(line, _rows.Count - 1);

    private void CaretChanged(bool scroll)
    {
        DiffPosition caret = _buffer.Caret;

        if (caret.Line < _rowOf.Length && _rowOf[caret.Line] < 0)
        {
            OpenAt(caret.Line);
            RebuildRows();
        }

        if (scroll)
        {
            int row = RowOf(caret.Line);

            if (row < TopRow)
            {
                TopRow = row;
            }
            else if (row >= TopRow + VisibleRows)
            {
                TopRow = row - VisibleRows + 1;
            }

            int column = DiffTextControl.ColumnsOf(_buffer.Lines[caret.Line])[caret.Column];

            if (column < _horizontal.Value)
            {
                _horizontal.Value = Math.Clamp(column - 4, 0, MaxLeftColumn);
            }
            else if (column >= _horizontal.Value + VisibleColumns)
            {
                _horizontal.Value = Math.Clamp(column - VisibleColumns + 4, 0, MaxLeftColumn);
            }
        }

        CaretMoved?.Invoke(this, caret);
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

    private void PaintLine(Graphics graphics, int y, int line)
    {
        // Number, and the marker of a section starting here: ▸ collapsed, ▾ open
        var number = new Rectangle(0, y + 1, NumberWidth, _rowHeight);
        string lineNumber = (line + 1).ToString();
        TextRenderer.DrawText(graphics, lineNumber, Font, number, _gutterFore, TextFlags | TextFormatFlags.Right);

        if (_folds.ContainsKey(line))
        {
            var marker = new Rectangle(NumberWidth + 2, y, MarkerWidth - 2, _rowHeight);
            PaintMarker(graphics, marker, _collapsed.Contains(line));
        }

        string text = _buffer.Lines[line];
        int[] columns = DiffTextControl.ColumnsOf(text);
        var area = Rectangle.FromLTRB(TextLeft, y, TextArea.Width, y + _rowHeight);
        GraphicsState state = graphics.Save();
        graphics.SetClip(Rectangle.FromLTRB(GutterWidth, y, TextArea.Width, y + _rowHeight));

        PaintMatches(graphics, area, line, columns);
        PaintSelection(graphics, area, line, columns);
        PaintTokens(graphics, area, line, text, columns);
        PaintError(graphics, area, line, columns);

        if (_collapsed.Contains(line) && _folds.TryGetValue(line, out JsonFoldModel? fold))
        {
            float x = area.X + (columns[^1] + 1 - _horizontal.Value) * _charWidth;
            Size size = TextRenderer.MeasureText(fold.Summary, Font, Size.Empty, TextFlags);

            using (var back = new SolidBrush(_summaryBack))
            {
                graphics.FillRectangle(back, x - 2, y + 1, size.Width + 4, _rowHeight - 2);
            }

            TextRenderer.DrawText(graphics, fold.Summary, Font, new Point((int)x, y + 1), _gutterFore, TextFlags);
        }

        graphics.Restore(state);
    }

    private void PaintMarker(Graphics graphics, Rectangle box, bool isCollapsed)
    {
        float size = Math.Min(box.Width, box.Height) * 0.45f;
        float cx = box.X + box.Width / 2f;
        float cy = box.Y + box.Height / 2f;
        PointF[] triangle = isCollapsed
            ? [new(cx - size / 2, cy - size / 2 - 1), new(cx + size / 2, cy), new(cx - size / 2, cy + size / 2 + 1)]
            : [new(cx - size / 2 - 1, cy - size / 3), new(cx + size / 2 + 1, cy - size / 3), new(cx, cy + size / 2)];
        SmoothingMode smoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using (var brush = new SolidBrush(_markerColor))
        {
            graphics.FillPolygon(brush, triangle);
        }

        graphics.SmoothingMode = smoothing;
    }

    private void PaintTokens(Graphics graphics, Rectangle area, int line, string text, int[] columns)
    {
        IReadOnlyList<JsonTokenModel> tokens = line < _tokens.Count ? _tokens[line] : [];
        int first = _horizontal.Value;

        // Text not read yet, as just typed: plain until the document comes back
        if (tokens.Count == 0 && text.Trim().Length > 0)
        {
            DrawPart(graphics, area, DiffTextControl.Expanded(text), 0, first, ForeColor);

            return;
        }

        foreach (JsonTokenModel token in tokens)
        {
            if (token.Column + token.Length > text.Length)
            {
                continue;
            }

            int start = columns[token.Column];
            int end = columns[token.Column + token.Length];

            if (end <= first || start - first > VisibleColumns + 2)
            {
                continue;
            }

            string part = DiffTextControl.Expanded(text.Substring(token.Column, token.Length));
            DrawPart(graphics, area, part, start, first, _colors[token.Kind]);
        }
    }

    private void DrawPart(Graphics graphics, Rectangle area, string part, int startColumn, int firstColumn, Color color)
    {
        if (startColumn < firstColumn)
        {
            part = part[Math.Min(part.Length, firstColumn - startColumn)..];
            startColumn = firstColumn;
        }

        int x = area.X + (int)((startColumn - firstColumn) * _charWidth);
        TextRenderer.DrawText(graphics, part, Font, new Point(x, area.Y + 1), color, TextFlags);
    }

    private void PaintMatches(Graphics graphics, Rectangle area, int line, int[] columns)
    {
        for (int i = 0; i < _matches.Count; i++)
        {
            JsonMatchModel match = _matches[i];

            if (line < match.Start.Line || line > match.End.Line)
            {
                continue;
            }

            int length = columns.Length - 1;
            int start = line == match.Start.Line ? Math.Min(match.Start.Column, length) : 0;
            int end = line == match.End.Line ? Math.Min(match.End.Column, length) : length;
            float from = area.X + (columns[start] - _horizontal.Value) * _charWidth;
            float to = area.X + (columns[end] - _horizontal.Value) * _charWidth;

            using var brush = new SolidBrush(i == _currentMatch ? _currentMatchBack : _matchBack);
            graphics.FillRectangle(brush, from, area.Y, Math.Max(2, to - from), _rowHeight);
        }
    }

    // The part of a line selected, with a bit past its end when the selection goes on to the next line
    private void PaintSelection(Graphics graphics, Rectangle area, int line, int[] columns)
    {
        if (!_buffer.HasSelection || line < _buffer.SelectionStart.Line || line > _buffer.SelectionEnd.Line)
        {
            return;
        }

        int length = columns.Length - 1;
        int start = line == _buffer.SelectionStart.Line ? Math.Min(_buffer.SelectionStart.Column, length) : 0;
        int end = line == _buffer.SelectionEnd.Line ? Math.Min(_buffer.SelectionEnd.Column, length) : length;
        float from = area.X + (columns[start] - _horizontal.Value) * _charWidth;
        float to = area.X + (columns[end] - _horizontal.Value) * _charWidth;

        if (line < _buffer.SelectionEnd.Line)
        {
            to += _charWidth / 2;
        }

        using var brush = new SolidBrush(_selectionBack);
        graphics.FillRectangle(brush, from, area.Y, Math.Max(0, to - from), _rowHeight);
    }

    // A wavy line under the first thing wrong, a few characters long
    private void PaintError(Graphics graphics, Rectangle area, int line, int[] columns)
    {
        if (_errorAt is not DiffPosition at || at.Line != line)
        {
            return;
        }

        int length = columns.Length - 1;
        int start = Math.Min(at.Column, length);
        float from = area.X + (columns[start] - _horizontal.Value) * _charWidth;
        float to = from + Math.Max(3, Math.Min(4, length - start)) * _charWidth;
        float bottom = area.Y + _rowHeight - 2;
        List<PointF> wave = [];

        for (float x = from; x <= to; x += 2)
        {
            wave.Add(new PointF(x, bottom - ((int)((x - from) / 2) % 2 == 0 ? 0 : 2)));
        }

        if (wave.Count > 1)
        {
            using var pen = new Pen(_errorColor);
            graphics.DrawLines(pen, [.. wave]);
        }
    }

    private void PaintCaret(Graphics graphics)
    {
        if (!Focused || !_isCaretOn)
        {
            return;
        }

        DiffPosition caret = _buffer.Caret;
        int row = RowOf(caret.Line);

        if (row < TopRow || row >= TopRow + VisibleRows + 1)
        {
            return;
        }

        int column = DiffTextControl.ColumnsOf(_buffer.Lines[caret.Line])[caret.Column] - _horizontal.Value;
        int x = TextLeft + (int)(column * _charWidth);

        if (x < GutterWidth || x > TextArea.Width)
        {
            return;
        }

        using var brush = new SolidBrush(ForeColor);
        graphics.FillRectangle(brush, x - CaretWidth / 2, (row - TopRow) * _rowHeight, CaretWidth, _rowHeight);
    }
}
