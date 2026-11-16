using CincoVertice.MyTools.Ui.ConfigEditor.Models;

namespace CincoVertice.MyTools.Ui.ConfigEditor.Services;

/// <summary>
///     Suggestions for hotkeys.cfg, by where the caret is on its line (<c>Action = Shortcut, value</c>): the actions at
///     the start, written with " = " when the line is new; the modifiers and keys after the =, each key saying which
///     action already has the shortcut it would make; zone numbers after MoveToZone's comma. Nothing in comments.
/// </summary>
public static class HotkeysSuggester
{
    public const string MoveToZone = "MoveToZone";

    /// <param name="isExplicit">Asked for with Ctrl+Space, so a word not started yet gets suggestions too.</param>
    /// <param name="actions">The actions, with what each does.</param>
    /// <param name="keys">The names of the keys a shortcut can end with.</param>
    /// <param name="zoneCount">The most zones a monitor has, for MoveToZone's value.</param>
    public static CodeSuggestionsModel? Suggest(
        string text,
        int caret,
        bool isExplicit,
        IReadOnlyList<CodeSuggestionModel> actions,
        IReadOnlyList<string> keys,
        int zoneCount)
    {
        (int lineStart, string before) = SuggestionText.LineBefore(text, caret);

        if (before.TrimStart().StartsWith('#'))
        {
            return null;
        }

        int equals = before.IndexOf('=');

        if (equals < 0)
        {
            return Actions(text, caret, isExplicit, before, actions);
        }

        string action = before[..equals].Trim();
        int comma = before.IndexOf(',', equals);

        if (comma >= 0)
        {
            return Zones(caret, before, action, zoneCount);
        }

        (List<CodeSuggestionModel> Items, int TypedLength)? part = SuggestionText.ShortcutPart(
            before[(equals + 1)..],
            keys,
            shortcut => KeyDetail(text, lineStart, shortcut));

        return part is { } found ? Of(caret - found.TypedLength, found.TypedLength, found.Items) : null;
    }

    private static CodeSuggestionsModel? Actions(
        string text,
        int caret,
        bool isExplicit,
        string before,
        IReadOnlyList<CodeSuggestionModel> actions)
    {
        string typed = SuggestionText.WordAtEnd(before);

        // Only a name at the start of the line, typed or asked for
        if (before.TrimStart().Length != typed.Length || (typed.Length == 0 && !isExplicit))
        {
            return null;
        }

        int lineEnd = text.IndexOf('\n', caret);
        string after = text[caret..(lineEnd < 0 ? text.Length : lineEnd)];
        bool isNewLine = after.Trim().Length == 0;
        IEnumerable<CodeSuggestionModel> items = actions.Select(action =>
            isNewLine ? action with { Insert = action.Text + " = " } : action);

        return Of(caret - typed.Length, typed.Length, SuggestionText.Narrowed(items, typed));
    }

    private static CodeSuggestionsModel? Zones(int caret, string before, string action, int zoneCount)
    {
        if (!action.Equals(MoveToZone, StringComparison.OrdinalIgnoreCase) || zoneCount <= 0)
        {
            return null;
        }

        string typed = SuggestionText.WordAtEnd(before);
        IEnumerable<CodeSuggestionModel> zones = Enumerable.Range(1, zoneCount)
            .Select(zone => new CodeSuggestionModel(zone.ToString(), $"Zone {zone}"));

        return Of(caret - typed.Length, typed.Length, SuggestionText.Narrowed(zones, typed));
    }

    // "Key", or which action already has the shortcut it would make, on another line
    private static string KeyDetail(string text, int lineStart, string shortcut)
    {
        string wanted = SuggestionText.Normalized(shortcut);
        int start = 0;

        foreach (string line in text.Split('\n'))
        {
            int equals = line.IndexOf('=');

            if (start != lineStart && equals > 0 && !line.TrimStart().StartsWith('#'))
            {
                string bound = line[(equals + 1)..];
                int comma = bound.IndexOf(',');

                if (SuggestionText.Normalized(comma < 0 ? bound : bound[..comma]) == wanted)
                {
                    return $"Key; already used by {line[..equals].Trim()}";
                }
            }

            start += line.Length + 1;
        }

        return "Key";
    }

    private static CodeSuggestionsModel? Of(int start, int length, List<CodeSuggestionModel> items) =>
        items.Count == 0 ? null : new CodeSuggestionsModel(start, length, items);
}
