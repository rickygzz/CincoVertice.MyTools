using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Views;
using CincoVertice.MyTools.Ui.Win.Help.Models;
using CincoVertice.MyTools.Ui.Win.Help.Services;

namespace CincoVertice.MyTools.Ui.Win.Help;

/// <summary>
///     The user guide reader: on the left a search box over the pages, or the pages a search found; on the right the
///     page in a RichTextBox, with back and forward, its outline, and find in the page. Dark when Windows' apps are.
///     <see cref="Ui.Help.Presenters.HelpPresenter"/> decides what to show; the form shows it, and says what was
///     clicked.
/// </summary>
public partial class HelpForm : Form, IHelpView
{
    private const string BaseTitle = "My Tools help";

    // How many lines show above a text found by a search, for what it's in
    private const int LinesAboveFound = 3;

    // Where the links are in the text, and the headings
    private readonly List<(int Start, int Length, string Target)> _links = [];
    private readonly Dictionary<string, int> _anchors = [];

    // As Windows' apps are when the window opens
    private readonly bool _isDark;
    private readonly Font _boldFont;

    private MarkdownDocumentModel? _document;
    private string? _shownPage;

    // How wide the text box was when the page was laid out: tables are as wide as it
    private int _laidOutWidth;

    // While the list is filled, its changes are not the user's
    private bool _isUpdating;

    public HelpForm()
        : this(DarkMode.IsOn())
    {
    }

    /// <param name="isDark">Dark, whatever Windows' mode is; for the tests.</param>
    internal HelpForm(bool isDark)
    {
        _isDark = isDark;
        InitializeComponent();
        _boldFont = new Font(PagesListBox.Font, FontStyle.Bold);
        ActiveControl = PageTextBox;
        ApplyColors();
    }

    public event EventHandler<string>? LinkClicked;

    public event EventHandler<string>? PageChosen;

    public event EventHandler? BackRequested;

    public event EventHandler? ForwardRequested;

    public event EventHandler<string>? SearchRequested;

    public event EventHandler<HelpSearchResultModel>? SearchResultChosen;

    public event EventHandler? WindowClosing;

    /// <summary>Where each link of the page shown starts, in the text, how long it is, and where it goes.</summary>
    public IReadOnlyList<(int Start, int Length, string Target)> Links => _links;

    public int TopPosition => PageTextBox.GetCharIndexFromPosition(new Point(1, 1));

    public Rectangle WindowBounds => WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;

    public bool IsMaximized => WindowState == FormWindowState.Maximized;

    /// <summary>The page's text box, for the tests.</summary>
    internal RichTextBox Page => PageTextBox;

    /// <summary>What the list on the left shows, for the tests.</summary>
    internal IReadOnlyList<object> ListItems => [.. PagesListBox.Items.Cast<object>()];

    /// <returns>Where the heading starts in the text; null when the page has no such heading.</returns>
    public int? PositionOf(string anchor) => _anchors.TryGetValue(anchor, out int position) ? position : null;

    public void PlaceWindow(Rectangle bounds, bool isMaximized)
    {
        // Not on a monitor that's gone
        if (Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(bounds)))
        {
            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
        }

        if (isMaximized)
        {
            WindowState = FormWindowState.Maximized;
        }
    }

    public void ShowPages(IReadOnlyList<HelpPageModel> pages)
    {
        FillList(pages.Cast<object>());
        SelectShownPage();
    }

    public void ShowSearchResults(string text, IReadOnlyList<HelpSearchResultModel> results)
    {
        FillList(results.Count > 0 ? results.Cast<object>() : [$"No page has \"{text}\"."]);
    }

    public void ShowPage(HelpPageModel page, MarkdownDocumentModel document, string? anchor)
    {
        _document = document;
        _shownPage = page.Name;
        Text = page.Title.Length == 0 ? BaseTitle : $"{page.Title} - {BaseTitle}";
        SelectShownPage();
        ShowOutline(document);
        LayOut();
        ScrollTo(anchor);
    }

    public void ScrollTo(string? anchor)
    {
        ScrollToPosition(anchor is not null && _anchors.TryGetValue(anchor, out int position) ? position : 0);
    }

    public void ScrollToPosition(int position)
    {
        PageTextBox.Select(position, 0);
        ScrollLineToTop(position, 0);
    }

    public void HighlightText(string text)
    {
        FindTextBox.Text = text;
        Find(0);

        if (PageTextBox.SelectionLength > 0)
        {
            ScrollLineToTop(PageTextBox.SelectionStart, LinesAboveFound);
        }
    }

    public void ShowNavigation(bool canGoBack, bool canGoForward)
    {
        BackButton.Enabled = canGoBack;
        ForwardButton.Enabled = canGoForward;
    }

    public void OpenExternal(string url)
    {
        try
        {
            using var _ = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Win32Exception ex)
        {
            ShowProblem($"Could not open {url}: {ex.Message}");
        }
    }

    public void ShowProblem(string message)
    {
        MessageBox.Show(this, message, BaseTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>Windows draws the title bar and the scroll bars; dark, it's told so once they're there.</summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_isDark)
        {
            DarkMode.UseDarkTitleBar(Handle, true);
            DarkMode.UseDarkScrollBars(PageTextBox.Handle, true);
            DarkMode.UseDarkScrollBars(PagesListBox.Handle, true);
        }
    }

    /// <summary>
    ///     Tab and Shift+Tab in the page go from link to link, selecting it; Enter opens the one selected, or the one
    ///     the caret is in. Tab never reaches KeyDown: the form takes it to move the focus first.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (PageTextBox.Focused && _links.Count > 0)
        {
            switch (keyData)
            {
                case Keys.Tab:
                    SelectLink(forward: true);
                    return true;

                case Keys.Shift | Keys.Tab:
                    SelectLink(forward: false);
                    return true;

                case Keys.Enter when LinkAtCaret() is string target:
                    LinkClicked?.Invoke(this, target);
                    return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <returns>Where the link under the point goes; null when there's none.</returns>
    internal string? LinkAt(Point point)
    {
        if (_links.Count == 0)
        {
            return null;
        }

        int index = PageTextBox.GetCharIndexFromPosition(point);

        foreach ((int start, int length, string target) in _links)
        {
            if (index < start || index >= start + length)
            {
                continue;
            }

            // The nearest character can be past the end of the line, where nothing is
            Point left = PageTextBox.GetPositionFromCharIndex(index);
            Point next = PageTextBox.GetPositionFromCharIndex(index + 1);
            int right = next.Y == left.Y && next.X > left.X ? next.X : left.X + LogicalToDeviceUnits(12);

            return point.X >= left.X - 1 && point.X <= right && point.Y >= left.Y ? target : null;
        }

        return null;
    }

    // The next link after the selection, or the one before it, round to the other end of the page
    internal void SelectLink(bool forward)
    {
        // Past what's selected; with nothing selected, from the caret, so a page that starts with a link starts at it
        int caret = PageTextBox.SelectionStart;
        int end = caret + PageTextBox.SelectionLength;
        int index = forward
            ? _links.FindIndex(link => link.Start >= end)
            : _links.FindLastIndex(link => link.Start < caret);

        if (index < 0)
        {
            index = forward ? 0 : _links.Count - 1;
        }

        PageTextBox.Select(_links[index].Start, _links[index].Length);
        PageTextBox.ScrollToCaret();
    }

    internal string? LinkAtCaret()
    {
        int caret = PageTextBox.SelectionStart;

        foreach ((int start, int length, string target) in _links)
        {
            if (caret >= start && caret < start + length)
            {
                return target;
            }
        }

        return null;
    }

    private void ApplyColors()
    {
        if (!_isDark)
        {
            return;
        }

        Color page = MarkdownRtfWriter.BackgroundOf(true);
        Color side = Color.FromArgb(37, 37, 38);
        Color text = HelpDarkColorTable.Text;

        BackColor = HelpDarkColorTable.Background;
        ForeColor = text;
        Tools.Renderer = new ToolStripProfessionalRenderer(new HelpDarkColorTable()) { RoundedEdges = false };
        Tools.ForeColor = text;
        OutlineButton.DropDown.ForeColor = text;
        FindTextBox.BackColor = side;
        FindTextBox.ForeColor = text;
        Split.BackColor = HelpDarkColorTable.Background;
        Split.Panel1.BackColor = side;
        PagesListBox.BackColor = side;
        PagesListBox.ForeColor = text;
        SearchTextBox.BackColor = page;
        SearchTextBox.ForeColor = text;
        PagePanel.BackColor = page;
        PageTextBox.BackColor = page;
        PageTextBox.ForeColor = text;
    }

    private void FillList(IEnumerable<object> items)
    {
        _isUpdating = true;
        PagesListBox.BeginUpdate();
        PagesListBox.Items.Clear();
        PagesListBox.Items.AddRange(items.ToArray());
        PagesListBox.EndUpdate();
        _isUpdating = false;
    }

    // The page shown, when the list has it
    private void SelectShownPage()
    {
        _isUpdating = true;
        PagesListBox.SelectedItem = PagesListBox.Items.OfType<HelpPageModel>().FirstOrDefault(
            item => item.Name == _shownPage);
        _isUpdating = false;
    }

    // The page's headings, but the title, to go to
    private void ShowOutline(MarkdownDocumentModel document)
    {
        foreach (ToolStripItem item in OutlineButton.DropDownItems.Cast<ToolStripItem>().ToList())
        {
            item.Dispose();
        }

        IEnumerable<MarkdownBlockModel> headings = document.Blocks.Where(
            block => block.Kind == MarkdownBlockKind.Heading && block.Level is 2 or 3 && block.Anchor is not null);

        foreach (MarkdownBlockModel heading in headings)
        {
            string anchor = heading.Anchor!;

            // A level 3 heading in from its level 2; an & shown, not taken as the item's key
            string text = (heading.Level == 3 ? "      " : string.Empty)
                + heading.PlainText.Replace("&", "&&", StringComparison.Ordinal);
            var item = new ToolStripMenuItem(
                text,
                null,
                (_, _) => LinkClicked?.Invoke(this, "#" + anchor));

            if (_isDark)
            {
                item.ForeColor = HelpDarkColorTable.Text;
            }

            OutlineButton.DropDownItems.Add(item);
        }

        OutlineButton.Enabled = OutlineButton.DropDownItems.Count > 0;
    }

    // The page as RTF, as wide as the text box, then the marks taken out, kept as where the links and headings are
    private void LayOut()
    {
        if (_document is null)
        {
            return;
        }

        _laidOutWidth = PageTextBox.ClientSize.Width;

        // Some room on the right, as there is on the left
        int pixels = Math.Max(0, PageTextBox.ClientSize.Width - LogicalToDeviceUnits(16));
        HelpRtfModel page = MarkdownRtfWriter.Write(_document, pixels * 1440 / DeviceDpi, _isDark);

        // Drawn once, when it's all there
        SetRedraw(false);

        try
        {
            PageTextBox.ReadOnly = false;
            PageTextBox.Rtf = page.Rtf;
            TakeMarks(page.Marks);
            PageTextBox.ReadOnly = true;
            PageTextBox.Select(0, 0);
        }
        finally
        {
            SetRedraw(true);
        }
    }

    private void SetRedraw(bool isOn)
    {
        User32.SendMessage(PageTextBox.Handle, User32.WM_SETREDRAW, isOn ? 1 : 0, 0);

        if (isOn)
        {
            PageTextBox.Invalidate();
        }
    }

    private void TakeMarks(IReadOnlyList<HelpRtfMarkModel> marks)
    {
        _links.Clear();
        _anchors.Clear();
        string mark = MarkdownRtfWriter.Mark.ToString();
        int from = 0;

        foreach (HelpRtfMarkModel item in marks)
        {
            int position = PageTextBox.Find(mark, from, RichTextBoxFinds.MatchCase | RichTextBoxFinds.NoHighlight);

            if (position < 0)
            {
                break;
            }

            PageTextBox.Select(position, 1);
            PageTextBox.SelectedText = string.Empty;
            from = position;

            if (item.Link is not null)
            {
                _links.Add((position, item.Length, item.Link));
            }
            else if (item.Anchor is not null)
            {
                _anchors.TryAdd(item.Anchor, position);
            }
        }
    }

    // The line with a position at the top of the window, with some lines above it
    private void ScrollLineToTop(int position, int linesAbove)
    {
        int line = Math.Max(0, PageTextBox.GetLineFromCharIndex(position) - linesAbove);
        int top = (int)User32.SendMessage(PageTextBox.Handle, User32.EM_GETFIRSTVISIBLELINE, 0, 0);
        User32.SendMessage(PageTextBox.Handle, User32.EM_LINESCROLL, 0, line - top);
    }

    private void Find(int start)
    {
        string text = FindTextBox.Text;

        if (text.Length == 0)
        {
            return;
        }

        int position = start < PageTextBox.TextLength ? PageTextBox.Find(text, start, RichTextBoxFinds.None) : -1;

        // From the top again
        if (position < 0 && start > 0)
        {
            position = PageTextBox.Find(text, 0, RichTextBoxFinds.None);
        }

        if (position < 0)
        {
            SystemSounds.Beep.Play();

            return;
        }

        PageTextBox.ScrollToCaret();
    }

    private void FindNext() => Find(PageTextBox.SelectionStart + PageTextBox.SelectionLength);

    /// <summary>Esc clears what's being found or searched first; then it closes the window.</summary>
    /// <returns>Whether there was something to clear.</returns>
    internal bool ClearFindOrSearch()
    {
        if (SearchTextBox.Focused && SearchTextBox.TextLength > 0)
        {
            SearchTextBox.Clear();

            return true;
        }

        if (FindTextBox.TextLength > 0)
        {
            FindTextBox.Clear();
            PageTextBox.Select(PageTextBox.SelectionStart, 0);
            PageTextBox.Focus();

            return true;
        }

        return false;
    }

    private void Search()
    {
        SearchTimer.Stop();
        SearchRequested?.Invoke(this, SearchTextBox.Text);
    }

    private void BackButton_Click(object? sender, EventArgs e) => BackRequested?.Invoke(this, EventArgs.Empty);

    private void ForwardButton_Click(object? sender, EventArgs e) => ForwardRequested?.Invoke(this, EventArgs.Empty);

    private void FindNextButton_Click(object? sender, EventArgs e) => FindNext();

    private void FindTextBox_TextChanged(object? sender, EventArgs e) => Find(PageTextBox.SelectionStart);

    private void FindTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            FindNext();
            e.SuppressKeyPress = true;
        }
    }

    // Searched once the typing stops for a moment
    private void SearchTextBox_TextChanged(object? sender, EventArgs e)
    {
        SearchTimer.Stop();
        SearchTimer.Start();
    }

    private void SearchTimer_Tick(object? sender, EventArgs e) => Search();

    private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Enter:
                Search();
                break;

            // To the list, to go through what it found
            case Keys.Down when PagesListBox.Items.Count > 0:
                PagesListBox.Focus();
                break;

            default:
                return;
        }

        e.SuppressKeyPress = true;
    }

    private void PagesListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isUpdating)
        {
            return;
        }

        switch (PagesListBox.SelectedItem)
        {
            case HelpPageModel page:
                PageChosen?.Invoke(this, page.Name);
                break;

            case HelpSearchResultModel result:
                SearchResultChosen?.Invoke(this, result);
                break;
        }
    }

    // A page is a line; a page a search found is two: its title and how often, then the text around it
    private void PagesListBox_MeasureItem(object? sender, MeasureItemEventArgs e)
    {
        int line = PagesListBox.Font.Height;
        e.ItemHeight = PagesListBox.Items[e.Index] is HelpSearchResultModel ? (line * 2) + 10 : line + 6;
    }

    private void PagesListBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
        {
            return;
        }

        bool isSelected = (e.State & DrawItemState.Selected) != 0;
        Color selected = _isDark ? Color.FromArgb(0, 84, 153) : SystemColors.Highlight;
        Color back = isSelected ? selected : PagesListBox.BackColor;
        Color text = isSelected ? (_isDark ? Color.White : SystemColors.HighlightText) : PagesListBox.ForeColor;
        Color quiet = isSelected ? text : (_isDark ? Color.FromArgb(160, 160, 160) : SystemColors.GrayText);

        using (var brush = new SolidBrush(back))
        {
            e.Graphics.FillRectangle(brush, e.Bounds);
        }

        const TextFormatFlags Line =
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
        int line = PagesListBox.Font.Height;
        var first = new Rectangle(e.Bounds.Left + 4, e.Bounds.Top + 3, e.Bounds.Width - 8, line);

        switch (PagesListBox.Items[e.Index])
        {
            case HelpSearchResultModel result:
                string times = result.Count == 1 ? "once" : $"{result.Count} times";
                TextRenderer.DrawText(e.Graphics, $"{result.Title} ({times})", _boldFont, first, text, Line);
                first.Offset(0, line + 2);
                TextRenderer.DrawText(e.Graphics, result.Snippet, PagesListBox.Font, first, quiet, Line);
                break;

            case HelpPageModel page:
                TextRenderer.DrawText(e.Graphics, page.Title, PagesListBox.Font, first, text, Line);
                break;

            case string message:
                TextRenderer.DrawText(e.Graphics, message, PagesListBox.Font, first, quiet, Line);
                break;
        }

        e.DrawFocusRectangle();
    }

    private void PageTextBox_MouseMove(object? sender, MouseEventArgs e)
    {
        Cursor cursor = e.Button == MouseButtons.None && LinkAt(e.Location) is not null ? Cursors.Hand : Cursors.IBeam;

        if (PageTextBox.Cursor != cursor)
        {
            PageTextBox.Cursor = cursor;
        }
    }

    private void PageTextBox_MouseUp(object? sender, MouseEventArgs e)
    {
        switch (e.Button)
        {
            // A click, not the end of selecting text
            case MouseButtons.Left when PageTextBox.SelectionLength == 0 && LinkAt(e.Location) is string target:
                LinkClicked?.Invoke(this, target);
                break;

            case MouseButtons.XButton1:
                BackRequested?.Invoke(this, EventArgs.Empty);
                break;

            case MouseButtons.XButton2:
                ForwardRequested?.Invoke(this, EventArgs.Empty);
                break;
        }
    }

    // Laid out again once the size stops changing, the same text at the top
    private void PageTextBox_Resize(object? sender, EventArgs e)
    {
        if (_document is not null && PageTextBox.ClientSize.Width != _laidOutWidth)
        {
            ResizeTimer.Stop();
            ResizeTimer.Start();
        }
    }

    private void ResizeTimer_Tick(object? sender, EventArgs e)
    {
        ResizeTimer.Stop();
        int top = TopPosition;
        LayOut();
        ScrollToPosition(top);
    }

    private void HelpForm_KeyDown(object? sender, KeyEventArgs e)
    {
        bool isTyping = FindTextBox.Focused || SearchTextBox.Focused;

        switch (e.KeyData)
        {
            case Keys.Alt | Keys.Left or Keys.BrowserBack:
            case Keys.Back when !isTyping:
                BackRequested?.Invoke(this, EventArgs.Empty);
                break;

            case Keys.Alt | Keys.Right or Keys.BrowserForward:
                ForwardRequested?.Invoke(this, EventArgs.Empty);
                break;

            case Keys.Control | Keys.F:
                FindTextBox.Focus();
                FindTextBox.SelectAll();
                break;

            case Keys.Control | Keys.E:
                SearchTextBox.Focus();
                SearchTextBox.SelectAll();
                break;

            case Keys.F3:
                FindNext();
                break;

            case Keys.Escape:
                if (!ClearFindOrSearch())
                {
                    Close();
                }

                break;

            default:
                return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private void HelpForm_FormClosing(object? sender, FormClosingEventArgs e) =>
        WindowClosing?.Invoke(this, EventArgs.Empty);
}
