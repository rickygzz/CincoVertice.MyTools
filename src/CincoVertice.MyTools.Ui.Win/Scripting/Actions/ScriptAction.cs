using CincoVertice.Common.Application.Scripting;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Scripting.Actions;

/// <summary>
///     Runs one script file from config\scripts. The file is read on every run, so edits apply without a restart.
///     A second press of the hotkey while the script runs (e.g. while it asks something) is ignored.
/// </summary>
public sealed class ScriptAction : IAppAction
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

    public async Task ExecuteAsync()
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;

        try
        {
            string source = await File.ReadAllTextAsync(FilePath);

            await _engine.RunAsync(source, new DesktopScriptHost(Name, _notifications));
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

    private void ShowError(string message)
    {
        MessageBox.Show(message, $"My Tools: {Name}", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
