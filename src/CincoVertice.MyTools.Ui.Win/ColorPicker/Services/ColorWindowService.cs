using CincoVertice.MyTools.Ui.ColorPicker.Presenters;
using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.ColorPicker.Services;

/// <summary>
///     The one color window: a color picked while it's open is shown in it, whether by the hotkey or by its
///     Pick another button. The window disposes itself when closed.
/// </summary>
public sealed class ColorWindowService
{
    private readonly AppIconService _appIcon;
    private readonly HelpWindowService _help;
    private readonly ScreenColorPicker _picker;

    private ColorForm? _form;
    private ColorPresenter? _presenter;

    public ColorWindowService(AppIconService appIcon, HelpWindowService help, ScreenColorPicker picker)
    {
        _appIcon = appIcon;
        _help = help;
        _picker = picker;
    }

    public void Show(Color color)
    {
        if (_form is null || _presenter is null)
        {
            _form = new ColorForm { Icon = _appIcon.Current };
            _presenter = new ColorPresenter(_form, _picker);
            _form.FormClosed += Form_FormClosed;
            _help.OpenOnF1(_form, () => HelpTopics.ColorPicker);
            _presenter.Show(color);
            _form.Show();
        }
        else
        {
            _presenter.Show(color);
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
