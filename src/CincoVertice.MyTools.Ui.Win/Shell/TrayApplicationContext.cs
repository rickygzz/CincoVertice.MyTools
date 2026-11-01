using System.Diagnostics;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.CodeReview.Actions;
using CincoVertice.MyTools.Ui.Win.Scripting.Actions;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Shell;

/// <summary>
///     Root of the application. Lives in the systray, starts the global hotkeys
///     and shows the main form on demand.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext
{
    // Menu item name, so Hotkey problems can be inserted above it
    private const string EditHotkeysKey = "EditHotkeys";

    private readonly MainForm _mainForm;
    private readonly AppIconService _appIconService;
    private readonly CheckFolderAction _checkFolderAction;
    private readonly FixFolderAction _fixFolderAction;
    private readonly EditHotkeysAction _editHotkeysAction;
    private readonly IReadOnlyList<ScriptAction> _scriptActions;
    private readonly TrayNotificationService _notifications;
    private readonly NotifyIcon _notifyIcon;

    public TrayApplicationContext(
        MainForm mainForm,
        AppIconService appIconService,
        GlobalHotkeyService hotkeyService,
        CheckFolderAction checkFolderAction,
        FixFolderAction fixFolderAction,
        EditHotkeysAction editHotkeysAction,
        IEnumerable<IAppAction> actions,
        TrayNotificationService notifications)
    {
        _mainForm = mainForm;
        _appIconService = appIconService;
        _checkFolderAction = checkFolderAction;
        _fixFolderAction = fixFolderAction;
        _editHotkeysAction = editHotkeysAction;
        _scriptActions = [.. actions.OfType<ScriptAction>()];
        _notifications = notifications;

        _mainForm.Resize += MainForm_Resize;
        _appIconService.Changed += AppIcon_Changed;
        _notifications.Requested += Notification_Requested;

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

        ShowHotkeyProblems(hotkeyService.Start());
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var open = new ToolStripMenuItem(
            "&Open",
            null,
            (_, _) => ShowMainForm());
        open.Font = new Font(open.Font, FontStyle.Bold);

        var checkFolder = new ToolStripMenuItem(
            "Check folder for &standard...",
            null,
            async (_, _) => await _checkFolderAction.ExecuteAsync());

        var fixFolder = new ToolStripMenuItem(
            "&Fix folder for standard...",
            null,
            async (_, _) => await _fixFolderAction.ExecuteAsync());

        var editHotkeys = new ToolStripMenuItem(
            "Edit &hotkeys...",
            null,
            async (_, _) => await _editHotkeysAction.ExecuteAsync());
        editHotkeys.Name = EditHotkeysKey;

        menu.Items.Add(open);
        menu.Items.Add(checkFolder);
        menu.Items.Add(fixFolder);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(BuildScriptsMenu());
        menu.Items.Add(editHotkeys);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("&Exit", null, (_, _) => Exit()));

        return menu;
    }

    /// <summary>
    ///     Scripts: one item per script in config\scripts, as found at startup, and the folder to add or edit them.
    /// </summary>
    private ToolStripMenuItem BuildScriptsMenu()
    {
        var scripts = new ToolStripMenuItem("S&cripts");

        foreach (var script in _scriptActions)
        {
            scripts.DropDownItems.Add(new ToolStripMenuItem(
                script.Name,
                null,
                async (_, _) => await script.ExecuteAsync()));
        }

        if (_scriptActions.Count > 0)
        {
            scripts.DropDownItems.Add(new ToolStripSeparator());
        }

        scripts.DropDownItems.Add(new ToolStripMenuItem(
            "Open scripts &folder",
            null,
            (_, _) => OpenScriptsFolder()));

        return scripts;
    }

    private static void OpenScriptsFolder()
    {
        Directory.CreateDirectory(ScriptCatalog.FolderPath);

        using var explorer = Process.Start(new ProcessStartInfo(ScriptCatalog.FolderPath) { UseShellExecute = true });
    }

    private void Notification_Requested(object? sender, TrayNotificationModel notification)
    {
        _notifyIcon.ShowBalloonTip(5000, notification.Title, notification.Text, notification.Icon);
    }

    private void ShowMainForm()
    {
        _mainForm.Show();
        _mainForm.WindowState = FormWindowState.Normal;
        _mainForm.Activate();
    }

    private void ShowHotkeyProblems(List<string> problems)
    {
        if (problems.Count == 0)
        {
            return;
        }

        // A balloon shows about 255 characters; the first problems are enough to find the file
        List<string> lines = [.. problems.Take(3)];

        if (problems.Count > 3)
        {
            lines.Add($"...and {problems.Count - 3} more.");
        }

        lines.Add("Tray menu: Hotkey problems, Edit hotkeys.");

        // The notification disappears after a few seconds (a toast on Windows 11), so the menu keeps the full list
        var showProblems = new ToolStripMenuItem(
            $"Hotkey &problems ({problems.Count})...",
            null,
            (_, _) => MessageBox.Show(
                string.Join(Environment.NewLine + Environment.NewLine, problems),
                "My Tools: some hotkeys are off",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning));
        int editHotkeysIndex = _notifyIcon.ContextMenuStrip!.Items.IndexOfKey(EditHotkeysKey);
        _notifyIcon.ContextMenuStrip.Items.Insert(editHotkeysIndex, showProblems);

        _notifyIcon.ShowBalloonTip(
            10000,
            "My Tools: some hotkeys are off",
            string.Join(Environment.NewLine, lines),
            ToolTipIcon.Warning);
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
            _notifications.Requested -= Notification_Requested;
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
