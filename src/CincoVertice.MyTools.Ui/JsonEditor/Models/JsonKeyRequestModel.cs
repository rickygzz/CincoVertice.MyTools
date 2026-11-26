using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>Ctrl+Space at the caret, asking for names; the presenter answers in Names.</summary>
public sealed class JsonKeyRequestModel(DiffPosition position)
{
    public DiffPosition Position { get; } = position;

    /// <summary>The names to offer for a new member of the object there, the schema's first.</summary>
    public IReadOnlyList<string> Names { get; set; } = [];
}
