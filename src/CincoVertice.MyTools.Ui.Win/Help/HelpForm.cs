using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Views;
using CincoVertice.MyTools.Ui.Win.Help.Models;
using CincoVertice.MyTools.Ui.Win.Help.Services;

namespace CincoVertice.MyTools.Ui.Win.Help;

/// <summary>
///     The user guide reader: the pages on the left, the page on the right in a RichTextBox, back and forward, and
///     find in the page. <see cref="Ui.Help.Presenters.HelpPresenter"/> decides which page; the form shows it, and says
///     which link was clicked.
/// </summary>
public partial class HelpForm : Form, IHelpView
{
    private const string BaseTitle = "My Tools help";

    // Where the links are in the text, and the headings
    private readonly List<(int Start, int Length, string Target)> _links = [];
    private readonly Dictionary<string, int> _anchors = [];

    private MarkdownDocumentModel? _document;

    // How wide the text box was when the page was laid out: tables are as wide as it
    private int _laidOutWidth;

    // While the list is filled, its changes are not the user's
    private bool _isUpdating;

    public HelpForm()
    {
        InitializeComponent();
        ActiveControl = PageTextBox;
    }

    public event EventHandler<string>? LinkClicked;

    public event EventHandler<string>? PageChosen;

    public event EventHandler? BackRequested;

    public event EventHandler? ForwardRequested;

    /// <summary>Where each link of the page shown starts, in the text, how long it is, and where it goes.</summary>
    public IReadOnlyList<(int Start, int Length, string Target)> Links => _links;

    public int TopPosition => PageTextBox.GetCharIndexFromPosition(new Point(1, 1));

    /// <returns>Where the heading starts in the text; null when the page has no such heading.</returns>
    public int? PositionOf(string anchor) => _anchors.TryGetValue(anchor, out int position) ? position : null;

    public void ShowPages(IReadOnlyList<HelpPageModel> pages)
    {
        _isUpdating = true;
        PagesListBox.BeginUpdate();
        PagesListBox.Items.Clear();

        foreach (HelpPageModel page in pages)
        {
            PagesListBox.Items.Add(page);
        }

        PagesListBox.EndUpdate();
        _isUpdating = false;
    }

    public void ShowPage(HelpPageModel page, MarkdownDocumentModel document, string? anchor)
    {
        _document = document;
        Text = page.Title.Length == 0 ? BaseTitle : $"{page.Title} - {BaseTitle}";

        _isUpdating = true;
        PagesListBox.SelectedItem = PagesListBox.Items.Cast<HelpPageModel>().FirstOrDefault(
            item => item.Name == page.Name);
        _isUpdating = false;

        LayOut();
        ScrollTo(anchor);
    }

    public void ScrollTo(string? anchor)
    {
        ScrollToPosition(anchor is not null && _anchors.TryGetValue(anchor, out int position) ? position : 0);
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
        HelpRtfModel page = MarkdownRtfWriter.Write(_document, pixels * 1440 / DeviceDpi);

        PageTextBox.ReadOnly = false;
        PageTextBox.Rtf = page.Rtf;
        TakeMarks(page.Marks);
        PageTextBox.ReadOnly = true;
        PageTextBox.Select(0, 0);
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

    // At the top of the window: to the end first, or ScrollToCaret would leave it at the bottom
    public void ScrollToPosition(int position)
    {
        if (position > 0)
        {
            PageTextBox.Select(PageTextBox.TextLength, 0);
            PageTextBox.ScrollToCaret();
        }

        PageTextBox.Select(position, 0);
        PageTextBox.ScrollToCaret();
    }

    /// <returns>Where the link under the point goes; null when there's none.</returns>
    private string? LinkAt(Point point)
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

    // The next link after the selection, or the one before it, round to the other end of the page
    private void SelectLink(bool forward)
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

    private string? LinkAtCaret()
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

    private void PagesListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_isUpdating && PagesListBox.SelectedItem is HelpPageModel page)
        {
            PageChosen?.Invoke(this, page.Name);
        }
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
        bool isInFind = FindTextBox.Focused;

        switch (e.KeyData)
        {
            case Keys.Alt | Keys.Left or Keys.BrowserBack:
            case Keys.Back when !isInFind:
                BackRequested?.Invoke(this, EventArgs.Empty);
                break;

            case Keys.Alt | Keys.Right or Keys.BrowserForward:
                ForwardRequested?.Invoke(this, EventArgs.Empty);
                break;

            case Keys.Control | Keys.F:
                FindTextBox.Focus();
                FindTextBox.SelectAll();
                break;

            case Keys.F3:
                FindNext();
                break;

            case Keys.Escape:
                Close();
                break;

            default:
                return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
    }
}
