namespace CincoVertice.MyTools.Ui.Help.Services;

/// <summary>
///     The user guide's page about each window, which F1 opens there, as links: "page.md", or "page.md#heading".
///     A test checks each against the guide, so renaming a page or heading there fails until this follows.
/// </summary>
public static class HelpTopics
{
    public const string Calculator = "03-calculator.md";
    public const string Snipping = "04-snipping.md";
    public const string DiagramEditor = "05-diagram-editor.md";
    public const string ConfigEditor = "07-config-editor.md";
    public const string Scripts = "08-scripts.md";
    public const string CodeReview = "09-code-review.md";
    public const string WindowZones = "13-window-zones.md";

    /// <summary>Every topic, files' ones included, for the test.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Calculator, Snipping, DiagramEditor, ConfigEditor, Scripts, CodeReview, WindowZones,
        ForFile("hotkeys.cfg"), ForFile("activity.cfg"), ForFile("recording.cfg"), ForFile("startup.cfg"),
    ];

    /// <returns>
    ///     The page about a file the config editor has open: hotkeys.cfg's is the hotkeys page, a script's the scripts
    ///     page; the config editor's page for any other, or none.
    /// </returns>
    public static string ForFile(string? path)
    {
        string name = Path.GetFileName(path ?? string.Empty).ToLowerInvariant();

        return name switch
        {
            "hotkeys.cfg" => "02-hotkeys.md",
            "activity.cfg" => "06-mouse-mover.md",
            "zones.cfg" => WindowZones,
            "recording.cfg" => Snipping + "#recordingcfg",
            "startup.cfg" => "01-getting-started.md#startup-actions",
            _ when name.EndsWith(".mts", StringComparison.Ordinal) => Scripts,
            _ => ConfigEditor,
        };
    }
}
