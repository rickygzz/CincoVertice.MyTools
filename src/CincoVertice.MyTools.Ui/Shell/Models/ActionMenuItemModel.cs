using System.Text;

namespace CincoVertice.MyTools.Ui.Shell.Models;

/// <summary>An action in the tray's Actions menu, so it can be run without a hotkey.</summary>
/// <param name="Name">The action's name, as in hotkeys.cfg, e.g. SnipAndEdit.</param>
/// <param name="Shortcut">Its hotkey as hotkeys.cfg has it, e.g. Ctrl+Alt+S; null when it has none.</param>
/// <param name="RunAsync">Runs the action, as its hotkey would without a value.</param>
public sealed record ActionMenuItemModel(string Name, string? Shortcut, Func<Task> RunAsync)
{
    /// <summary>The name as words, for the menu: SnipAndEdit is "Snip and edit".</summary>
    public string Title => TitleOf(Name);

    /// <returns>
    ///     The words of a name written as one, only the first capitalized: MoveToNextZone is "Move to next zone".
    /// </returns>
    public static string TitleOf(string name)
    {
        var title = new StringBuilder(name.Length + 8);

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];

            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
            {
                title.Append(' ');
            }

            title.Append(i == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
        }

        return title.ToString();
    }
}
