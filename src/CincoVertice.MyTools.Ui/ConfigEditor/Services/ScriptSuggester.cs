using System.Text.RegularExpressions;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;

namespace CincoVertice.MyTools.Ui.ConfigEditor.Services;

/// <summary>
///     Suggestions for scripts (.mts), by what's before the caret: a module's functions after "module.", with how
///     they're called and what they do; otherwise the variables the script sets, the keywords, the modules and the
///     global functions. Inside a text, only where a text names something: the libraries in script.include("..."), the
///     keys in keyboard.press("..."). Nothing in comments or numbers.
/// </summary>
public static partial class ScriptSuggester
{
    private const string BlockQuotes = "\"\"\"";

    /// <param name="isExplicit">Asked for with Ctrl+Space, so a word not started yet gets suggestions too.</param>
    /// <param name="functions">Every function, with how it's called and what it does.</param>
    /// <param name="keywords">The language's keywords.</param>
    /// <param name="libraries">The scripts made to be included, by name.</param>
    /// <param name="keys">The names of the keys keyboard.press can press.</param>
    public static CodeSuggestionsModel? Suggest(
        string text,
        int caret,
        bool isExplicit,
        IReadOnlyList<ScriptFunctionModel> functions,
        IReadOnlyList<string> keywords,
        IReadOnlyList<string> libraries,
        IReadOnlyList<string> keys)
    {
        (int lineStart, string before) = SuggestionText.LineBefore(text, caret);

        if (IsInBlockText(text, lineStart))
        {
            return null;
        }

        int quote = -1;

        for (int i = 0; i < before.Length; i++)
        {
            char c = before[i];

            if (quote >= 0)
            {
                if (c == '\\')
                {
                    i++;
                }
                else if (c == '"')
                {
                    quote = -1;
                }
            }
            else if (c == '#')
            {
                return null;
            }
            else if (c == '"')
            {
                quote = i;
            }
        }

        if (quote >= 0)
        {
            return InText(before, quote, caret, libraries, keys);
        }

        string typed = SuggestionText.WordAtEnd(before);
        int wordStart = before.Length - typed.Length;

        if (typed.Length > 0 && char.IsDigit(typed[0]))
        {
            return null;
        }

        if (wordStart > 0 && before[wordStart - 1] == '.')
        {
            string module = SuggestionText.WordAtEnd(before[..(wordStart - 1)]);

            return Of(caret, typed, ModuleFunctions(functions, module));
        }

        if (typed.Length == 0 && !isExplicit)
        {
            return null;
        }

        return Of(caret, typed, Words(text, lineStart, functions, keywords));
    }

    private static CodeSuggestionsModel? Of(int caret, string typed, IEnumerable<CodeSuggestionModel> items)
    {
        List<CodeSuggestionModel> narrowed = SuggestionText.Narrowed(items, typed);

        return narrowed.Count == 0 ? null : new CodeSuggestionsModel(caret - typed.Length, typed.Length, narrowed);
    }

    private static IEnumerable<CodeSuggestionModel> ModuleFunctions(
        IReadOnlyList<ScriptFunctionModel> functions,
        string module) =>
        functions
            .Where(function => function.Module.Equals(module, StringComparison.OrdinalIgnoreCase))
            .Select(function => new CodeSuggestionModel(
                function.ShortName,
                $"{function.Signature}: {function.Description}",
                function.ShortName + (function.HasArguments ? "(" : "()")));

    // A word on its own: the script's variables, keywords, modules, then the global functions
    private static IEnumerable<CodeSuggestionModel> Words(
        string text,
        int lineStart,
        IReadOnlyList<ScriptFunctionModel> functions,
        IReadOnlyList<string> keywords)
    {
        foreach (string variable in Variables(text, lineStart, keywords))
        {
            yield return new CodeSuggestionModel(variable, "Variable");
        }

        foreach (string keyword in keywords)
        {
            yield return new CodeSuggestionModel(keyword, "Keyword");
        }

        foreach (IGrouping<string, ScriptFunctionModel> module in functions
            .Where(function => function.Module.Length > 0)
            .GroupBy(function => function.Module))
        {
            string names = string.Join(", ", module.Select(function => function.ShortName));

            yield return new CodeSuggestionModel(module.Key, $"Module: {names}", module.Key + ".");
        }

        foreach (ScriptFunctionModel function in functions.Where(function => function.Module.Length == 0))
        {
            yield return new CodeSuggestionModel(function.Signature, function.Description, function.Name + "(");
        }
    }

    // The names set at the start of a line, other than the one being typed
    private static IEnumerable<string> Variables(string text, int lineStart, IReadOnlyList<string> keywords)
    {
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        int start = 0;

        foreach (string line in text.Split('\n'))
        {
            if (start != lineStart && AssignmentRegex().Match(line) is { Success: true } match
                && !keywords.Contains(match.Groups[1].Value, StringComparer.OrdinalIgnoreCase))
            {
                names.Add(match.Groups[1].Value);
            }

            start += line.Length + 1;
        }

        return names;
    }

    private static CodeSuggestionsModel? InText(
        string before,
        int quote,
        int caret,
        IReadOnlyList<string> libraries,
        IReadOnlyList<string> keys)
    {
        string inside = before[(quote + 1)..];
        Match call = CallRegex().Match(before[..quote]);

        if (!call.Success)
        {
            return null;
        }

        string function = call.Groups[1].Value;

        if (function.Equals("script.include", StringComparison.OrdinalIgnoreCase))
        {
            IEnumerable<CodeSuggestionModel> items =
                libraries.Select(library => new CodeSuggestionModel(library, "Library"));

            return Of(caret, inside, items);
        }

        if (function.Equals("keyboard.press", StringComparison.OrdinalIgnoreCase)
            && SuggestionText.ShortcutPart(inside, keys, _ => "Key", ["AltGr"]) is { } part)
        {
            return part.Items.Count == 0
                ? null
                : new CodeSuggestionsModel(caret - part.TypedLength, part.TypedLength, part.Items);
        }

        return null;
    }

    // Inside a text of several lines: after a line that ends with """, up to the line that starts with """
    private static bool IsInBlockText(string text, int lineStart)
    {
        bool isOpen = false;

        foreach (string line in text[..lineStart].Split('\n'))
        {
            isOpen = isOpen
                ? !line.TrimStart().StartsWith(BlockQuotes, StringComparison.Ordinal)
                : line.TrimEnd().EndsWith(BlockQuotes, StringComparison.Ordinal);
        }

        return isOpen;
    }

    [GeneratedRegex(@"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=(?!=)")]
    private static partial Regex AssignmentRegex();

    // The function a text is the first value of: name( just before it
    [GeneratedRegex(@"([A-Za-z_][A-Za-z0-9_.]*)\s*\(\s*$")]
    private static partial Regex CallRegex();
}
