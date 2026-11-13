using System.Drawing;
using CincoVertice.MyTools.Ui.Help.Models;

namespace CincoVertice.MyTools.Ui.Help.Views;

/// <summary>
///     The user guide reader: the list of pages, or of the pages a search found, the page shown, and back and forward.
/// </summary>
public interface IHelpView
{
    /// <summary>A link in the page was clicked: its target as written, e.g. "02-hotkeys.md#default-hotkeys".</summary>
    event EventHandler<string>? LinkClicked;

    /// <summary>A page was chosen in the list: its file name.</summary>
    event EventHandler<string>? PageChosen;

    event EventHandler? BackRequested;

    event EventHandler? ForwardRequested;

    /// <summary>The guide was searched for a text; empty to go back to the list of pages.</summary>
    event EventHandler<string>? SearchRequested;

    /// <summary>A page the search found was chosen.</summary>
    event EventHandler<HelpSearchResultModel>? SearchResultChosen;

    /// <summary>The window is closing.</summary>
    event EventHandler? WindowClosing;

    /// <summary>Where the text at the top of the window is, in the page's text.</summary>
    int TopPosition { get; }

    /// <summary>Where the window is on the screen, as it is when not maximized.</summary>
    Rectangle WindowBounds { get; }

    bool IsMaximized { get; }

    /// <summary>Puts the window where it was, if that's on a screen now.</summary>
    void PlaceWindow(Rectangle bounds, bool isMaximized);

    void ShowPages(IReadOnlyList<HelpPageModel> pages);

    /// <param name="text">What was searched for.</param>
    void ShowSearchResults(string text, IReadOnlyList<HelpSearchResultModel> results);

    /// <param name="anchor">The heading to scroll to; null for the top.</param>
    void ShowPage(HelpPageModel page, MarkdownDocumentModel document, string? anchor);

    /// <summary>Scrolls the page shown to a heading; null, or one it doesn't have, is the top.</summary>
    void ScrollTo(string? anchor);

    /// <summary>Scrolls the page so the text at a position is at the top.</summary>
    /// <param name="position">As <see cref="TopPosition"/> gave it.</param>
    void ScrollToPosition(int position);

    /// <summary>Selects the first time the page shown has a text, as finding it in the page does.</summary>
    void HighlightText(string text);

    void ShowNavigation(bool canGoBack, bool canGoForward);

    /// <summary>Opens a web page, or another address, in the program Windows opens it with.</summary>
    void OpenExternal(string url);

    void ShowProblem(string message);
}
