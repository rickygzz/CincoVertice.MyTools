using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.JmesPath;

/// <summary>
///     Runs a JMESPath expression on a JSON value, as the specification says: a field of what isn't an object is null,
///     a projection leaves out the nulls its elements give, and so on. Values of the document keep their places in
///     the text, so the search can show them.
/// </summary>
public static class JmesEvaluator
{
    /// <exception cref="JmesPathException">It can't be read, or a function is given what it doesn't take.</exception>
    public static JsonNodeModel Search(string expression, JsonNodeModel root) =>
        Evaluate(JmesParser.Parse(expression), root);

    public static JsonNodeModel Evaluate(JmesNode node, JsonNodeModel value)
    {
        switch (node.Kind)
        {
            case JmesNodeKind.Identity or JmesNodeKind.Current:
                return value;
            case JmesNodeKind.Literal:
                return node.Value ?? JsonNodeModel.Null;
            case JmesNodeKind.Field:
                return value.Kind == JsonNodeKind.Object
                    ? value.Member(node.Name) ?? JsonNodeModel.Null
                    : JsonNodeModel.Null;
            case JmesNodeKind.Subexpression or JmesNodeKind.IndexExpression or JmesNodeKind.Pipe:
                return Evaluate(node.Right, Evaluate(node.Left, value));
            case JmesNodeKind.Index:
                return IndexOf(value, node.Slice![0]!.Value);
            case JmesNodeKind.Slice:
                return SliceOf(value, node.Slice!);
            case JmesNodeKind.Projection:
                return Project(Evaluate(node.Left, value), JsonNodeKind.Array, item => item, node.Right);
            case JmesNodeKind.ValueProjection:
                return Project(Evaluate(node.Left, value), JsonNodeKind.Object, item => item, node.Right);
            case JmesNodeKind.FilterProjection:
                JsonNodeModel filtered = Evaluate(node.Left, value);

                return filtered.Kind != JsonNodeKind.Array
                    ? JsonNodeModel.Null
                    : Collect(
                        filtered.Items.Where(item => IsTrue(Evaluate(node.Children[2], item))),
                        node.Right);
            case JmesNodeKind.Flatten:
                return Flatten(Evaluate(node.Left, value));
            case JmesNodeKind.Or:
                JsonNodeModel either = Evaluate(node.Left, value);

                return IsTrue(either) ? either : Evaluate(node.Right, value);
            case JmesNodeKind.And:
                JsonNodeModel first = Evaluate(node.Left, value);

                return IsTrue(first) ? Evaluate(node.Right, value) : first;
            case JmesNodeKind.Not:
                return JsonNodeModel.FromBool(!IsTrue(Evaluate(node.Left, value)));
            case JmesNodeKind.Comparator:
                return Compare(node.Name, Evaluate(node.Left, value), Evaluate(node.Right, value));
            case JmesNodeKind.MultiSelectList:
                return value.Kind == JsonNodeKind.Null
                    ? JsonNodeModel.Null
                    : JsonNodeModel.ArrayOf(node.Children.Select(child => Evaluate(child, value)));
            case JmesNodeKind.MultiSelectHash:
                if (value.Kind == JsonNodeKind.Null)
                {
                    return JsonNodeModel.Null;
                }

                var hash = new JsonNodeModel(JsonNodeKind.Object);

                for (int i = 0; i < node.Children.Count; i++)
                {
                    hash.Members.Add(
                        new KeyValuePair<string, JsonNodeModel>(node.Keys[i], Evaluate(node.Children[i], value)));
                }

                return hash;
            case JmesNodeKind.FunctionCall:
                return JmesFunctions.Call(node, value);
            default:
                throw new JmesPathException(
                    "An expression reference (&) goes only in a function such as sort_by",
                    node.Position);
        }
    }

    /// <summary>Whether JMESPath takes it as true: all but false, null, and empty texts, arrays and objects.</summary>
    public static bool IsTrue(JsonNodeModel value) => value.Kind switch
    {
        JsonNodeKind.False or JsonNodeKind.Null => false,
        JsonNodeKind.String => value.Text.Length > 0,
        JsonNodeKind.Array => value.Items.Count > 0,
        JsonNodeKind.Object => value.Members.Count > 0,
        _ => true,
    };

    /// <summary>Whether two values are the same JSON, an object's members in any order.</summary>
    public static bool AreEqual(JsonNodeModel a, JsonNodeModel b)
    {
        if (a.Kind != b.Kind)
        {
            return false;
        }

        return a.Kind switch
        {
            JsonNodeKind.String => a.Text == b.Text,
            JsonNodeKind.Number => a.Number.Equals(b.Number),
            JsonNodeKind.Array => a.Items.Count == b.Items.Count
                && a.Items.Zip(b.Items).All(pair => AreEqual(pair.First, pair.Second)),
            JsonNodeKind.Object => a.Members.Count == b.Members.Count
                && a.Members.All(member => b.Member(member.Key) is { } other && AreEqual(member.Value, other)),
            _ => true,
        };
    }

    private static JsonNodeModel Project(
        JsonNodeModel value,
        JsonNodeKind over,
        Func<JsonNodeModel, JsonNodeModel> each,
        JmesNode right)
    {
        if (value.Kind != over)
        {
            return JsonNodeModel.Null;
        }

        IEnumerable<JsonNodeModel> elements = over == JsonNodeKind.Array
            ? value.Items
            : value.Members.Select(member => member.Value);

        return Collect(elements.Select(each), right);
    }

    // What the right side gives for each element, nulls left out
    private static JsonNodeModel Collect(IEnumerable<JsonNodeModel> elements, JmesNode right) =>
        JsonNodeModel.ArrayOf(elements
            .Select(element => Evaluate(right, element))
            .Where(result => result.Kind != JsonNodeKind.Null));

    private static JsonNodeModel IndexOf(JsonNodeModel value, int index)
    {
        if (value.Kind != JsonNodeKind.Array)
        {
            return JsonNodeModel.Null;
        }

        int at = index < 0 ? value.Items.Count + index : index;

        return at >= 0 && at < value.Items.Count ? value.Items[at] : JsonNodeModel.Null;
    }

    // As Python slices a list: negative counts from the end, a negative step goes backwards
    private static JsonNodeModel SliceOf(JsonNodeModel value, IReadOnlyList<int?> parts)
    {
        if (value.Kind != JsonNodeKind.Array)
        {
            return JsonNodeModel.Null;
        }

        int count = value.Items.Count;
        int step = parts[2] ?? 1;
        int Bound(int? part, int whenNone)
        {
            if (part is not int given)
            {
                return whenNone;
            }

            int at = given < 0 ? given + count : given;

            return step > 0 ? Math.Clamp(at, 0, count) : Math.Clamp(at, -1, count - 1);
        }

        int start = Bound(parts[0], step > 0 ? 0 : count - 1);
        int stop = Bound(parts[1], step > 0 ? count : -1);
        List<JsonNodeModel> items = [];

        for (int i = start; step > 0 ? i < stop : i > stop; i += step)
        {
            items.Add(value.Items[i]);
        }

        return JsonNodeModel.ArrayOf(items);
    }

    private static JsonNodeModel Flatten(JsonNodeModel value)
    {
        if (value.Kind != JsonNodeKind.Array)
        {
            return JsonNodeModel.Null;
        }

        return JsonNodeModel.ArrayOf(
            value.Items.SelectMany(item => item.Kind == JsonNodeKind.Array ? item.Items : [item]));
    }

    // == and != for any values; <, <=, >, >= only for numbers, else null
    private static JsonNodeModel Compare(string comparator, JsonNodeModel left, JsonNodeModel right)
    {
        if (comparator is nameof(JmesTokenKind.Eq) or nameof(JmesTokenKind.Ne))
        {
            return JsonNodeModel.FromBool(AreEqual(left, right) == (comparator == nameof(JmesTokenKind.Eq)));
        }

        if (left.Kind != JsonNodeKind.Number || right.Kind != JsonNodeKind.Number)
        {
            return JsonNodeModel.Null;
        }

        return JsonNodeModel.FromBool(comparator switch
        {
            nameof(JmesTokenKind.Lt) => left.Number < right.Number,
            nameof(JmesTokenKind.Lte) => left.Number <= right.Number,
            nameof(JmesTokenKind.Gt) => left.Number > right.Number,
            _ => left.Number >= right.Number,
        });
    }
}
