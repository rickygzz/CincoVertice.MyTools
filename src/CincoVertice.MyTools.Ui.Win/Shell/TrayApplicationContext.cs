using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.CodeReview.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Shell;

/// <summary>
///     Root of the application. Lives in the systray, starts the global hotkeys
///     and shows the main form on demand.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly MainForm _mainForm;
    private readonly CheckFolderAction _checkFolderAction;
    private readonly FixFolderAction _fixFolderAction;
    private readonly AppIconService _appIconService;
    private readonly NotifyIcon _notifyIcon;

    public TrayApplicationContext(
        MainForm mainForm,
        GlobalHotkeyService hotkeyService,
        CheckFolderAction checkFolderAction,
        FixFolderAction fixFolderAction,
        AppIconService appIconService)
    {
        _mainForm = mainForm;
        _checkFolderAction = checkFolderAction;
        _fixFolderAction = fixFolderAction;
        _appIconService = appIconService;
        _appIconService.Changed += AppIcon_Changed;
        _mainForm.Resize += MainForm_Resize;
        // X hides to the systray
        _mainForm.HideOnUserClose();

        _notifyIcon = new NotifyIcon
        {
            Icon = _appIconService.Current,
            Text = "My Tools",
            BalloonTipTitle = "My Tools",
            ContextMenuStrip = BuildContextMenu(),
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ShowMainForm();

        hotkeyService.Start();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var open = new ToolStripMenuItem("&Open", null, (_, _) => ShowMainForm());
        open.Font = new Font(open.Font, FontStyle.Bold);

        menu.Items.Add(open);
        menu.Items.Add(new ToolStripMenuItem(
            "Check folder for &standard...",
            null,
            async (_, _) => await _checkFolderAction.ExecuteAsync()));
        menu.Items.Add(new ToolStripMenuItem(
            "&Fix folder for standard...",
            null,
            async (_, _) => await _fixFolderAction.ExecuteAsync()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("&Exit", null, (_, _) => Exit()));

        return menu;
    }

    private void ShowMainForm()
    {
        _mainForm.Show();
        _mainForm.WindowState = FormWindowState.Normal;
        _mainForm.Activate();
    }

    private void AppIcon_Changed(object? sender, EventArgs e)
    {
        _notifyIcon.Icon = _appIconService.Current;
        _mainForm.Icon = _appIconService.Current;
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (_mainForm.WindowState == FormWindowState.Minimized)
        {
            _mainForm.Hide();
        }
    }

    private void Exit()
    {
        // Hide first, otherwise the icon lingers in the systray until the mouse hovers over it.
        _notifyIcon.Visible = false;

        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _appIconService.Changed -= AppIcon_Changed;
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
