using System.Globalization;
using CincoVertice.MyTools.Ui.JsonEditor.JmesPath;
using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Services;

/// <summary>
///     What the search box offers while typing. For text: the document's names and values that hold what's typed. For
///     JMESPath: the names found in the values the expression reaches before the name being typed (after a ., in a
///     filter's elements, after a |), and the functions where a value starts.
/// </summary>
public static class JsonSuggester
{
    public const int MaxSuggestions = 30;

    // The values looked through for names: enough for an array of records
    private const int MaxValuesLooked = 500;

    public static IReadOnlyList<JsonSuggestionModel> Suggest(JsonSearchMode mode, string query, JsonNodeModel? root) =>
        root is null ? [] : mode == JsonSearchMode.Text ? ForText(query, root) : ForJmesPath(query, root);

    private static List<JsonSuggestionModel> ForText(string query, JsonNodeModel root)
    {
        if (query.Length == 0)
        {
            return [];
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        List<JsonSuggestionModel> suggestions = [];
        CompareInfo compare = CultureInfo.InvariantCulture.CompareInfo;

        void Offer(string text, string detail)
        {
            if (suggestions.Count < MaxSuggestions && text != query
                && compare.IndexOf(text, query, CompareOptions.IgnoreCase) >= 0 && seen.Add(text))
            {
                suggestions.Add(new JsonSuggestionModel(text, detail, text, 0));
            }
        }

        foreach (JsonNodeModel node in Walk(root))
        {
            if (node.Key is string key)
            {
                Offer(key, "name");
            }

            if (node.Kind is JsonNodeKind.String or JsonNodeKind.Number)
            {
                Offer(node.Text, JmesFunctions.TypeOf(node));
            }
        }

        return suggestions;
    }

    private static List<JsonSuggestionModel> ForJmesPath(string query, JsonNodeModel root)
    {
        int start = query.Length;

        while (start > 0 && (char.IsAsciiLetterOrDigit(query[start - 1]) || query[start - 1] == '_'))
        {
            start--;
        }

        string partial = query[start..];
        (List<JsonNodeModel> values, bool offersFunctions) = Reached(query[..start], root);
        List<JsonSuggestionModel> suggestions = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (JsonNodeModel value in values.SelectMany(ObjectsIn).Take(MaxValuesLooked))
        {
            foreach (KeyValuePair<string, JsonNodeModel> member in value.Members)
            {
                if (member.Key.StartsWith(partial, StringComparison.OrdinalIgnoreCase) && seen.Add(member.Key))
                {
                    suggestions.Add(new JsonSuggestionModel(
                        member.Key,
                        Describe(member.Value),
                        JsonPathWriter.Name(member.Key),
                        start));
                }
            }
        }

        if (offersFunctions)
        {
            suggestions.AddRange(JmesFunctions.Signatures
                .Where(function => partial.Length > 0
                    && function.Key.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                .Select(function =>
                    new JsonSuggestionModel(function.Key + "()", function.Value, function.Key + "(", start)));
        }

        return [.. suggestions.Take(MaxSuggestions)];
    }

    /// <returns>
    ///     The values whose names go where the expression ends, and whether a function can go there, as at the start,
    ///     in a filter or after a |.
    /// </returns>
    private static (List<JsonNodeModel> Values, bool OffersFunctions) Reached(string before, JsonNodeModel root)
    {
        int filter = OpenFilterAt(before);

        if (filter >= 0)
        {
            List<JsonNodeModel> elements = [.. ItemsOf(Run(before[..filter], root))];

            return Relative(before[(filter + 2)..], elements);
        }

        return Relative(before, [root]);
    }

    // The last part of an expression, after anything that starts a new value: what its names are relative to
    private static (List<JsonNodeModel> Values, bool OffersFunctions) Relative(
        string text,
        List<JsonNodeModel> contexts)
    {
        // After a pipe, names are those of what the left side gives
        int pipe = LastPipeAt(text);

        if (pipe >= 0)
        {
            string left = text[..pipe];
            contexts = [.. contexts.Select(context => Run(left, context)).OfType<JsonNodeModel>()];
            text = text[(pipe + 1)..];
        }

        int separator = text.LastIndexOfAny([' ', '(', ',', '!', '=', '<', '>', '&', '{', ':']);
        string segment = text[(separator + 1)..];

        if (segment.Length == 0)
        {
            return (contexts, true);
        }

        if (!segment.EndsWith('.'))
        {
            return ([], false);
        }

        string path = segment[..^1];

        return ([.. contexts.Select(context => Run(path, context)).OfType<JsonNodeModel>().SelectMany(Spread)], false);
    }

    // The last |, not of ||; -1 for none
    private static int LastPipeAt(string text)
    {
        for (int i = text.Length - 1; i >= 0; i--)
        {
            if (text[i] == '|' && (i == 0 || text[i - 1] != '|') && (i + 1 >= text.Length || text[i + 1] != '|'))
            {
                return i;
            }
        }

        return -1;
    }

    // Where an unclosed [? starts, outside quotes; -1 for none
    private static int OpenFilterAt(string text)
    {
        Stack<int> open = new();
        char quote = '\0';

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (quote != '\0')
            {
                quote = c == quote ? '\0' : quote;
            }
            else if (c is '"' or '\'' or '`')
            {
                quote = c;
            }
            else if (c == '[')
            {
                open.Push(i + 1 < text.Length && text[i + 1] == '?' ? i : -1);
            }
            else if (c == ']' && open.Count > 0)
            {
                open.Pop();
            }
        }

        return open.FirstOrDefault(at => at >= 0, -1);
    }

    private static JsonNodeModel? Run(string expression, JsonNodeModel context)
    {
        if (expression.Trim().Length == 0)
        {
            return context;
        }

        try
        {
            return JmesEvaluator.Search(expression, context);
        }
        catch (JmesPathException)
        {
            return null;
        }
    }

    // A projection's result is the values it gathered
    private static IEnumerable<JsonNodeModel> Spread(JsonNodeModel value) =>
        value.Kind == JsonNodeKind.Array && !value.IsInText ? value.Items : [value];

    private static IEnumerable<JsonNodeModel> ItemsOf(JsonNodeModel? value) =>
        value?.Kind == JsonNodeKind.Array ? value.Items : [];

    private static IEnumerable<JsonNodeModel> ObjectsIn(JsonNodeModel value) => value.Kind switch
    {
        JsonNodeKind.Object => [value],
        JsonNodeKind.Array => value.Items.Where(item => item.Kind == JsonNodeKind.Object),
        _ => [],
    };

    private static IEnumerable<JsonNodeModel> Walk(JsonNodeModel node)
    {
        Stack<JsonNodeModel> pending = new([node]);

        while (pending.Count > 0)
        {
            JsonNodeModel current = pending.Pop();

            yield return current;

            // In reverse, so the first comes out first
            List<JsonNodeModel> children = [.. current.Items, .. current.Members.Select(member => member.Value)];

            for (int i = children.Count - 1; i >= 0; i--)
            {
                pending.Push(children[i]);
            }
        }
    }

    /// <returns>A value in a few words: string "Ann", number 42, object of 3, array of 12.</returns>
    public static string Describe(JsonNodeModel value) => value.Kind switch
    {
        JsonNodeKind.String => $"string \"{(value.Text.Length > 24 ? value.Text[..24] + "…" : value.Text)}\"",
        JsonNodeKind.Number => $"number {value.Text}",
        JsonNodeKind.Object => $"object of {value.Members.Count}",
        JsonNodeKind.Array => $"array of {value.Items.Count}",
        JsonNodeKind.True => "true",
        JsonNodeKind.False => "false",
        _ => "null",
    };
}
