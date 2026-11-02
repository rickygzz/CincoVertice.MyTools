using CincoVertice.Common.Application.Scripting;
using CincoVertice.Common.Application.Scripting.Models;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Scripting.Actions;

/// <summary>
///     Runs one script file from config\scripts. The file is read on every run, so edits apply without a restart.
///     A second press of the hotkey while the script runs (e.g. while it asks something) is ignored.
/// </summary>
public sealed class ScriptAction : IArgumentAppAction
{
    private readonly ScriptEngine _engine;
    private readonly TrayNotificationService _notifications;

    private bool _isRunning;

    public ScriptAction(string filePath, ScriptEngine engine, TrayNotificationService notifications)
    {
        FilePath = filePath;
        Name = ScriptCatalog.GetActionName(filePath);
        _engine = engine;
        _notifications = notifications;
    }

    public string FilePath { get; }

    public string Name { get; }

    public string? DefaultShortcut => null;

    public Task ExecuteAsync() => ExecuteAsync(string.Empty);

    /// <param name="argument">What script.argument() returns, e.g. from the hotkeys.cfg line; empty for none.</param>
    public async Task ExecuteAsync(string argument)
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;

        try
        {
            string source = await File.ReadAllTextAsync(FilePath);

            var options = new ScriptRunOptions { Argument = argument, ReadInclude = ReadInclude };

            await _engine.RunAsync(source, new DesktopScriptHost(Name, _notifications), options);
        }
        catch (ScriptException ex)
        {
            ShowError($"{Path.GetFileName(FilePath)}, {ex.Description}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowError($"Could not read {FilePath}: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
        }
    }

    /// <summary>
    ///     The source of script.include(name): a script in the same folder, with or without ".mts".
    /// </summary>
    private string ReadInclude(string name)
    {
        string fileName = name.EndsWith(ScriptCatalog.Extension, StringComparison.OrdinalIgnoreCase)
            ? name
            : name + ScriptCatalog.Extension;

        if (fileName.IndexOfAny(['/', '\\', ':']) >= 0 || fileName.StartsWith('.'))
        {
            throw new InvalidOperationException($"'{name}' must be the name of a script in the scripts folder");
        }

        string folder = Path.GetDirectoryName(FilePath)!;
        string path = Path.Combine(folder, fileName);

        return File.Exists(path)
            ? File.ReadAllText(path)
            : throw new InvalidOperationException($"There is no script {fileName} in {folder}");
    }

    private void ShowError(string message)
    {
        MessageBox.Show(message, $"My Tools: {Name}", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
