using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Shell.Actions;

/// <summary>
///     Turns activity on or off, e.g. from the global hotkey: starts or stops the mouse move timer, and shows the
///     red icon while it runs.
/// </summary>
public sealed class ToggleActivityAction
{
    private readonly MouseMoveSimulator _mouseMoveSimulator;
    private readonly AppIconService _appIconService;

    public ToggleActivityAction(MouseMoveSimulator mouseMoveSimulator, AppIconService appIconService)
    {
        _mouseMoveSimulator = mouseMoveSimulator;
        _appIconService = appIconService;
    }

    public Task ExecuteAsync()
    {
        if (_mouseMoveSimulator.IsRunning)
        {
            _mouseMoveSimulator.Stop();
        }
        else
        {
            _mouseMoveSimulator.Start();
        }

        _appIconService.ShowActive(_mouseMoveSimulator.IsRunning);

        return Task.CompletedTask;
    }
}
