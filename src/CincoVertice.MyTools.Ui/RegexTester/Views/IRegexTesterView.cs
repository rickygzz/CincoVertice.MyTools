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

    /// <summary>Raised with a part the user chose in the Add menu, to write into the pattern.</summary>
    event EventHandler<RegexPieceRequestModel>? PieceChosen;

    /// <summary>Raised with an example the user chose in the Examples menu.</summary>
    event EventHandler<RegexExampleModel>? ExampleChosen;

    /// <summary>What the user wrote and ticked.</summary>
    RegexInputModel Input { get; }

    /// <summary>
    ///     Fills the boxes and the ticks, as the window opens or for an example; the text keeps its undo when it's the
    ///     same. Raises no <see cref="InputChanged"/>.
    /// </summary>
    void ShowInput(RegexInputModel input);

    /// <summary>Colors these parts of the text, a later one over an earlier one, and nothing else.</summary>
    void ShowHighlights(IReadOnlyList<RegexHighlightModel> highlights);

    void ShowRows(IReadOnlyList<RegexRowModel> rows);

    /// <summary>How many matches there are, or why the pattern can't run (a problem).</summary>
    void ShowStatus(string text, bool isProblem);

    /// <summary>The text with every match replaced; null to show there's no replacement.</summary>
    void ShowReplaced(string? text);

    /// <summary>
    ///     Writes the pattern with this selection and gives it the focus, so the user types on; ticks ^ and $ at each
    ///     line when <paramref name="eachLine"/>, and leaves it as it is otherwise.
    /// </summary>
    void ShowPattern(RegexEditModel pattern, bool eachLine);

    /// <summary>Selects a part of the text and scrolls to it.</summary>
    void SelectText(int start, int length);
}
