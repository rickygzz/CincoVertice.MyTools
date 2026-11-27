using CincoVertice.MyTools.Ui.RegexTester.Models;

namespace CincoVertice.MyTools.Ui.RegexTester.Views;

/// <summary>
///     The regex tester: a pattern with its options, a text to try it on with its matches colored, the list of matches
///     and their groups, and the text with a replacement. Passive: <see cref="Presenters.RegexTesterPresenter"/>
///     decides what it shows.
/// </summary>
public interface IRegexTesterView
{
    /// <summary>Raised once the user paused in changing the pattern, an option, the text or the replacement.</summary>
    event EventHandler? InputChanged;

    /// <summary>Raised with a row of the matches list the user chose.</summary>
    event EventHandler<RegexRowModel>? RowChosen;

    /// <summary>What the user wrote and ticked.</summary>
    RegexInputModel Input { get; }

    /// <summary>Fills the boxes and the ticks, as the window opens.</summary>
    void ShowInput(RegexInputModel input);

    /// <summary>Colors these parts of the text, a later one over an earlier one, and nothing else.</summary>
    void ShowHighlights(IReadOnlyList<RegexHighlightModel> highlights);

    void ShowRows(IReadOnlyList<RegexRowModel> rows);

    /// <summary>How many matches there are, or why the pattern can't run (a problem).</summary>
    void ShowStatus(string text, bool isProblem);

    /// <summary>The text with every match replaced; null to show there's no replacement.</summary>
    void ShowReplaced(string? text);

    /// <summary>Selects a part of the text and scrolls to it.</summary>
    void SelectText(int start, int length);
}
