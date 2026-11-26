using System.Globalization;
using System.Text.RegularExpressions;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Services;

namespace CincoVertice.MyTools.Ui.JsonEditor.Schema;

/// <summary>
///     Checks a document against a JSON Schema, of the keywords most schemas use: type, enum, const, the limits of
///     strings, numbers, arrays and objects, pattern, properties, required, additionalProperties, patternProperties,
///     items and prefixItems, allOf, anyOf, oneOf, not, if/then/else, and $ref to a place in the same schema
///     (#/definitions/..., #/$defs/...). Others, as format, are left out: what they'd say isn't said.
/// </summary>
public sealed class JsonSchemaValidator
{
    public const int MaxProblems = 500;
    private const int MaxDepth = 64;

    private static readonly TimeSpan _patternTimeout = TimeSpan.FromMilliseconds(100);

    private readonly JsonNodeModel _schema;

    public JsonSchemaValidator(JsonNodeModel schema)
    {
        _schema = schema;
    }

    /// <returns>What isn't as the schema says, in the order of the text; at most <see cref="MaxProblems"/>.</returns>
    public IReadOnlyList<JsonSchemaProblemModel> Validate(JsonNodeModel document)
    {
        List<JsonSchemaProblemModel> problems = [];
        Check(document, _schema, problems, 0);

        return [.. problems.OrderBy(problem => problem.Start).Take(MaxProblems)];
    }

    /// <returns>
    ///     The names the schema gives the members of this object, in the schema's order: its properties and required
    ///     ones, wherever the schema says them (allOf, anyOf, $ref...).
    /// </returns>
    public IReadOnlyList<string> NamesFor(JsonNodeModel node)
    {
        List<JsonNodeModel> schemas = [_schema];

        foreach (JsonNodeModel step in JsonTree.LineageOf(node).Skip(1))
        {
            schemas = [.. schemas.SelectMany(schema => Branches(schema, 0)).SelectMany(schema => Under(schema, step))];
        }

        List<string> names = [];

        foreach (JsonNodeModel schema in schemas.SelectMany(schema => Branches(schema, 0)))
        {
            names.AddRange(schema.Member("properties")?.Members.Select(member => member.Key) ?? []);
            names.AddRange(schema.Member("required")?.Items.Select(item => item.Text) ?? []);
        }

        return [.. names.Distinct()];
    }

    // The schema and those it takes in: by $ref, allOf, anyOf, oneOf, then/else
    private IEnumerable<JsonNodeModel> Branches(JsonNodeModel schema, int depth)
    {
        if (schema.Kind != JsonNodeKind.Object || depth > MaxDepth)
        {
            yield break;
        }

        yield return schema;

        if (Resolve(schema) is JsonNodeModel target)
        {
            foreach (JsonNodeModel branch in Branches(target, depth + 1))
            {
                yield return branch;
            }
        }

        foreach (string keyword in new[] { "allOf", "anyOf", "oneOf" })
        {
            foreach (JsonNodeModel part in schema.Member(keyword)?.Items ?? [])
            {
                foreach (JsonNodeModel branch in Branches(part, depth + 1))
                {
                    yield return branch;
                }
            }
        }

        foreach (string keyword in new[] { "then", "else" })
        {
            if (schema.Member(keyword) is JsonNodeModel part)
            {
                foreach (JsonNodeModel branch in Branches(part, depth + 1))
                {
                    yield return branch;
                }
            }
        }
    }

    // The schemas a member or an item of something this schema describes has
    private static IEnumerable<JsonNodeModel> Under(JsonNodeModel schema, JsonNodeModel step)
    {
        if (step.Key is string key)
        {
            if (schema.Member("properties")?.Member(key) is JsonNodeModel property)
            {
                yield return property;
            }

            foreach ((string pattern, JsonNodeModel value) in schema.Member("patternProperties")?.Members ?? [])
            {
                if (Matches(pattern, key))
                {
                    yield return value;
                }
            }

            if (schema.Member("additionalProperties") is { Kind: JsonNodeKind.Object } additional)
            {
                yield return additional;
            }
        }
        else if (step.Index is int index)
        {
            JsonNodeModel? prefix = schema.Member("prefixItems")
                ?? (schema.Member("items") is { Kind: JsonNodeKind.Array } tuple ? tuple : null);

            if (prefix is not null && index < prefix.Items.Count)
            {
                yield return prefix.Items[index];
            }
            else if (schema.Member("items") is { Kind: JsonNodeKind.Object } items)
            {
                yield return items;
            }
        }
    }

    private void Check(JsonNodeModel node, JsonNodeModel schema, List<JsonSchemaProblemModel> problems, int depth)
    {
        if (depth > MaxDepth || problems.Count >= MaxProblems)
        {
            return;
        }

        if (schema.Kind == JsonNodeKind.False)
        {
            Add(problems, node, "Not allowed here");

            return;
        }

        if (schema.Kind != JsonNodeKind.Object)
        {
            return;
        }

        if (Resolve(schema) is JsonNodeModel target)
        {
            Check(node, target, problems, depth + 1);
        }

        if (schema.Member("type") is JsonNodeModel type && !IsOfType(node, type))
        {
            string expected = type.Kind == JsonNodeKind.Array
                ? string.Join(" or ", type.Items.Select(item => Article(item.Text)))
                : Article(type.Text);
            Add(problems, node, $"Expected {expected}, not {Article(TypeOf(node))}");

            return;
        }

        if (schema.Member("enum") is { Kind: JsonNodeKind.Array } choices
            && !choices.Items.Any(item => Equal(item, node)))
        {
            string shown = string.Join(", ", choices.Items.Take(8).Select(item => JsonTextWriter.Write(item, false)));
            Add(problems, node, $"Should be one of: {shown}{(choices.Items.Count > 8 ? ", …" : string.Empty)}");
        }

        if (schema.Member("const") is JsonNodeModel constant && !Equal(constant, node))
        {
            Add(problems, node, $"Should be {JsonTextWriter.Write(constant, indented: false)}");
        }

        switch (node.Kind)
        {
            case JsonNodeKind.String:
                CheckString(node, schema, problems);
                break;
            case JsonNodeKind.Number:
                CheckNumber(node, schema, problems);
                break;
            case JsonNodeKind.Object:
                CheckObject(node, schema, problems, depth);
                break;
            case JsonNodeKind.Array:
                CheckArray(node, schema, problems, depth);
                break;
        }

        CheckCombinations(node, schema, problems, depth);
    }

    private void CheckCombinations(
        JsonNodeModel node,
        JsonNodeModel schema,
        List<JsonSchemaProblemModel> problems,
        int depth)
    {
        foreach (JsonNodeModel part in schema.Member("allOf")?.Items ?? [])
        {
            Check(node, part, problems, depth + 1);
        }

        if (schema.Member("anyOf") is { Kind: JsonNodeKind.Array } anyOf
            && !anyOf.Items.Any(part => IsValid(node, part, depth)))
        {
            Add(problems, node, Closest(node, anyOf.Items, depth) ?? "Doesn't match any of the schema's choices");
        }

        if (schema.Member("oneOf") is { Kind: JsonNodeKind.Array } oneOf)
        {
            int matching = oneOf.Items.Count(part => IsValid(node, part, depth));

            if (matching == 0)
            {
                Add(problems, node, Closest(node, oneOf.Items, depth) ?? "Doesn't match any of the schema's choices");
            }
            else if (matching > 1)
            {
                Add(problems, node, $"Matches {matching} of the schema's choices, where it should match one");
            }
        }

        if (schema.Member("not") is JsonNodeModel not && IsValid(node, not, depth))
        {
            Add(problems, node, "Matches what the schema says it shouldn't");
        }

        if (schema.Member("if") is JsonNodeModel condition)
        {
            JsonNodeModel? branch = IsValid(node, condition, depth) ? schema.Member("then") : schema.Member("else");

            if (branch is not null)
            {
                Check(node, branch, problems, depth + 1);
            }
        }
    }

    // When no choice matches, what the one closest to matching says: the one with fewest problems
    private string? Closest(JsonNodeModel node, IReadOnlyList<JsonNodeModel> choices, int depth)
    {
        List<JsonSchemaProblemModel>? best = null;

        foreach (JsonNodeModel choice in choices)
        {
            List<JsonSchemaProblemModel> found = [];
            Check(node, choice, found, depth + 1);

            if (best is null || found.Count < best.Count)
            {
                best = found;
            }
        }

        return best is [JsonSchemaProblemModel only] ? only.Message : null;
    }

    private static void CheckString(JsonNodeModel node, JsonNodeModel schema, List<JsonSchemaProblemModel> problems)
    {
        int length = new StringInfo(node.Text).LengthInTextElements;

        if (NumberOf(schema, "minLength") is double min && length < min)
        {
            Add(problems, node, $"At least {min} characters");
        }

        if (NumberOf(schema, "maxLength") is double max && length > max)
        {
            Add(problems, node, $"At most {max} characters");
        }

        if (schema.Member("pattern") is { Kind: JsonNodeKind.String } pattern && !Matches(pattern.Text, node.Text))
        {
            Add(problems, node, $"Should match {pattern.Text}");
        }
    }

    private static void CheckNumber(JsonNodeModel node, JsonNodeModel schema, List<JsonSchemaProblemModel> problems)
    {
        double value = node.Number;
        double? minimum = NumberOf(schema, "minimum");
        double? maximum = NumberOf(schema, "maximum");

        // Draft 4 says exclusive with a true beside minimum or maximum; later drafts give the number itself
        bool exclusiveMin = schema.Member("exclusiveMinimum")?.Kind == JsonNodeKind.True;
        bool exclusiveMax = schema.Member("exclusiveMaximum")?.Kind == JsonNodeKind.True;

        if (minimum is double min && (exclusiveMin ? value <= min : value < min))
        {
            Add(problems, node, exclusiveMin ? $"More than {min}" : $"At least {min}");
        }

        if (maximum is double max && (exclusiveMax ? value >= max : value > max))
        {
            Add(problems, node, exclusiveMax ? $"Less than {max}" : $"At most {max}");
        }

        if (NumberOf(schema, "exclusiveMinimum") is double above && value <= above)
        {
            Add(problems, node, $"More than {above}");
        }

        if (NumberOf(schema, "exclusiveMaximum") is double below && value >= below)
        {
            Add(problems, node, $"Less than {below}");
        }

        if (NumberOf(schema, "multipleOf") is double step && step > 0
            && Math.Abs(Math.IEEERemainder(value, step)) > 1e-9 * Math.Max(1, Math.Abs(value)))
        {
            Add(problems, node, $"Should be a multiple of {step}");
        }
    }

    private void CheckObject(JsonNodeModel node, JsonNodeModel schema, List<JsonSchemaProblemModel> problems, int depth)
    {
        foreach (JsonNodeModel name in schema.Member("required")?.Items ?? [])
        {
            if (node.Member(name.Text) is null)
            {
                Add(problems, node, $"Missing \"{name.Text}\"", atStart: true);
            }
        }

        JsonNodeModel? properties = schema.Member("properties");
        JsonNodeModel? patterns = schema.Member("patternProperties");
        JsonNodeModel? additional = schema.Member("additionalProperties");

        foreach ((string key, JsonNodeModel value) in node.Members)
        {
            bool described = false;

            if (properties?.Member(key) is JsonNodeModel property)
            {
                described = true;
                Check(value, property, problems, depth + 1);
            }

            foreach ((string pattern, JsonNodeModel patternSchema) in patterns?.Members ?? [])
            {
                if (Matches(pattern, key))
                {
                    described = true;
                    Check(value, patternSchema, problems, depth + 1);
                }
            }

            if (described || additional is null)
            {
                continue;
            }

            if (additional.Kind == JsonNodeKind.False)
            {
                Add(problems, value, $"\"{key}\" isn't one of the names allowed here");
            }
            else
            {
                Check(value, additional, problems, depth + 1);
            }
        }

        if (NumberOf(schema, "minProperties") is double min && node.Members.Count < min)
        {
            Add(problems, node, $"At least {min} members", atStart: true);
        }

        if (NumberOf(schema, "maxProperties") is double max && node.Members.Count > max)
        {
            Add(problems, node, $"At most {max} members", atStart: true);
        }
    }

    private void CheckArray(JsonNodeModel node, JsonNodeModel schema, List<JsonSchemaProblemModel> problems, int depth)
    {
        JsonNodeModel? items = schema.Member("items");
        JsonNodeModel? prefix = schema.Member("prefixItems") ?? (items?.Kind == JsonNodeKind.Array ? items : null);
        JsonNodeModel? rest = items?.Kind == JsonNodeKind.Array ? schema.Member("additionalItems") : items;

        for (int i = 0; i < node.Items.Count; i++)
        {
            JsonNodeModel? itemSchema = prefix is not null && i < prefix.Items.Count ? prefix.Items[i] : rest;

            if (itemSchema is not null)
            {
                Check(node.Items[i], itemSchema, problems, depth + 1);
            }
        }

        if (NumberOf(schema, "minItems") is double min && node.Items.Count < min)
        {
            Add(problems, node, $"At least {min} items", atStart: true);
        }

        if (NumberOf(schema, "maxItems") is double max && node.Items.Count > max)
        {
            Add(problems, node, $"At most {max} items", atStart: true);
        }

        if (schema.Member("uniqueItems")?.Kind == JsonNodeKind.True)
        {
            for (int i = 1; i < node.Items.Count; i++)
            {
                if (node.Items.Take(i).Any(earlier => Equal(earlier, node.Items[i])))
                {
                    Add(problems, node.Items[i], "The same as an item before it, where items should differ");
                }
            }
        }

        if (schema.Member("contains") is JsonNodeModel contains
            && !node.Items.Any(item => IsValid(item, contains, depth)))
        {
            Add(problems, node, "No item is what the schema says one should be", atStart: true);
        }
    }

    private bool IsValid(JsonNodeModel node, JsonNodeModel schema, int depth)
    {
        List<JsonSchemaProblemModel> found = [];
        Check(node, schema, found, depth + 1);

        return found.Count == 0;
    }

    // A $ref to a place in this schema: #, #/definitions/name, #/$defs/name, any JSON pointer
    private JsonNodeModel? Resolve(JsonNodeModel schema)
    {
        if (schema.Member("$ref") is not { Kind: JsonNodeKind.String } reference || !reference.Text.StartsWith('#'))
        {
            return null;
        }

        JsonNodeModel? target = _schema;

        foreach (string part in reference.Text[1..].Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            string name = Uri.UnescapeDataString(part).Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);
            target = target?.Kind == JsonNodeKind.Array && int.TryParse(name, out int index)
                ? (index < target.Items.Count ? target.Items[index] : null)
                : target?.Member(name);
        }

        return target;
    }

    private static bool IsOfType(JsonNodeModel node, JsonNodeModel type) => type.Kind == JsonNodeKind.Array
        ? type.Items.Any(item => IsOfType(node, item.Text))
        : IsOfType(node, type.Text);

    private static bool IsOfType(JsonNodeModel node, string type) => type switch
    {
        "integer" => node.Kind == JsonNodeKind.Number && node.Number == Math.Floor(node.Number)
            && !double.IsInfinity(node.Number),
        "number" => node.Kind == JsonNodeKind.Number,
        "boolean" => node.Kind is JsonNodeKind.True or JsonNodeKind.False,
        _ => TypeOf(node) == type,
    };

    private static string TypeOf(JsonNodeModel node) => node.Kind switch
    {
        JsonNodeKind.True or JsonNodeKind.False => "boolean",
        _ => node.Kind.ToString().ToLowerInvariant(),
    };

    private static string Article(string type) => type switch
    {
        "object" or "array" or "integer" => $"an {type}",
        "null" => "null",
        _ => $"a {type}",
    };

    private static double? NumberOf(JsonNodeModel schema, string keyword) =>
        schema.Member(keyword) is { Kind: JsonNodeKind.Number } number ? number.Number : null;

    // A pattern the schema gets wrong, or too slow, matches: the schema's fault isn't the document's
    private static bool Matches(string pattern, string text)
    {
        try
        {
            return Regex.IsMatch(text, pattern, RegexOptions.None, _patternTimeout);
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
        {
            return true;
        }
    }

    private static bool Equal(JsonNodeModel a, JsonNodeModel b)
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
                && a.Items.Zip(b.Items).All(pair => Equal(pair.First, pair.Second)),
            JsonNodeKind.Object => a.Members.Count == b.Members.Count
                && a.Members.All(member => b.Member(member.Key) is JsonNodeModel other && Equal(member.Value, other)),
            _ => true,
        };
    }

    // On the whole value, or only its first character: a missing member is said at the object's {
    private static void Add(
        List<JsonSchemaProblemModel> problems,
        JsonNodeModel node,
        string message,
        bool atStart = false)
    {
        if (node.Start is not DiffPosition start || node.End is not DiffPosition end)
        {
            return;
        }

        DiffPosition until = atStart ? start with { Column = start.Column + 1 } : end;
        problems.Add(new JsonSchemaProblemModel(start, until, JsonPathWriter.PathOf(node), message));
    }
}
