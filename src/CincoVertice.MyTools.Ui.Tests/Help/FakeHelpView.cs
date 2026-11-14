using System.Drawing;
using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Views;

namespace CincoVertice.MyTools.Ui.Tests.Help;

public sealed class FakeHelpView : IHelpView
{
    public event EventHandler<string>? LinkClicked;

    public event EventHandler<string>? PageChosen;

    public event EventHandler? BackRequested;

    public event EventHandler? ForwardRequested;

    public event EventHandler<string>? SearchRequested;

    public event EventHandler<HelpSearchResultModel>? SearchResultChosen;

    public event EventHandler? WindowClosing;

    public event EventHandler? ThemeToggled;

    /// <summary>Where the window is scrolled to; set by the test, as the user scrolling.</summary>
    public int TopPosition { get; set; }

    /// <summary>Where the window is; set by the test, as the user moving it.</summary>
    public Rectangle WindowBounds { get; set; } = new(10, 20, 800, 600);

    public bool IsMaximized { get; set; }

    public bool IsDark { get; set; }

    public List<bool> ThemesShown { get; } = [];

    public void ShowTheme(bool isDark)
    {
        IsDark = isDark;
        ThemesShown.Add(isDark);
    }

    public void ToggleTheme() => ThemeToggled?.Invoke(this, EventArgs.Empty);

    /// <summary>Where the presenter put the window; null when it didn't.</summary>
    public (Rectangle Bounds, bool IsMaximized)? Placed { get; private set; }

    /// <summary>Every scroll to a position the presenter remembered.</summary>
    public List<int> Positions { get; } = [];

    public IReadOnlyList<HelpPageModel> Pages { get; private set; } = [];

    /// <summary>What a search showed: the text and what it found; null when the list is the pages.</summary>
    public (string Text, IReadOnlyList<HelpSearchResultModel> Results)? Search { get; private set; }

    /// <summary>Every page shown, in order, with the heading it was shown at.</summary>
    public List<(string Name, string Title, string? Anchor)> Shown { get; } = [];

    /// <summary>Every scroll within the page shown.</summary>
    public List<string?> Scrolls { get; } = [];

    public List<string> Highlights { get; } = [];

    public MarkdownDocumentModel? Document { get; private set; }

    public (bool Back, bool Forward) Navigation { get; private set; }

    public List<string> Opened { get; } = [];

    public List<string> Problems { get; } = [];

    public void PlaceWindow(Rectangle bounds, bool isMaximized) => Placed = (bounds, isMaximized);

    public void ShowPages(IReadOnlyList<HelpPageModel> pages)
    {
        Pages = pages;
        Search = null;
    }

    public void ShowSearchResults(string text, IReadOnlyList<HelpSearchResultModel> results) =>
        Search = (text, results);

    public void ShowPage(HelpPageModel page, MarkdownDocumentModel document, string? anchor)
    {
        Shown.Add((page.Name, page.Title, anchor));
        Document = document;
    }

    public void ScrollTo(string? anchor) => Scrolls.Add(anchor);

    public void ScrollToPosition(int position) => Positions.Add(position);

    public void HighlightText(string text) => Highlights.Add(text);

    public void ShowNavigation(bool canGoBack, bool canGoForward) => Navigation = (canGoBack, canGoForward);

    public void OpenExternal(string url) => Opened.Add(url);

    public void ShowProblem(string message) => Problems.Add(message);

    public void ClickLink(string target) => LinkClicked?.Invoke(this, target);

    public void ChoosePage(string name) => PageChosen?.Invoke(this, name);

    public void Back() => BackRequested?.Invoke(this, EventArgs.Empty);

    public void Forward() => ForwardRequested?.Invoke(this, EventArgs.Empty);

    public void SearchFor(string text) => SearchRequested?.Invoke(this, text);

    public void ChooseResult(HelpSearchResultModel result) => SearchResultChosen?.Invoke(this, result);

    public void Close() => WindowClosing?.Invoke(this, EventArgs.Empty);
}
