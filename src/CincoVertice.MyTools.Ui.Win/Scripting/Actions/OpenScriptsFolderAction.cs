using System.Diagnostics;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.Scripting.Actions;

/// <summary>
///     Opens config\scripts in Explorer, creating it when it doesn't exist yet, to add or edit scripts.
/// </summary>
public sealed class OpenScriptsFolderAction : IAppAction
{
    public string Name => "OpenScriptsFolder";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        Directory.CreateDirectory(ScriptCatalog.FolderPath);

        using var explorer = Process.Start(new ProcessStartInfo(ScriptCatalog.FolderPath) { UseShellExecute = true });

        return Task.CompletedTask;
    }
}
