using CincoVertice.MyTools.Ui.Help.Interfaces;
using CincoVertice.MyTools.Ui.Help.Presenters;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Help.Services;

/// <summary>
///     The one help window: asking for it while it's open brings it to the front, at the page asked for, if any. The
///     window disposes itself when closed.
/// </summary>
public sealed class HelpWindowService
{
    private readonly IHelpPageStore _pages;
    private readonly AppIconService _appIcon;

    private HelpForm? _form;
    private HelpPresenter? _presenter;

    public HelpWindowService(IHelpPageStore pages, AppIconService appIcon)
    {
        _pages = pages;
        _appIcon = appIcon;
    }

    /// <param name="page">The page to show, e.g. "13-window-zones.md"; null for the contents, or what's shown.</param>
    public void Open(string? page = null)
    {
        if (_form is null || _presenter is null)
        {
            _form = new HelpForm { Icon = _appIcon.Current };
            _form.FormClosed += Form_FormClosed;

            // Shown first: the page is laid out at the window's size
            _form.Show();
            _presenter = new HelpPresenter(_form, _pages);
            _presenter.Start(page);
        }
        else if (page is not null)
        {
            _presenter.Open(page);
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
