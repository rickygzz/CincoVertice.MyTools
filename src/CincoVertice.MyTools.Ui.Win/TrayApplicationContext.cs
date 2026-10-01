using CincoVertice.MyTools.Ui.Win.Main;
using CincoVertice.MyTools.Ui.Win.Services;

namespace CincoVertice.MyTools.Ui.Win;

/// <summary>
///     Root of the application. Lives in the systray, starts the global hotkeys
///     and shows the main form on demand.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly MainForm _mainForm;
    private readonly NotifyIcon _notifyIcon;

    public TrayApplicationContext(MainForm mainForm, GlobalHotkeyService hotkeys)
    {
        _mainForm = mainForm;
        _mainForm.Resize += MainForm_Resize;
        _mainForm.FormClosing += MainForm_FormClosing;

        _notifyIcon = new NotifyIcon
        {
            Icon = Resources.Resources.RGIcon,
            Text = "My Tools",
            BalloonTipTitle = "My Tools",
            ContextMenuStrip = BuildContextMenu(),
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ShowMainForm();

        hotkeys.Start();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var open = new ToolStripMenuItem("&Open", null, (_, _) => ShowMainForm());
        open.Font = new Font(open.Font, FontStyle.Bold);

        menu.Items.Add(open);
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

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (_mainForm.WindowState == FormWindowState.Minimized)
        {
            _mainForm.Hide();
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // X hides to the systray; Windows shutdown, Task Manager, etc. still close normally.
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
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
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
