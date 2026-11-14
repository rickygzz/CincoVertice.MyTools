using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Presenters;

namespace CincoVertice.MyTools.Ui.Win.Zones.Services;

/// <summary>
///     The one window zones editor window: asking for it while it's open brings it to the front, so two editors never
///     save over each other. The window disposes itself when closed.
/// </summary>
public sealed class ZoneEditorWindowService
{
    private readonly ZonesConfigService _zones;
    private readonly IZoneDesktop _desktop;
    private readonly ConfigEditorWindowService _configEditor;
    private readonly AppIconService _appIcon;
    private readonly HelpWindowService _help;

    private ZoneEditorForm? _form;

    public ZoneEditorWindowService(
        ZonesConfigService zones,
        IZoneDesktop desktop,
        ConfigEditorWindowService configEditor,
        AppIconService appIcon,
        HelpWindowService help)
    {
        _zones = zones;
        _desktop = desktop;
        _configEditor = configEditor;
        _appIcon = appIcon;
        _help = help;
    }

    public void Open()
    {
        if (_form is null)
        {
            _form = new ZoneEditorForm { Icon = _appIcon.Current };
            _form.FormClosed += Form_FormClosed;
            _help.OpenOnF1(_form, () => HelpTopics.WindowZones);
            new ZoneEditorPresenter(_form, _zones, _desktop, OpenAsText).Start();
            _form.Show();
        }

        if (_form.WindowState == FormWindowState.Minimized)
        {
            _form.WindowState = FormWindowState.Normal;
        }

        _form.Activate();
    }

    private void OpenAsText()
    {
        // Created first, with the default layout, so there's something to edit
        _zones.LoadResult();
        _configEditor.Open(_zones.FilePath);
    }

    private void Form_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _form = null;
    }
}
