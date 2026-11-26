using System.Globalization;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Services;

namespace CincoVertice.MyTools.Ui.JsonEditor.JmesPath;

/// <summary>JMESPath's functions, as its specification lists them, each checking what it's given.</summary>
public static class JmesFunctions
{
    /// <summary>Each function's name and arguments, for suggestions and errors.</summary>
    public static IReadOnlyDictionary<string, string> Signatures { get; } = new Dictionary<string, string>
    {
        ["abs"] = "abs(number)",
        ["avg"] = "avg(array of numbers)",
        ["ceil"] = "ceil(number)",
        ["contains"] = "contains(array or string, value)",
        ["ends_with"] = "ends_with(string, suffix)",
        ["floor"] = "floor(number)",
        ["join"] = "join(glue, array of strings)",
        ["keys"] = "keys(object)",
        ["length"] = "length(string, array or object)",
        ["map"] = "map(&expression, array)",
        ["max"] = "max(array of numbers or strings)",
        ["max_by"] = "max_by(array, &expression)",
        ["merge"] = "merge(object, ...)",
        ["min"] = "min(array of numbers or strings)",
        ["min_by"] = "min_by(array, &expression)",
        ["not_null"] = "not_null(value, ...)",
        ["reverse"] = "reverse(string or array)",
        ["sort"] = "sort(array of numbers or strings)",
        ["sort_by"] = "sort_by(array, &expression)",
        ["starts_with"] = "starts_with(string, prefix)",
        ["sum"] = "sum(array of numbers)",
        ["to_array"] = "to_array(value)",
        ["to_number"] = "to_number(value)",
        ["to_string"] = "to_string(value)",
        ["type"] = "type(value)",
        ["values"] = "values(object)",
    };

    public static JsonNodeModel Call(JmesNode call, JsonNodeModel current)
    {
        string name = call.Name;

        if (!Signatures.TryGetValue(name, out string? signature))
        {
            throw new JmesPathException($"There's no function {name}()", call.Position);
        }

        IReadOnlyList<JmesNode> arguments = call.Children;
        (int min, int max) = name switch
        {
            "merge" or "not_null" => (1, int.MaxValue),
            "contains" or "ends_with" or "join" or "map" or "max_by" or "min_by" or "sort_by" or "starts_with" =>
                (2, 2),
            _ => (1, 1),
        };

        if (arguments.Count < min || arguments.Count > max)
        {
            throw new JmesPathException($"{signature} is given {arguments.Count} arguments", call.Position);
        }

        // An &expression stays unevaluated, for the function to run on each element
        bool IsReference(int i) => name switch
        {
            "map" => i == 0,
            "max_by" or "min_by" or "sort_by" => i == 1,
            _ => false,
        };

        List<JsonNodeModel> values = [];
        JmesNode? reference = null;

        for (int i = 0; i < arguments.Count; i++)
        {
            if (IsReference(i))
            {
                reference = arguments[i].Kind == JmesNodeKind.ExpRef
                    ? arguments[i].Left
                    : throw new JmesPathException($"{signature}: write & before the expression", arguments[i].Position);
            }
            else
            {
                values.Add(JmesEvaluator.Evaluate(arguments[i], current));
            }
        }

        JsonNodeModel Arg(int i, params JsonNodeKind[] kinds) => kinds.Contains(values[i].Kind)
            ? values[i]
            : throw new JmesPathException($"{signature} isn't given {TypeOf(values[i])} there", arguments[i].Position);

        JmesPathException WrongItem(JsonNodeModel item) =>
            new($"{signature} is given an array with {TypeOf(item)} in it", call.Position);

        List<double> Numbers(int i) => Arg(i, JsonNodeKind.Array).Items
            .Select(item => item.Kind == JsonNodeKind.Number ? item.Number : throw WrongItem(item))
            .ToList();

        switch (name)
        {
            case "abs":
                return JsonNodeModel.FromNumber(Math.Abs(Arg(0, JsonNodeKind.Number).Number));
            case "ceil":
                return JsonNodeModel.FromNumber(Math.Ceiling(Arg(0, JsonNodeKind.Number).Number));
            case "floor":
                return JsonNodeModel.FromNumber(Math.Floor(Arg(0, JsonNodeKind.Number).Number));
            case "avg":
                List<double> averaged = Numbers(0);
                return averaged.Count == 0 ? JsonNodeModel.Null : JsonNodeModel.FromNumber(averaged.Average());
            case "sum":
                return JsonNodeModel.FromNumber(Numbers(0).Sum());
            case "contains":
                JsonNodeModel subject = Arg(0, JsonNodeKind.Array, JsonNodeKind.String);
                return JsonNodeModel.FromBool(subject.Kind == JsonNodeKind.String
                    ? values[1].Kind == JsonNodeKind.String
                        && subject.Text.Contains(values[1].Text, StringComparison.Ordinal)
                    : subject.Items.Any(item => JmesEvaluator.AreEqual(item, values[1])));
            case "starts_with":
                return JsonNodeModel.FromBool(Arg(0, JsonNodeKind.String).Text.StartsWith(
                    Arg(1, JsonNodeKind.String).Text,
                    StringComparison.Ordinal));
            case "ends_with":
                return JsonNodeModel.FromBool(Arg(0, JsonNodeKind.String).Text.EndsWith(
                    Arg(1, JsonNodeKind.String).Text,
                    StringComparison.Ordinal));
            case "join":
                string glue = Arg(0, JsonNodeKind.String).Text;
                return JsonNodeModel.String(string.Join(glue, Arg(1, JsonNodeKind.Array).Items.Select(item =>
                    item.Kind == JsonNodeKind.String ? item.Text : throw WrongItem(item))));
            case "keys":
                return JsonNodeModel.ArrayOf(
                    Arg(0, JsonNodeKind.Object).Members.Select(member => JsonNodeModel.String(member.Key)));
            case "values":
                return JsonNodeModel.ArrayOf(Arg(0, JsonNodeKind.Object).Members.Select(member => member.Value));
            case "length":
                JsonNodeModel measured = Arg(0, JsonNodeKind.String, JsonNodeKind.Array, JsonNodeKind.Object);
                return JsonNodeModel.FromNumber(measured.Kind switch
                {
                    JsonNodeKind.String => new StringInfo(measured.Text).LengthInTextElements,
                    JsonNodeKind.Array => measured.Items.Count,
                    _ => measured.Members.Count,
                });
            case "map":
                return JsonNodeModel.ArrayOf(
                    Arg(0, JsonNodeKind.Array).Items.Select(item => JmesEvaluator.Evaluate(reference!, item)));
            case "max" or "min":
                return Extreme(Arg(0, JsonNodeKind.Array).Items, item => item, name == "max", signature, call.Position);
            case "max_by" or "min_by":
                return Extreme(
                    Arg(0, JsonNodeKind.Array).Items,
                    item => JmesEvaluator.Evaluate(reference!, item),
                    name == "max_by",
                    signature,
                    call.Position);
            case "sort":
                return JsonNodeModel.ArrayOf(
                    Sorted(Arg(0, JsonNodeKind.Array).Items, item => item, signature, call.Position));
            case "sort_by":
                return JsonNodeModel.ArrayOf(Sorted(
                    Arg(0, JsonNodeKind.Array).Items,
                    item => JmesEvaluator.Evaluate(reference!, item),
                    signature,
                    call.Position));
            case "merge":
                var merged = new JsonNodeModel(JsonNodeKind.Object);

                for (int i = 0; i < values.Count; i++)
                {
                    foreach (KeyValuePair<string, JsonNodeModel> member in Arg(i, JsonNodeKind.Object).Members)
                    {
                        merged.Members.RemoveAll(existing => existing.Key == member.Key);
                        merged.Members.Add(member);
                    }
                }

                return merged;
            case "not_null":
                return values.FirstOrDefault(value => value.Kind != JsonNodeKind.Null) ?? JsonNodeModel.Null;
            case "reverse":
                JsonNodeModel reversed = Arg(0, JsonNodeKind.String, JsonNodeKind.Array);
                return reversed.Kind == JsonNodeKind.String
                    ? JsonNodeModel.String(new string([.. reversed.Text.Reverse()]))
                    : JsonNodeModel.ArrayOf(Enumerable.Reverse(reversed.Items));
            case "to_array":
                return values[0].Kind == JsonNodeKind.Array ? values[0] : JsonNodeModel.ArrayOf([values[0]]);
            case "to_string":
                return values[0].Kind == JsonNodeKind.String
                    ? values[0]
                    : JsonNodeModel.String(JsonTextWriter.Write(values[0], indented: false));
            case "to_number":
                return values[0].Kind switch
                {
                    JsonNodeKind.Number => values[0],
                    JsonNodeKind.String when double.TryParse(
                        values[0].Text,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double number) => JsonNodeModel.FromNumber(number),
                    _ => JsonNodeModel.Null,
                };
            default:
                return JsonNodeModel.String(TypeOf(values[0]));
        }
    }

    /// <returns>A value's type as JMESPath names it: number, string, boolean, array, object, null.</returns>
    public static string TypeOf(JsonNodeModel value) => value.Kind switch
    {
        JsonNodeKind.Number => "number",
        JsonNodeKind.String => "string",
        JsonNodeKind.True or JsonNodeKind.False => "boolean",
        JsonNodeKind.Array => "array",
        JsonNodeKind.Object => "object",
        _ => "null",
    };

    // The items by their keys, all numbers or all strings
    private static List<JsonNodeModel> Sorted(
        List<JsonNodeModel> items,
        Func<JsonNodeModel, JsonNodeModel> keyOf,
        string signature,
        int position)
    {
        List<(JsonNodeModel Item, JsonNodeModel Key)> keyed = [.. items.Select(item => (item, keyOf(item)))];
        CheckKeys(keyed.Select(pair => pair.Key), signature, position);

        return [.. keyed.OrderBy(pair => pair.Key, KeyComparer.Instance).Select(pair => pair.Item)];
    }

    private static JsonNodeModel Extreme(
        List<JsonNodeModel> items,
        Func<JsonNodeModel, JsonNodeModel> keyOf,
        bool max,
        string signature,
        int position)
    {
        List<JsonNodeModel> sorted = Sorted(items, keyOf, signature, position);

        return sorted.Count == 0 ? JsonNodeModel.Null : max ? sorted[^1] : sorted[0];
    }

    private static void CheckKeys(IEnumerable<JsonNodeModel> keys, string signature, int position)
    {
        List<JsonNodeKind> kinds = [.. keys.Select(key => key.Kind).Distinct()];

        if (kinds.Count > 1 || kinds.Any(kind => kind is not (JsonNodeKind.Number or JsonNodeKind.String)))
        {
            throw new JmesPathException($"{signature} compares only numbers, or only strings", position);
        }
    }

    private sealed class KeyComparer : IComparer<JsonNodeModel>
    {
        public static KeyComparer Instance { get; } = new();

        public int Compare(JsonNodeModel? x, JsonNodeModel? y) => x!.Kind == JsonNodeKind.Number
            ? x.Number.CompareTo(y!.Number)
            : string.CompareOrdinal(x.Text, y!.Text);
    }
}
