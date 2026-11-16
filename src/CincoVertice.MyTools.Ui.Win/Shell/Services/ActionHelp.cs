namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     What each built-in action does, in a sentence, for the config editor's suggestions. A test checks every action
///     registered has one: a new action needs its line here. Scripts are described as scripts.
/// </summary>
public static class ActionHelp
{
    private static readonly Dictionary<string, string> _descriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ShowCalculator"] = "Shows the calculator",
        ["ToggleActivity"] = "Starts or stops the mouse mover",
        ["ToggleKeepAwake"] = "Starts or stops keeping the computer awake",
        ["EditActivitySettings"] = "Opens activity.cfg in the config editor",
        ["SnipAndEdit"] = "Takes a snip, then opens the snip editor",
        ["SnipRegion"] = "Takes a snip to the clipboard",
        ["SnipText"] = "Takes a snip and copies the text in it",
        ["RecordScreen"] = "Records a video of an area of the screen; again to stop",
        ["PickColor"] = "Picks the color of a pixel on the screen",
        ["DiagramEditor"] = "Opens a new diagram editor window",
        ["CompareFiles"] = "Opens a new Compare files window",
        ["CheckFolder"] = "Picks a folder and checks the C# standard",
        ["FixFolder"] = "Picks a folder and fixes it in place, with the fixers ticked in the tray menu",
        ["EditHotkeys"] = "Opens hotkeys.cfg in the config editor",
        ["EditScripts"] = "Opens the first script in the config editor",
        ["OpenScriptsFolder"] = "Opens config\\scripts in Explorer",
        ["MoveToZone"] = "Moves the active window to the zone given after the shortcut: MoveToZone = Ctrl+Alt+1, 1",
        ["MoveToNextZone"] = "Moves the active window to the next zone",
        ["MoveToPreviousZone"] = "Moves the active window to the zone before",
        ["EditZones"] = "Opens the window zones editor",
        ["ShowHelp"] = "Opens the user guide",
        ["ShowMouseSpotlight"] = "Shows the mouse spotlight, as tapping Left Ctrl twice does",
    };

    /// <returns>What the action does, or null for one not built in, such as a script.</returns>
    public static string? DescriptionOf(string name) => _descriptions.GetValueOrDefault(name);
}
