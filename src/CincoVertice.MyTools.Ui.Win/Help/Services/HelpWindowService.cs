using CincoVertice.MyTools.Ui.Help.Interfaces;
using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Presenters;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Help.Services;

/// <summary>
///     The one help window: asking for it while it's open brings it to the front, at the page asked for, if any. The
///     window disposes itself when closed.
/// </summary>
public sealed class HelpWindowService
{
    private readonly IHelpPageStore _pages;
    private readonly IHelpSettingsStore _settings;
    private readonly AppIconService _appIcon;

    private HelpForm? _form;
    private HelpPresenter? _presenter;

    public HelpWindowService(IHelpPageStore pages, IHelpSettingsStore settings, AppIconService appIcon)
    {
        _pages = pages;
        _settings = settings;
        _appIcon = appIcon;
    }

    /// <summary>Where the window keeps its place and last page: config\help.json.</summary>
    public static string SettingsFilePath { get; } = Path.Combine(AppContext.BaseDirectory, "config", "help.json");

    /// <param name="link">
    ///     The page to show, and a heading in it, e.g. "04-snipping.md#recordingcfg"; null for the contents, or what's
    ///     shown.
    /// </param>
    public void Open(string? link = null)
    {
        HelpLinkModel? place = link is null ? null : HelpLinkModel.Parse(link);

        if (_form is null || _presenter is null)
        {
            _form = new HelpForm { Icon = _appIcon.Current };
            _form.FormClosed += Form_FormClosed;
            _presenter = new HelpPresenter(_form, _pages, _settings);

            // Where it was, then shown: the page is laid out at the window's size
            _presenter.RestoreWindow();
            _form.Show();
            _presenter.Start(place?.Page, place?.Anchor);
        }
        else if (place?.Page is not null)
        {
            _presenter.Open(place.Page, place.Anchor);
        }

        if (_form.WindowState == FormWindowState.Minimized)
        {
            _form.WindowState = FormWindowState.Normal;
        }

        _form.Activate();
    }

    /// <summary>Makes F1 in a window open the guide at the page about it.</summary>
    /// <param name="topic">The page, as a link; asked at each F1, for a window whose page changes.</param>
    public void OpenOnF1(Form form, Func<string> topic)
    {
        form.OpenHelpOnF1(topic, Open);
    }

    private void Form_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _form = null;
        _presenter = null;
    }
}
