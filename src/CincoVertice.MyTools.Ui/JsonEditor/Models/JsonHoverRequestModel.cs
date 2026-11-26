using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>The pointer resting on the text, asking what's there; the presenter answers in Text.</summary>
public sealed class JsonHoverRequestModel(DiffPosition position)
{
    public DiffPosition Position { get; } = position;

    /// <summary>The value's path, what it is, and what the schema finds wrong with it; null for nothing.</summary>
    public string? Text { get; set; }
}
