using CincoVertice.MyTools.Ui.Shell.Interfaces;
using CincoVertice.MyTools.Ui.Shell.Models;
using CincoVertice.MyTools.Ui.Shell.Services;
using CincoVertice.MyTools.Ui.Shell.Views;

namespace CincoVertice.MyTools.Ui.Shell.Presenters;

/// <summary>
///     Which version this is, and whether a newer one is out. Asked from the menu, it always answers; at start, when
///     that's turned on, it only speaks up when there's a newer one, since nobody asked. It never downloads anything:
///     it opens the release page.
/// </summary>
public class UpdatesPresenter
{
    private readonly IUpdatesView _view;
    private readonly IReleaseSource _releases;
    private readonly IUpdateSettingsStore _settings;
    private readonly Version _current;
    private readonly TimeSpan _startDelay;

    // A second click while it's still asking would only ask again
    private bool _isChecking;

    /// <param name="view">The tray menu.</param>
    /// <param name="releases">Where the releases are.</param>
    /// <param name="settings">Whether to look at start.</param>
    /// <param name="current">This app's version.</param>
    /// <param name="startDelay">How long after start to look: at sign-in, the network may not be up yet.</param>
    public UpdatesPresenter(
        IUpdatesView view,
        IReleaseSource releases,
        IUpdateSettingsStore settings,
        Version current,
        TimeSpan startDelay)
    {
        _view = view;
        _releases = releases;
        _settings = settings;
        _current = current;
        _startDelay = startDelay;

        _view.AboutRequested += OnAboutRequested;
        _view.CheckForUpdatesRequested += OnCheckForUpdatesRequested;
        _view.CheckAtStartRequested += OnCheckAtStartRequested;
    }

    /// <summary>
    ///     Shows the setting and, when it's on, looks for a newer version after the delay. Call once, on the UI
    ///     thread; it never throws, so the task needn't be awaited.
    /// </summary>
    public async Task Start()
    {
        bool checkAtStart = _settings.LoadCheckAtStart();

        _view.ShowCheckAtStart(checkAtStart);

        if (!checkAtStart)
        {
            return;
        }

        if (_startDelay > TimeSpan.Zero)
        {
            await Task.Delay(_startDelay);
        }

        // Turned off while it waited
        if (!_settings.LoadCheckAtStart())
        {
            return;
        }

        if (await TryGetNewerAsync() is { } newer)
        {
            _view.NotifyUpdateAvailable(newer);
        }
    }

    /// <summary>
    ///     Looks now, and says what it found: a newer version, that this is the latest, or why it can't tell.
    /// </summary>
    public async Task CheckNowAsync()
    {
        if (_isChecking)
        {
            return;
        }

        _isChecking = true;

        try
        {
            AppReleaseModel? latest = await _releases.GetLatestAsync();

            if (latest is not null && latest.Version > _current)
            {
                if (_view.AskToOpenRelease(latest, _current))
                {
                    _view.OpenPage(latest.PageUrl);
                }
            }
            else
            {
                _view.ShowUpToDate(_current);
            }
        }
        catch (UpdateCheckException ex)
        {
            _view.ShowUpdateCheckFailed(ex.Message);
        }
        finally
        {
            _isChecking = false;
        }
    }

    /// <summary>
    ///     The latest release when it's newer than this one; null when it isn't, or it can't be found out.
    /// </summary>
    private async Task<AppReleaseModel?> TryGetNewerAsync()
    {
        try
        {
            AppReleaseModel? latest = await _releases.GetLatestAsync();

            return latest is not null && latest.Version > _current ? latest : null;
        }
        catch (UpdateCheckException)
        {
            // Nobody asked, so a failure isn't worth a notification; the menu's Check for updates says why
            return null;
        }
    }

    private void OnAboutRequested(object? sender, EventArgs e)
    {
        _view.ShowAbout(_current, _releases.ReleasesPageUrl);
    }

    private async void OnCheckForUpdatesRequested(object? sender, EventArgs e)
    {
        await CheckNowAsync();
    }

    private void OnCheckAtStartRequested(object? sender, bool enabled)
    {
        _settings.SaveCheckAtStart(enabled);
        _view.ShowCheckAtStart(_settings.LoadCheckAtStart());
    }
}
