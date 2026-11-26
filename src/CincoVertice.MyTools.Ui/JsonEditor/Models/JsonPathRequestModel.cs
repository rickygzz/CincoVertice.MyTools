using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>A right click in the text, asking what's there; the presenter answers in the rest.</summary>
/// <param name="Position">Where the click was.</param>
/// <param name="Selection">The text selected, the click in it; null for none, or a click outside it.</param>
public sealed class JsonPathRequestModel(
    DiffPosition position,
    (DiffPosition Start, DiffPosition End)? selection = null)
{
    public DiffPosition Position { get; } = position;

    public (DiffPosition Start, DiffPosition End)? Selection { get; } = selection;

    /// <summary>The JMESPath of the value there; null outside every value, as in invalid JSON.</summary>
    public string? Path { get; set; }

    /// <summary>
    ///     The values to copy, as JSON on one line each: those the selection holds, or the one clicked; with several,
    ///     a comma at the end of each line but the last, as in an array.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>How many values <see cref="Value"/> has.</summary>
    public int ValueCount { get; set; }

    /// <summary>Those values that have a name, each as "name": value, as in an object; null for none.</summary>
    public string? NameAndValue { get; set; }

    /// <summary>How many <see cref="NameAndValue"/> has.</summary>
    public int NameAndValueCount { get; set; }
}
