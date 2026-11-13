using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Presenters;
using CincoVertice.MyTools.Ui.Win.Help.Services;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     The one snip editor window: a new snip goes in it while it's open, with the earlier ones kept to go back to.
///     The window disposes itself and all its snips when closed.
/// </summary>
public sealed class SnipEditorWindowService
{
    private readonly ISnipEditorSettingsStore _settingsStore;
    private readonly ITextRecognizer _textRecognizer;
    private readonly HelpWindowService _help;

    private SnipEditorForm? _form;

    public SnipEditorWindowService(
        ISnipEditorSettingsStore settingsStore,
        ITextRecognizer textRecognizer,
        HelpWindowService help)
    {
        _settingsStore = settingsStore;
        _textRecognizer = textRecognizer;
        _help = help;
    }

    /// <param name="snip">The image to edit; the window owns it from here and disposes it.</param>
    public void Show(Bitmap snip)
    {
        if (_form is null)
        {
            _form = new SnipEditorForm(snip);
            _form.FormClosed += Form_FormClosed;
            _help.OpenOnF1(_form, () => HelpTopics.Snipping);
            new SnipEditorPresenter(_form, TimeProvider.System, _settingsStore, _textRecognizer).Start();
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

    /// <summary>
    ///     Keeps the open editor out of the screenshot of a snip, when the editor's settings say so: the user may also
    ///     want to snip the editor itself.
    /// </summary>
    /// <returns>What shows it in screenshots again; null when it isn't left out.</returns>
    public IDisposable? KeepOutOfSnip()
    {
        if (_form is not { IsDisposed: false, Visible: true }
            || _settingsStore.Load()?.HideWhileSnipping == false)
        {
            return null;
        }

        // It stays on the screen as it is; only Windows older than 10, version 2004, needs it see-through instead
        return (IDisposable?)CaptureExclusion.Exclude([_form.Handle]) ?? new SeeThrough(_form);
    }

    private void Form_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _form = null;
    }

    /// <summary>The window invisible, but where it is, for as long as this isn't disposed.</summary>
    private sealed class SeeThrough : IDisposable
    {
        private readonly Form _form;
        private readonly double _opacity;

        public SeeThrough(Form form)
        {
            _form = form;
            _opacity = form.Opacity;
            form.Opacity = 0;

            // The screen keeps it until it's drawn again
            Dwmapi.DwmFlush();
        }

        public void Dispose()
        {
            if (!_form.IsDisposed)
            {
                _form.Opacity = _opacity;
            }
        }
    }
}
