using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>The document as a tree, for the tree panel: what each value shows, and what's under it.</summary>
public static class JsonTree
{
    private const int MaxShownText = 60;

    /// <returns>
    ///     What its row shows: its name or index, then the value, or how much it holds:
    ///     name: "Ann", age: 31, people [2], [0] {3}. The document itself shows only what it holds.
    /// </returns>
    public static string LabelOf(JsonNodeModel node)
    {
        string name = node.Index is int index ? $"[{index}]" : node.Key is string key ? key : string.Empty;
        string value = node.Kind switch
        {
            JsonNodeKind.Object => $"{{{node.Members.Count}}}",
            JsonNodeKind.Array => $"[{node.Items.Count}]",
            JsonNodeKind.String => Shortened(JsonTextWriter.Quoted(node.Text)),
            _ => JsonTextWriter.Write(node, indented: false),
        };

        if (name.Length == 0)
        {
            return value;
        }

        return node.Kind is JsonNodeKind.Object or JsonNodeKind.Array ? $"{name} {value}" : $"{name}: {value}";
    }

    /// <returns>The values directly under it: an object's members, an array's items; none for any other.</returns>
    public static IReadOnlyList<JsonNodeModel> ChildrenOf(JsonNodeModel node) => node.Kind switch
    {
        JsonNodeKind.Object => [.. node.Members.Select(member => member.Value)],
        JsonNodeKind.Array => node.Items,
        _ => [],
    };

    /// <returns>The values from the document's down to this one, the document's first.</returns>
    public static IReadOnlyList<JsonNodeModel> LineageOf(JsonNodeModel node)
    {
        List<JsonNodeModel> lineage = [];

        for (JsonNodeModel? step = node; step is not null; step = step.Parent)
        {
            lineage.Add(step);
        }

        lineage.Reverse();

        return lineage;
    }

    private static string Shortened(string text) =>
        text.Length <= MaxShownText ? text : text[..(MaxShownText - 2)] + "…\"";
}
