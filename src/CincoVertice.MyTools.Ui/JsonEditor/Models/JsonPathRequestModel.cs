using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>A right click in the text, asking what's there; the presenter answers in Path and Value.</summary>
/// <param name="Position">Where the click was.</param>
public sealed class JsonPathRequestModel(DiffPosition position)
{
    public DiffPosition Position { get; } = position;

    /// <summary>The JMESPath of the value there; null outside every value, as in invalid JSON.</summary>
    public string? Path { get; set; }

    /// <summary>That value as JSON on one line, to copy.</summary>
    public string? Value { get; set; }
}
