using CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;
using CincoVertice.MyTools.Ui.ConfigEditor.Presenters;
using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;

/// <summary>
///     The one config editor window: asking for a file while it's open shows that file in it, so a file is never open
///     twice with different changes. The window disposes itself when closed.
/// </summary>
public sealed class ConfigEditorWindowService
{
    private readonly IEditableFileCatalog _catalog;
    private readonly IEditableFileStore _store;
    private readonly IEditableFileLanguage _language;
    private readonly AppIconService _appIcon;
    private readonly HelpWindowService _help;

    private ConfigEditorForm? _form;
    private ConfigEditorPresenter? _presenter;

    public ConfigEditorWindowService(
        IEditableFileCatalog catalog,
        IEditableFileStore store,
        IEditableFileLanguage language,
        AppIconService appIcon,
        HelpWindowService help)
    {
        _catalog = catalog;
        _store = store;
        _language = language;
        _appIcon = appIcon;
        _help = help;
    }

    /// <param name="path">The file to show, or null for the one open, or the first.</param>
    public void Open(string? path)
    {
        if (_form is null || _presenter is null)
        {
            _form = new ConfigEditorForm { Icon = _appIcon.Current };
            _presenter = new ConfigEditorPresenter(_form, _catalog, _store, _language);
            _form.FormClosed += Form_FormClosed;

            // The page about the file open then: hotkeys.cfg's, a script's...
            _help.OpenOnF1(_form, () => HelpTopics.ForFile(_presenter?.FilePath));
            _presenter.Open(path);
            _form.Show();
        }
        else
        {
            _presenter.Open(path);
        }

        if (_form.WindowState == FormWindowState.Minimized)
        {
            _form.WindowState = FormWindowState.Normal;
        }

        _form.Activate();
    }

    private void Form_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _form = null;
        _presenter = null;
    }
}
