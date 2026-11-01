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

        // An action can have several lines when each passes a different argument
        HashSet<string> boundActions = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<(FSModifiers, KeyCode), string> usedShortcuts = [];

        for (int i = 0; i < items.KeysCount; i++)
        {
            string name = items.GetKeyName(i);
            var (shortcut, argument) = SplitArgument(WithoutComment(items.GetValue(i)));
            string? action = actionNames.FirstOrDefault(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (action is null)
            {
                result.Problems.Add($"{name}: unknown action. Actions: {string.Join(", ", actionNames)}.");

                continue;
            }

            if (!boundActions.Add(action + "\0" + argument))
            {
                string with = argument is null ? string.Empty : $" with {argument}";
                result.Problems.Add($"{name}{with}: set more than once; the first line is used.");

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
            result.Bindings.Add(new HotkeyBindingModel(action, shortcut, modifiers, key, argument));
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

    /// <summary>
    ///     Splits "Ctrl+Alt+1, app1" at the first comma that is not the shortcut's own key, so "Ctrl+," is the comma
    ///     key and "Ctrl+,, app1" passes app1. The argument is null when there is none.
    /// </summary>
    private static (string Shortcut, string? Argument) SplitArgument(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            string before = value[..i].TrimEnd();

            if (value[i] == ',' && before.Length > 0 && before[^1] != '+')
            {
                string argument = value[(i + 1)..].Trim();

                return (before, argument.Length > 0 ? argument : null);
            }
        }

        return (value, null);
    }

    // A # after the shortcut is a comment; the lexer keeps it in the value
    private static string WithoutComment(string value)
    {
        int hash = value.IndexOf('#');

        return (hash < 0 ? value : value[..hash]).Trim();
    }
}
