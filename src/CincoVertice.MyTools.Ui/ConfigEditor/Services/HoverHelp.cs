using CincoVertice.MyTools.Ui.ConfigEditor.Models;

namespace CincoVertice.MyTools.Ui.ConfigEditor.Services;

/// <summary>
///     What the config editor shows when the pointer rests on a name: an action, a setting, a script function or
///     module, from the same lists its suggestions come from.
/// </summary>
public static class HoverHelp
{
    /// <returns>The name at a place: letters, digits, _ and dots, as clipboard.gettext; empty for none.</returns>
    public static string WordAt(string text, int position)
    {
        if (position < 0 || position >= text.Length || !IsNameChar(text[position]))
        {
            return string.Empty;
        }

        int start = position;
        int end = position;

        while (start > 0 && IsNameChar(text[start - 1]))
        {
            start--;
        }

        while (end < text.Length && IsNameChar(text[end]))
        {
            end++;
        }

        return text[start..end].Trim('.');
    }

    /// <returns>"Name: what it does" for one of the items, by its name ignoring case; null for none.</returns>
    public static string? Named(string word, IEnumerable<CodeSuggestionModel> items) =>
        items.FirstOrDefault(item => item.Text.Equals(word, StringComparison.OrdinalIgnoreCase)) is { } item
            ? $"{item.Text}: {item.Detail}"
            : null;

    /// <returns>"Name: what it means" for a setting; null for none.</returns>
    public static string? Setting(string word, IEnumerable<ConfigSettingModel> settings) =>
        Named(word, settings.Select(setting => new CodeSuggestionModel(setting.Name, setting.Detail)));

    /// <returns>
    ///     A function's signature and description, or a module's functions; null for anything else, such as a
    ///     variable.
    /// </returns>
    public static string? Script(string word, IReadOnlyList<ScriptFunctionModel> functions)
    {
        if (functions.FirstOrDefault(function => function.Name.Equals(word, StringComparison.OrdinalIgnoreCase))
            is { } found)
        {
            return $"{found.Signature}{Environment.NewLine}{found.Description}";
        }

        List<string> names =
        [
            .. functions
                .Where(function => function.Module.Equals(word, StringComparison.OrdinalIgnoreCase))
                .Select(function => function.ShortName),
        ];

        return names.Count == 0 ? null : $"{word.ToLowerInvariant()}: {string.Join(", ", names)}";
    }

    private static bool IsNameChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '.';
}
