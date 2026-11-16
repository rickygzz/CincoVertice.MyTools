using CincoVertice.MyTools.Ui.ConfigEditor.Models;

namespace CincoVertice.MyTools.Ui.ConfigEditor.Services;

/// <summary>What the suggesters of every kind of file share: reading around the caret, narrowing, shortcuts.</summary>
public static class SuggestionText
{
    /// <summary>The modifiers of a shortcut, as hotkeys.cfg writes them.</summary>
    public static IReadOnlyList<string> Modifiers { get; } = ["Ctrl", "Alt", "Shift", "Win"];

    /// <returns>Where the caret's line starts, and its text up to the caret.</returns>
    public static (int Start, string Before) LineBefore(string text, int caret)
    {
        caret = Math.Clamp(caret, 0, text.Length);
        int start = caret == 0 ? 0 : text.LastIndexOf('\n', caret - 1) + 1;

        return (start, text[start..caret]);
    }

    /// <returns>The letters, digits and _ just before the end of the text: the word being typed.</returns>
    public static string WordAtEnd(string text)
    {
        int start = text.Length;

        while (start > 0 && (char.IsLetterOrDigit(text[start - 1]) || text[start - 1] == '_'))
        {
            start--;
        }

        return text[start..];
    }

    /// <summary>
    ///     The items that start with what was typed, then, unless <paramref name="onlyStarting"/>, those that have it
    ///     elsewhere, ignoring case; none left out when nothing was typed.
    /// </summary>
    public static List<CodeSuggestionModel> Narrowed(
        IEnumerable<CodeSuggestionModel> items,
        string typed,
        bool onlyStarting = false)
    {
        List<CodeSuggestionModel> all = [.. items];

        if (typed.Length == 0)
        {
            return all;
        }

        return
        [
            .. all.Where(item => item.Text.StartsWith(typed, StringComparison.OrdinalIgnoreCase)),
            .. all.Where(item => !onlyStarting
                && !item.Text.StartsWith(typed, StringComparison.OrdinalIgnoreCase)
                && item.Text.Contains(typed, StringComparison.OrdinalIgnoreCase)),
        ];
    }

    /// <summary>
    ///     The next part of a shortcut being written, such as "Ctrl+Alt+": the modifiers it doesn't have yet, which
    ///     write their "+" too, then the keys.
    /// </summary>
    /// <param name="shortcut">The shortcut up to the caret.</param>
    /// <param name="keyDetail">What to say beside a key: the whole shortcut it would make is given.</param>
    /// <param name="extraModifiers">Modifiers besides <see cref="Modifiers"/>, such as AltGr in scripts.</param>
    /// <returns>The suggestions, and the length of the part typed so far; null when the part isn't a name.</returns>
    public static (List<CodeSuggestionModel> Items, int TypedLength)? ShortcutPart(
        string shortcut,
        IReadOnlyList<string> keys,
        Func<string, string> keyDetail,
        IReadOnlyList<string>? extraModifiers = null)
    {
        int partStart = shortcut.LastIndexOf('+') + 1;
        string typed = shortcut[partStart..].TrimStart();

        if (typed.Any(c => !char.IsLetterOrDigit(c)))
        {
            return null;
        }

        HashSet<string> used = new(
            shortcut[..partStart].Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase);
        string head = shortcut[..partStart].Trim();

        var items = new List<CodeSuggestionModel>();
        items.AddRange(Modifiers.Concat(extraModifiers ?? [])
            .Where(modifier => !used.Contains(modifier))
            .Select(modifier => new CodeSuggestionModel(modifier, "Modifier", modifier + "+")));
        items.AddRange(keys.Select(key => new CodeSuggestionModel(key, keyDetail(head + key))));

        // Only from the start: a key of one letter is in many names
        return (Narrowed(items, typed, onlyStarting: true), typed.Length);
    }

    /// <returns>A shortcut written one way, for comparing: its modifiers sorted, then its key, ignoring case.</returns>
    public static string Normalized(string shortcut)
    {
        string[] parts = shortcut.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return string.Empty;
        }

        IEnumerable<string> modifiers = parts[..^1]
            .Select(part => part.ToLowerInvariant() switch
            {
                "control" => "ctrl",
                "windows" => "win",
                string other => other,
            })
            .Order(StringComparer.Ordinal);

        return string.Join("+", modifiers.Append(parts[^1].ToLowerInvariant()));
    }
}
