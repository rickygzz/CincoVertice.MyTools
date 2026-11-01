using CincoVertice.Common.Utils.Config;
using CincoVertice.Common.WinApi.Hotkey;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Shell.Models;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Reads hotkeys.cfg with the config lexer: one "Action = Shortcut" per line, # for comment lines.
///     Every line that can't be used is reported as a problem and skipped; the other lines still work.
/// </summary>
public static class HotkeyConfigReader
{
    /// <param name="text">The content of hotkeys.cfg.</param>
    /// <param name="actionNames">The names of the actions that can be bound.</param>
    public static HotkeyConfigResultModel Read(string text, IReadOnlyCollection<string> actionNames)
    {
        var result = new HotkeyConfigResultModel();
        ConfigItems items = new ConfigLexer(text).Items;

        HashSet<string> boundActions = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<(FSModifiers, KeyCode), string> usedShortcuts = [];

        for (int i = 0; i < items.KeysCount; i++)
        {
            string name = items.GetKeyName(i);
            string shortcut = WithoutComment(items.GetValue(i));
            string? action = actionNames.FirstOrDefault(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (action is null)
            {
                result.Problems.Add($"{name}: unknown action. Actions: {string.Join(", ", actionNames)}.");

                continue;
            }

            if (!boundActions.Add(action))
            {
                result.Problems.Add($"{name}: set more than once; the first line is used.");

                continue;
            }

            if (!HotkeyParser.TryParse(shortcut, out FSModifiers modifiers, out KeyCode key, out string error))
            {
                result.Problems.Add($"{name}: {error}");

                continue;
            }

            if (usedShortcuts.TryGetValue((modifiers, key), out string? other))
            {
                result.Problems.Add($"{name}: {shortcut} is already used by {other}.");

                continue;
            }

            usedShortcuts[(modifiers, key)] = action;
            result.Bindings.Add(new HotkeyBindingModel(action, shortcut, modifiers, key));
        }

        AddSkippedLines(text, items, result);

        return result;
    }

    /// <summary>
    ///     The lexer drops lines it can't read (e.g. "Ctrl+Alt+A = ShowCalculator") without a trace, so every line
    ///     that is not blank, not a comment and not one of the parsed keys is reported.
    /// </summary>
    private static void AddSkippedLines(string text, ConfigItems items, HotkeyConfigResultModel result)
    {
        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < items.KeysCount; i++)
        {
            keys.Add(items.GetKeyName(i));
        }

        string[] lines = text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            int equals = line.IndexOf('=');

            if (equals < 0 || !keys.Contains(line[..equals].Trim()))
            {
                result.Problems.Add($"Line {i + 1}: '{line}' is not 'Action = Shortcut'.");
            }
        }
    }

    // A # after the shortcut is a comment; the lexer keeps it in the value
    private static string WithoutComment(string value)
    {
        int hash = value.IndexOf('#');

        return (hash < 0 ? value : value[..hash]).Trim();
    }
}
