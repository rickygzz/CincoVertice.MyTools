using System.Text;
using System.Text.RegularExpressions;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>
///     The JMESPath of a value in the document: people[0].address."zip code"; @ for the root. A name that isn't an
///     identifier (letters, digits and _, not starting with a digit) is quoted.
/// </summary>
public static partial class JsonPathWriter
{
    public static string PathOf(JsonNodeModel node)
    {
        var path = new StringBuilder();

        foreach (JsonNodeModel step in Ancestry(node))
        {
            if (step.Index is int index)
            {
                path.Append('[').Append(index).Append(']');
            }
            else if (step.Key is string key)
            {
                if (path.Length > 0)
                {
                    path.Append('.');
                }

                path.Append(Name(key));
            }
        }

        return path.Length == 0 ? "@" : path.ToString();
    }

    /// <returns>A member's name as JMESPath writes it: as itself, or in double quotes.</returns>
    public static string Name(string key) => IdentifierRegex().IsMatch(key) ? key : JsonTextWriter.Quoted(key);

    /// <returns>The deepest value the position is in, its name included; null outside every value.</returns>
    public static JsonNodeModel? NodeAt(JsonNodeModel? root, DiffPosition position)
    {
        JsonNodeModel? found = null;
        JsonNodeModel? current = root;

        while (current is not null && Contains(current, position))
        {
            found = current;
            current = current.Kind switch
            {
                JsonNodeKind.Object => current.Members
                    .Select(member => member.Value)
                    .FirstOrDefault(value => Contains(value, position)),
                JsonNodeKind.Array => current.Items.FirstOrDefault(item => Contains(item, position)),
                _ => null,
            };
        }

        return found;
    }

    /// <returns>
    ///     The values wholly inside the selection, outermost first and in their order: a whole object selected is one
    ///     value, some of its members are theirs. None when the selection is inside one value.
    /// </returns>
    public static IReadOnlyList<JsonNodeModel> NodesIn(JsonNodeModel? root, DiffPosition start, DiffPosition end)
    {
        List<JsonNodeModel> found = [];

        if (root is not null && start < end)
        {
            Collect(root, start, end, found);
        }

        return found;
    }

    private static void Collect(JsonNodeModel node, DiffPosition start, DiffPosition end, List<JsonNodeModel> found)
    {
        if (node.Start is not DiffPosition from || node.End is not DiffPosition to || to <= start || from >= end)
        {
            return;
        }

        if (start <= from && to <= end)
        {
            found.Add(node);

            return;
        }

        IEnumerable<JsonNodeModel> inside = node.Kind switch
        {
            JsonNodeKind.Object => node.Members.Select(member => member.Value),
            JsonNodeKind.Array => node.Items,
            _ => [],
        };

        foreach (JsonNodeModel child in inside)
        {
            Collect(child, start, end, found);
        }
    }

    private static bool Contains(JsonNodeModel node, DiffPosition position) =>
        node.Start is DiffPosition start && (node.KeyStart ?? start) <= position
        && (node.End is not DiffPosition end || position < end);

    /// <returns>
    ///     The steps from the document to the value, each with the value it leads to, for the status bar's
    ///     breadcrumb: people, [3], address. None for the document itself.
    /// </returns>
    public static IReadOnlyList<JsonCrumbModel> CrumbsOf(JsonNodeModel node) =>
    [
        .. Ancestry(node).Select(step => new JsonCrumbModel(
            step.Index is int index ? $"[{index}]" : Name(step.Key ?? string.Empty),
            step)),
    ];

    /// <returns>What the value is, in a few words: object, 3 keys; string, 5 characters; number.</returns>
    public static string KindOf(JsonNodeModel node) => node.Kind switch
    {
        JsonNodeKind.Object => $"object, {Count(node.Members.Count, "key", "keys")}",
        JsonNodeKind.Array => $"array, {Count(node.Items.Count, "item", "items")}",
        JsonNodeKind.String => $"string, {Count(node.Text.Length, "character", "characters")}",
        JsonNodeKind.Number => "number",
        JsonNodeKind.True or JsonNodeKind.False => "boolean",
        _ => "null",
    };

    private static string Count(int count, string one, string many) => $"{count} {(count == 1 ? one : many)}";

    private static IEnumerable<JsonNodeModel> Ancestry(JsonNodeModel node)
    {
        Stack<JsonNodeModel> steps = new();

        for (JsonNodeModel? step = node; step?.Parent is not null; step = step.Parent)
        {
            steps.Push(step);
        }

        return steps;
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierRegex();
}
