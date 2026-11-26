using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>Values made from others: keys sorted, and a result as plain lines.</summary>
public static class JsonTransforms
{
    /// <returns>
    ///     A copy of the value with every object's keys sorted, in it and under it: by name ignoring case, then as
    ///     written. The copy has no places in the text.
    /// </returns>
    public static JsonNodeModel SortedKeys(JsonNodeModel node)
    {
        switch (node.Kind)
        {
            case JsonNodeKind.Object:
                var sorted = new JsonNodeModel(JsonNodeKind.Object);
                sorted.Members.AddRange(node.Members
                    .OrderBy(member => member.Key, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(member => member.Key, StringComparer.Ordinal)
                    .Select(member => KeyValuePair.Create(member.Key, SortedKeys(member.Value))));

                return sorted;
            case JsonNodeKind.Array:
                return JsonNodeModel.ArrayOf(node.Items.Select(SortedKeys));
            default:
                return node;
        }
    }

    /// <returns>
    ///     The value as lines of plain text, for a spreadsheet or a list: an array's items one per line, each string
    ///     without its quotes, any other value as JSON on one line.
    /// </returns>
    public static string AsLines(JsonNodeModel node) => node.Kind == JsonNodeKind.Array
        ? string.Join(Environment.NewLine, node.Items.Select(Plain))
        : Plain(node);

    /// <returns>The text indented as a value starting after this indentation: every line after the first.</returns>
    public static string Indented(string text, string indentation) =>
        text.Replace("\n", "\n" + indentation, StringComparison.Ordinal);

    private static string Plain(JsonNodeModel node) =>
        node.Kind == JsonNodeKind.String ? node.Text : JsonTextWriter.Write(node, indented: false);
}
