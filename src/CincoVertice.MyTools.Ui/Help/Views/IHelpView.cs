using CincoVertice.MyTools.Ui.Help.Models;

namespace CincoVertice.MyTools.Ui.Help.Views;

/// <summary>The user guide reader: the list of pages, the page shown, and back and forward.</summary>
public interface IHelpView
{
    /// <summary>A link in the page was clicked: its target as written, e.g. "02-hotkeys.md#default-hotkeys".</summary>
    event EventHandler<string>? LinkClicked;

    /// <summary>A page was chosen in the list: its file name.</summary>
    event EventHandler<string>? PageChosen;

    event EventHandler? BackRequested;

    event EventHandler? ForwardRequested;

    /// <summary>Where the text at the top of the window is, in the page's text.</summary>
    int TopPosition { get; }

    void ShowPages(IReadOnlyList<HelpPageModel> pages);

    /// <param name="anchor">The heading to scroll to; null for the top.</param>
    void ShowPage(HelpPageModel page, MarkdownDocumentModel document, string? anchor);

    /// <summary>Scrolls the page shown to a heading; null, or one it doesn't have, is the top.</summary>
    void ScrollTo(string? anchor);

    /// <summary>Scrolls the page so the text at a position is at the top.</summary>
    /// <param name="position">As <see cref="TopPosition"/> gave it.</param>
    void ScrollToPosition(int position);

    void ShowNavigation(bool canGoBack, bool canGoForward);

    /// <summary>Opens a web page, or another address, in the program Windows opens it with.</summary>
    void OpenExternal(string url);

    void ShowProblem(string message);
}
