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

    private static bool Contains(JsonNodeModel node, DiffPosition position) =>
        node.Start is DiffPosition start && (node.KeyStart ?? start) <= position
        && (node.End is not DiffPosition end || position < end);

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
