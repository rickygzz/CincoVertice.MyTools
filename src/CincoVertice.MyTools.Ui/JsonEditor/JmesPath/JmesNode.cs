using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.JmesPath;

/// <summary>A part of a JMESPath expression, as parsed.</summary>
/// <param name="Kind">What it is.</param>
/// <param name="Position">Where it is in the expression, for errors.</param>
/// <param name="Children">Its parts: left and right, a function's arguments, or a multi-select's expressions.</param>
/// <param name="Name">A field's or function's name, or a comparator's operator.</param>
/// <param name="Value">A literal's value.</param>
/// <param name="Slice">An index's number, or a slice's start, stop and step; null for one left out.</param>
public sealed record JmesNode(
    JmesNodeKind Kind,
    int Position,
    IReadOnlyList<JmesNode> Children,
    string Name = "",
    JsonNodeModel? Value = null,
    IReadOnlyList<int?>? Slice = null)
{
    public IReadOnlyList<string> Keys { get; init; } = [];

    public JmesNode Left => Children[0];

    public JmesNode Right => Children[1];

    public static JmesNode Of(JmesNodeKind kind, int position, params JmesNode[] children) =>
        new(kind, position, children);
}
