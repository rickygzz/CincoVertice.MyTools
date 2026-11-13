using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Views;

namespace CincoVertice.MyTools.Ui.Tests.Help;

public sealed class FakeHelpView : IHelpView
{
    public event EventHandler<string>? LinkClicked;

    public event EventHandler<string>? PageChosen;

    public event EventHandler? BackRequested;

    public event EventHandler? ForwardRequested;

    public IReadOnlyList<HelpPageModel> Pages { get; private set; } = [];

    /// <summary>Every page shown, in order, with the heading it was shown at.</summary>
    public List<(string Name, string Title, string? Anchor)> Shown { get; } = [];

    /// <summary>Every scroll within the page shown.</summary>
    public List<string?> Scrolls { get; } = [];

    public MarkdownDocumentModel? Document { get; private set; }

    public (bool Back, bool Forward) Navigation { get; private set; }

    public List<string> Opened { get; } = [];

    public List<string> Problems { get; } = [];

    public void ShowPages(IReadOnlyList<HelpPageModel> pages) => Pages = pages;

    public void ShowPage(HelpPageModel page, MarkdownDocumentModel document, string? anchor)
    {
        Shown.Add((page.Name, page.Title, anchor));
        Document = document;
    }

    public void ScrollTo(string? anchor) => Scrolls.Add(anchor);

    public void ShowNavigation(bool canGoBack, bool canGoForward) => Navigation = (canGoBack, canGoForward);

    public void OpenExternal(string url) => Opened.Add(url);

    public void ShowProblem(string message) => Problems.Add(message);

    public void ClickLink(string target) => LinkClicked?.Invoke(this, target);

    public void ChoosePage(string name) => PageChosen?.Invoke(this, name);

    public void Back() => BackRequested?.Invoke(this, EventArgs.Empty);

    public void Forward() => ForwardRequested?.Invoke(this, EventArgs.Empty);
}
