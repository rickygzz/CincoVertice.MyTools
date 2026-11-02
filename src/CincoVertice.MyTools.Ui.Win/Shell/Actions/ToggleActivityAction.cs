using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

namespace CincoVertice.MyTools.Ui.Win.Shell.Actions;

/// <summary>
///     Turns activity on or off, e.g. from the global hotkey: starts or stops the mouse move timer, and shows the
///     red icon while it runs.
/// </summary>
public sealed class ToggleActivityAction : IAppAction
{
    private readonly MouseMoveSimulator _mouseMoveSimulator;
    private readonly AppIconService _appIconService;
    private readonly ActivitySettingsService _settingsService;
    private readonly TrayNotificationService _notifications;

    public ToggleActivityAction(
        MouseMoveSimulator mouseMoveSimulator,
        AppIconService appIconService,
        ActivitySettingsService settingsService,
        TrayNotificationService notifications)
    {
        _mouseMoveSimulator = mouseMoveSimulator;
        _appIconService = appIconService;
        _settingsService = settingsService;
        _notifications = notifications;
    }

    public string Name => "ToggleActivity";

    public string? DefaultShortcut => "Ctrl+Alt+M";

    public Task ExecuteAsync()
    {
        if (_mouseMoveSimulator.IsRunning)
        {
            _mouseMoveSimulator.Stop();
        }
        else
        {
            ApplySettings();
            _mouseMoveSimulator.Start();
        }

        _appIconService.ShowActive(_mouseMoveSimulator.IsRunning);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Reads activity.cfg on every start, so edits apply the next time activity is turned on.
    /// </summary>
    private void ApplySettings()
    {
        ActivitySettingsResultModel result = _settingsService.Load();

        _mouseMoveSimulator.Settings = result.Settings;

        if (result.Problems.Count > 0)
        {
            _notifications.Show("activity.cfg", string.Join(Environment.NewLine, result.Problems), ToolTipIcon.Warning);
        }
    }
}
