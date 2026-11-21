using CincoVertice.MyTools.Ui.Spotlight.Presenters;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.Spotlight.Actions;

/// <summary>
///     Shows the app spotlight around the active window, as tapping Right Ctrl twice does
///     (<see cref="Services.SpotlightWatcher"/>). It has no hotkey by default.
/// </summary>
public sealed class ShowAppSpotlightAction : IAppAction
{
    private readonly AppSpotlightPresenter _presenter;

    public ShowAppSpotlightAction(AppSpotlightPresenter presenter)
    {
        _presenter = presenter;
    }

    public string Name => "ShowAppSpotlight";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        _presenter.Show();

        return Task.CompletedTask;
    }
}
