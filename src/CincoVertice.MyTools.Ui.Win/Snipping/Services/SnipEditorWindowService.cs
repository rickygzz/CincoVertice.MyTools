using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Presenters;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     The one snip editor window: a new snip goes in it while it's open, with the earlier ones kept to go back to.
///     The window disposes itself and all its snips when closed.
/// </summary>
public sealed class SnipEditorWindowService
{
    private readonly ISnipEditorSettingsStore _settingsStore;

    private SnipEditorForm? _form;

    public SnipEditorWindowService(ISnipEditorSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    /// <param name="snip">The image to edit; the window owns it from here and disposes it.</param>
    public void Show(Bitmap snip)
    {
        if (_form is null)
        {
            _form = new SnipEditorForm(snip);
            _form.FormClosed += Form_FormClosed;
            new SnipEditorPresenter(_form, TimeProvider.System, _settingsStore).Start();
            _form.Show();

            return;
        }

        // Back from the taskbar first, so the window is fitted to the new snip
        if (_form.WindowState == FormWindowState.Minimized)
        {
            _form.WindowState = FormWindowState.Normal;
        }

        _form.AddSnip(snip);

        // From a global hotkey, so another application is active by now
        WindowHelper.ForceForeground(_form.Handle);
        _form.Activate();
    }

    private void Form_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _form = null;
    }
}
