using System.Text;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Models;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Owns hotkeys.cfg: creates it with each action's default shortcut the first time, and reads it.
/// </summary>
public sealed class HotkeyConfigService
{
    public HotkeyConfigService(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>
    ///     config\hotkeys.cfg next to the executable. Based on the executable's folder, not the current directory,
    ///     which depends on how MyTools was started (e.g. C:\Windows\System32 from autostart).
    /// </summary>
    public static string DefaultFilePath { get; } = Path.Combine(
        AppContext.BaseDirectory,
        "config",
        "hotkeys.cfg");

    public string FilePath { get; }

    /// <summary>
    ///     Reads the bindings, creating the file with the defaults when it doesn't exist. When the file can't be
    ///     read or written, the defaults are used and the reason is reported as a problem.
    /// </summary>
    public HotkeyConfigResultModel Load(IReadOnlyCollection<IAppAction> actions)
    {
        List<string> actionNames = [.. actions.Select(a => a.Name)];
        string defaults = CreateDefaultText(actions);

        try
        {
            if (!File.Exists(FilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, defaults);
            }

            return HotkeyConfigReader.Read(File.ReadAllText(FilePath), actionNames);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var result = HotkeyConfigReader.Read(defaults, actionNames);
            result.Problems.Insert(0, $"Could not use {FilePath}, so the default hotkeys are used: {ex.Message}");

            return result;
        }
    }

    /// <summary>
    ///     The content of a new hotkeys.cfg: a line per action, commented out when it has no default shortcut.
    /// </summary>
    public static string CreateDefaultText(IEnumerable<IAppAction> actions)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# MyTools hotkeys: one line per hotkey, Action = Shortcut. Restart MyTools after editing.");
        sb.AppendLine("# Modifiers: Ctrl, Alt, Shift, Win.");
        sb.AppendLine("# Keys: A-Z, 0-9, F1-F24, NumPad0-NumPad9, Space, Enter, Esc, Tab, Delete, Insert, Home, End,");
        sb.AppendLine("#       PageUp, PageDown, Up, Down, Left, Right, PrintScreen, Plus, Minus.");
        sb.AppendLine("# A line starting with # is ignored: remove the # to turn a hotkey on, add it to turn one off.");
        sb.AppendLine();

        foreach (var action in actions.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine(
                action.DefaultShortcut is null
                ? $"# {action.Name} = Ctrl+Alt+"
                : $"{action.Name} = {action.DefaultShortcut}");
        }

        return sb.ToString();
    }
}
