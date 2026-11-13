using CincoVertice.MyTools.Ui.Help.Interfaces;
using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.Help.Views;

namespace CincoVertice.MyTools.Ui.Help.Presenters;

/// <summary>
///     The user guide reader: the guide's pages, the contents first, and one page at a time. Its links open other
///     pages and their headings, and web pages outside; back and forward go through the pages and headings opened,
///     as in a browser.
/// </summary>
public sealed class HelpPresenter
{
    /// <summary>The guide's first page, with its contents.</summary>
    public const string ContentsPage = "README.md";

    // Shown when the guide isn't there, e.g. in a build that wasn't published
    private const string MissingGuide =
        "# Help\n\nThe user guide isn't here. It's in the `docs\\user-guide` folder next to My Tools, in the zip"
        + " the app comes in: unpack all of it again.";

    private readonly IHelpView _view;
    private readonly IHelpPageStore _store;

    // Where back and forward go; every place has its page
    private readonly List<HelpLinkModel> _history = [];

    private IReadOnlyList<HelpPageModel> _pages = [];
    private int _position = -1;
    private string? _shownPage;

    public HelpPresenter(IHelpView view, IHelpPageStore store)
    {
        _view = view;
        _store = store;
    }

    /// <param name="page">The page to open first; the contents when null.</param>
    public void Start(string? page = null)
    {
        _view.LinkClicked += OnLinkClicked;
        _view.PageChosen += OnPageChosen;
        _view.BackRequested += OnBackRequested;
        _view.ForwardRequested += OnForwardRequested;

        _pages = ReadPages();
        _view.ShowPages(_pages);

        if (_pages.Count == 0)
        {
            MarkdownDocumentModel missing = MarkdownParser.Parse(MissingGuide);
            _view.ShowPage(new HelpPageModel(string.Empty, missing.Title ?? "Help"), missing, null);
            _view.ShowNavigation(false, false);

            return;
        }

        Open(page ?? _pages[0].Name);
    }

    /// <summary>Opens a page, at a heading, after the place shown, which back goes to.</summary>
    /// <param name="page">Its file name, e.g. "02-hotkeys.md".</param>
    public void Open(string page, string? anchor = null)
    {
        var place = new HelpLinkModel(NameOf(page), anchor);

        if (_position >= 0 && _history[_position] == place)
        {
            _view.ScrollTo(anchor);

            return;
        }

        if (!Show(place))
        {
            return;
        }

        _history.RemoveRange(_position + 1, _history.Count - _position - 1);
        _history.Add(place);
        _position++;
        ShowNavigation();
    }

    private IReadOnlyList<HelpPageModel> ReadPages()
    {
        List<HelpPageModel> pages = [];

        foreach (string name in _store.GetPageNames())
        {
            string? markdown = _store.Read(name);

            if (markdown is not null)
            {
                string? title = MarkdownParser.Parse(markdown).Title;
                pages.Add(new HelpPageModel(name, title ?? Path.GetFileNameWithoutExtension(name)));
            }
        }

        // The contents first, then the pages in their numbers' order
        return
        [
            .. pages.OrderBy(page => !string.Equals(page.Name, ContentsPage, StringComparison.OrdinalIgnoreCase))
                .ThenBy(page => page.Name, StringComparer.OrdinalIgnoreCase),
        ];
    }

    // The page's name as the guide has it, whatever case the link wrote it in
    private string NameOf(string page) =>
        _pages.FirstOrDefault(known => string.Equals(known.Name, page, StringComparison.OrdinalIgnoreCase))?.Name
        ?? page;

    private bool Show(HelpLinkModel place)
    {
        string page = place.Page!;

        if (page == _shownPage)
        {
            _view.ScrollTo(place.Anchor);

            return true;
        }

        string? markdown = _pages.Any(known => known.Name == page) ? _store.Read(page) : null;

        if (markdown is null)
        {
            _view.ShowProblem($"The user guide has no page {page}.");

            return false;
        }

        MarkdownDocumentModel document = MarkdownParser.Parse(markdown);
        _view.ShowPage(new HelpPageModel(page, document.Title ?? page), document, place.Anchor);
        _shownPage = page;

        return true;
    }

    private void ShowNavigation()
    {
        _view.ShowNavigation(_position > 0, _position < _history.Count - 1);
    }

    private void OnLinkClicked(object? sender, string target)
    {
        HelpLinkModel link = HelpLinkModel.Parse(target);

        if (link.Url is not null)
        {
            _view.OpenExternal(link.Url);
        }
        else if (link.Page is not null && !link.Page.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            _view.ShowProblem($"{target} isn't a page of the user guide.");
        }
        else if (_shownPage is not null)
        {
            Open(link.Page ?? _shownPage, link.Anchor);
        }
    }

    private void OnPageChosen(object? sender, string page) => Open(page);

    private void OnBackRequested(object? sender, EventArgs e) => Go(-1);

    private void OnForwardRequested(object? sender, EventArgs e) => Go(1);

    private void Go(int step)
    {
        int position = _position + step;

        if (position < 0 || position >= _history.Count || !Show(_history[position]))
        {
            return;
        }

        _position = position;
        ShowNavigation();
    }
}
