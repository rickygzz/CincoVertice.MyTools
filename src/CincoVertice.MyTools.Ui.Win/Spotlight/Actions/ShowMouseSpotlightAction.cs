using CincoVertice.MyTools.Ui.Spotlight.Presenters;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.Spotlight.Actions;

/// <summary>
///     Shows the mouse spotlight, as tapping Left Ctrl twice does (<see cref="Services.SpotlightWatcher"/>). Not a
///     toggle: the hotkey's own key press hides it, as any key does. It has no hotkey by default.
/// </summary>
public sealed class ShowMouseSpotlightAction : IAppAction
{
    private readonly SpotlightPresenter _presenter;

    public ShowMouseSpotlightAction(SpotlightPresenter presenter)
    {
        _presenter = presenter;
    }

    public string Name => "ShowMouseSpotlight";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        _presenter.Show();

        return Task.CompletedTask;
    }
}
