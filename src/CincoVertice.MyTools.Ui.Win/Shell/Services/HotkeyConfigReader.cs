using CincoVertice.Common.Utils.Config;
using CincoVertice.Common.WinApi.Hotkey;
using CincoVertice.Common.WinApi.Libs;
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
    public static HotkeyConfigResultModel Read(string text, IReadOnlyCollection<string> actionNames) =>
        Read(text, actionNames, c => User32.VkKeyScan(c));

    /// <summary>
    ///     Reads hotkeys.cfg, finding the keys of characters with <paramref name="keyScan"/> instead of the current
    ///     keyboard layout, e.g. in tests.
    /// </summary>
    /// <param name="text">The content of hotkeys.cfg.</param>
    /// <param name="actionNames">The names of the actions that can be bound.</param>
    /// <param name="keyScan">Like VkKeyScan: the key and shift state of a character; -1 when no key types it.</param>
    public static HotkeyConfigResultModel Read(
        string text,
        IReadOnlyCollection<string> actionNames,
        Func<char, short> keyScan)
    {
        var result = new HotkeyConfigResultModel();
        ConfigItems items = new ConfigLexer(text).Items;

        // An action can have several lines when each passes a different argument. Ordinal: the action's spelling
        // comes from actionNames, and arguments that differ only in case are different arguments
        HashSet<string> boundActions = new(StringComparer.Ordinal);
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

            if (!HotkeyParser.TryParse(shortcut, keyScan, out FSModifiers modifiers, out KeyCode key, out string error))
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

        foreach (var (number, line) in ConfigLineChecker.FindUnreadLines(text, items))
        {
            result.Problems.Add($"Line {number}: '{line}' is not 'Action = Shortcut'.");
        }

        return result;
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

    /// <summary>
    ///     Removes a comment after the shortcut, which the lexer keeps in the value. A # starts a comment when it
    ///     starts the value or follows a space, unless it is the shortcut's own key: "Ctrl+# # note" is the # key with
    ///     a comment, and "Ctrl+1, app#1" passes app#1.
    /// </summary>
    private static string WithoutComment(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            string before = value[..i].TrimEnd();

            if (value[i] == '#'
                && (i == 0 || char.IsWhiteSpace(value[i - 1]))
                && (before.Length == 0 || before[^1] != '+'))
            {
                return before;
            }
        }

        return value.Trim();
    }
}
