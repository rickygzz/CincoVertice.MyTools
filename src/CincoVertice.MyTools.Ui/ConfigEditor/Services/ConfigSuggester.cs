using CincoVertice.MyTools.Ui.ConfigEditor.Models;

namespace CincoVertice.MyTools.Ui.ConfigEditor.Services;

/// <summary>
///     Suggestions for the config files of <c>Name = Value</c> lines (activity.cfg, recording.cfg, startup.cfg,
///     zones.cfg): the names at the start of a line, those already set in the same section left out unless they can
///     be repeated, written with " = " on a new line; after the =, the values of that name; after a value's comma,
///     what that value takes. Nothing in comments.
/// </summary>
public static class ConfigSuggester
{
    /// <param name="settings">The names the file can set.</param>
    public static CodeSuggestionsModel? Suggest(
        string text,
        int caret,
        SuggestionTrigger trigger,
        IReadOnlyList<ConfigSettingModel> settings)
    {
        (int lineStart, string before) = SuggestionText.LineBefore(text, caret);

        if (before.TrimStart().StartsWith('#'))
        {
            return null;
        }

        int equals = before.IndexOf('=');

        if (equals < 0)
        {
            return Names(text, caret, lineStart, before, trigger, settings);
        }

        string name = before[..equals].Trim();
        ConfigSettingModel? setting =
            settings.FirstOrDefault(known => known.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (setting is null)
        {
            return null;
        }

        string value = before[(equals + 1)..];
        int comma = setting.ArgumentsOf is null ? -1 : value.IndexOf(',');

        if (comma >= 0)
        {
            string typedArgument = value[(comma + 1)..].TrimStart();

            return Of(caret, typedArgument, setting.ArgumentsOf!(value[..comma].Trim()));
        }

        return Of(caret, value.TrimStart(), setting.Values);
    }

    private static CodeSuggestionsModel? Names(
        string text,
        int caret,
        int lineStart,
        string before,
        SuggestionTrigger trigger,
        IReadOnlyList<ConfigSettingModel> settings)
    {
        string typed = SuggestionText.WordAtEnd(before);

        if (before.TrimStart().Length != typed.Length || (typed.Length == 0 && trigger == SuggestionTrigger.Typing))
        {
            return null;
        }

        HashSet<string> set = SetInSection(text, lineStart, settings);
        int lineEnd = text.IndexOf('\n', caret);
        bool isNewLine = text[caret..(lineEnd < 0 ? text.Length : lineEnd)].Trim().Length == 0;
        IEnumerable<CodeSuggestionModel> items = settings
            .Where(setting => setting.IsRepeatable || !set.Contains(setting.Name))
            .Select(setting => new CodeSuggestionModel(
                setting.Name,
                setting.Detail,
                isNewLine ? setting.Name + " = " : setting.Name));

        return Of(caret, typed, items);
    }

    // The names set on the other lines of the caret's section: from the line that starts it to the next one
    private static HashSet<string> SetInSection(string text, int lineStart, IReadOnlyList<ConfigSettingModel> settings)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int start = 0;

        foreach (string line in text.Split('\n'))
        {
            int equals = line.IndexOf('=');
            string name = equals > 0 && !line.TrimStart().StartsWith('#') ? line[..equals].Trim() : string.Empty;
            bool startsSection = settings.Any(setting =>
                setting.StartsSection && setting.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (start > lineStart && startsSection)
            {
                break;
            }

            if (startsSection)
            {
                set.Clear();
            }
            else if (start != lineStart && name.Length > 0)
            {
                set.Add(name);
            }

            start += line.Length + 1;
        }

        return set;
    }

    private static CodeSuggestionsModel? Of(int caret, string typed, IEnumerable<CodeSuggestionModel> items)
    {
        List<CodeSuggestionModel> narrowed = SuggestionText.Narrowed(items, typed);

        return narrowed.Count == 0 ? null : new CodeSuggestionsModel(caret - typed.Length, typed.Length, narrowed);
    }
}
