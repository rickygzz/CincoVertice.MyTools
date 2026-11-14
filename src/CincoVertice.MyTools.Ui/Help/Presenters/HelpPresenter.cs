using System.Drawing;
using CincoVertice.MyTools.Ui.Help.Interfaces;
using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.Help.Views;

namespace CincoVertice.MyTools.Ui.Help.Presenters;

/// <summary>
///     The user guide reader: the guide's pages, the contents first, and one page at a time. Its links open other
///     pages and their headings, and web pages outside; back and forward go through the pages and headings opened,
///     as in a browser, each scrolled to where it was left. A search lists the pages with the text, the most first.
///     The window opens where it was, at the page it showed last.
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
    private readonly IHelpSettingsStore _settings;

    // Where back and forward go
    private readonly List<HelpPlaceModel> _history = [];

    // Every page, read once: the guide doesn't change while the app runs
    private readonly Dictionary<string, MarkdownDocumentModel> _documents = [];

    private IReadOnlyList<HelpPageModel> _pages = [];
    private int _position = -1;
    private string? _shownPage;
    private string _searchText = string.Empty;

    // Chosen with the button; null while it follows Windows' mode
    private bool? _isDark;

    public HelpPresenter(IHelpView view, IHelpPageStore store, IHelpSettingsStore settings)
    {
        _view = view;
        _store = store;
        _settings = settings;
    }

    /// <summary>
    ///     Puts the window where it was when it closed last, dark or light as chosen; before it's shown, so it doesn't
    ///     jump or flash.
    /// </summary>
    public void RestoreWindow()
    {
        HelpSettingsModel? saved = _settings.Load();

        if (saved is { Bounds: Rectangle bounds })
        {
            _view.PlaceWindow(bounds, saved.IsMaximized);
        }

        _isDark = saved?.IsDark;

        if (_isDark is bool isDark)
        {
            _view.ShowTheme(isDark);
        }
    }

    /// <param name="page">The page to open first; when null, the one shown last, or else the contents.</param>
    /// <param name="anchor">The heading to open it at; the top when null.</param>
    public void Start(string? page = null, string? anchor = null)
    {
        _view.LinkClicked += OnLinkClicked;
        _view.PageChosen += OnPageChosen;
        _view.BackRequested += OnBackRequested;
        _view.ForwardRequested += OnForwardRequested;
        _view.SearchRequested += OnSearchRequested;
        _view.SearchResultChosen += OnSearchResultChosen;
        _view.ThemeToggled += OnThemeToggled;
        _view.WindowClosing += OnClosing;

        _pages = ReadPages();
        _view.ShowPages(_pages);

        if (_pages.Count == 0)
        {
            MarkdownDocumentModel missing = MarkdownParser.Parse(MissingGuide);
            _view.ShowPage(new HelpPageModel(string.Empty, missing.Title ?? "Help"), missing, null);
            _view.ShowNavigation(false, false);

            return;
        }

        string? last = _settings.Load()?.Page;
        Open(page ?? (last is not null && _documents.ContainsKey(NameOf(last)) ? last : _pages[0].Name), anchor);
    }

    /// <summary>Opens a page, at a heading, after the place shown, which back goes to.</summary>
    /// <param name="page">Its file name, e.g. "02-hotkeys.md".</param>
    public void Open(string page, string? anchor = null)
    {
        var place = new HelpPlaceModel(NameOf(page), anchor);

        if (_position >= 0 && _history[_position].Page == place.Page && _history[_position].Anchor == anchor)
        {
            _view.ScrollTo(anchor);

            return;
        }

        RememberTop();

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
            if (_store.Read(name) is string markdown)
            {
                MarkdownDocumentModel document = MarkdownParser.Parse(markdown);
                _documents[name] = document;
                pages.Add(new HelpPageModel(name, document.Title ?? Path.GetFileNameWithoutExtension(name)));
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

    /// <summary>Shows a place: where it was scrolled to when it was left, or else at its heading.</summary>
    private bool Show(HelpPlaceModel place)
    {
        string page = place.Page;

        if (page != _shownPage)
        {
            if (!_documents.TryGetValue(page, out MarkdownDocumentModel? document))
            {
                _view.ShowProblem($"The user guide has no page {page}.");

                return false;
            }

            _view.ShowPage(new HelpPageModel(page, document.Title ?? page), document, place.Anchor);
            _shownPage = page;
        }
        else if (place.Top is null)
        {
            _view.ScrollTo(place.Anchor);
        }

        if (place.Top is int top)
        {
            _view.ScrollToPosition(top);
        }

        return true;
    }

    // Where the place shown is scrolled to, for when it's come back to
    private void RememberTop()
    {
        if (_position >= 0)
        {
            _history[_position] = _history[_position] with { Top = _view.TopPosition };
        }
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

        if (position < 0 || position >= _history.Count)
        {
            return;
        }

        RememberTop();

        if (!Show(_history[position]))
        {
            return;
        }

        _position = position;
        ShowNavigation();
    }

    private void OnSearchRequested(object? sender, string text)
    {
        _searchText = text.Trim();

        if (_searchText.Length == 0)
        {
            _view.ShowPages(_pages);

            return;
        }

        IEnumerable<(HelpPageModel, MarkdownDocumentModel)> pages =
            _pages.Select(page => (page, _documents[page.Name]));
        _view.ShowSearchResults(_searchText, HelpSearch.Find(pages, _searchText));
    }

    // The page, at the first time it has the text
    private void OnSearchResultChosen(object? sender, HelpSearchResultModel result)
    {
        Open(result.Page);
        _view.HighlightText(_searchText);
    }

    private void OnClosing(object? sender, EventArgs e)
    {
        _settings.Save(new HelpSettingsModel(_shownPage, _view.WindowBounds, _view.IsMaximized, _isDark));
    }

    /// <summary>The other of dark and light, kept for the next time the window opens.</summary>
    private void OnThemeToggled(object? sender, EventArgs e)
    {
        _isDark = !_view.IsDark;
        _view.ShowTheme(_isDark.Value);
    }
}
