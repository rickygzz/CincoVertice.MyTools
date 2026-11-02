using CincoVertice.MyTools.Ui.Shell.Models;
using CincoVertice.MyTools.Ui.Shell.Presenters;
using CincoVertice.MyTools.Ui.Shell.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.CodeReview.Actions;
using CincoVertice.MyTools.Ui.Win.Scripting.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Shell;

/// <summary>
///     Root of the application. Lives in the systray and shows the main form on demand. As the tray view, it shows
///     what <see cref="TrayPresenter"/> gives it: the scripts, the hotkey problems and notifications.
/// </summary>
public sealed class TrayApplicationContext : ApplicationContext, ITrayView
{
    private const int InfoNotificationMilliseconds = 5000;
    private const int WarningNotificationMilliseconds = 10000;

    private readonly MainForm _mainForm;
    private readonly AppIconService _appIconService;
    private readonly TrayNotificationService _notifications;
    private readonly OpenScriptsFolderAction _openScriptsFolderAction;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _scriptsMenu = new("S&cripts");

    // The Hotkey problems item goes above it
    private readonly ToolStripMenuItem _editHotkeysItem;

    private ToolStripMenuItem? _hotkeyProblemsItem;

    public TrayApplicationContext(
        MainForm mainForm,
        AppIconService appIconService,
        TrayNotificationService notifications,
        CheckFolderAction checkFolderAction,
        FixFolderAction fixFolderAction,
        EditHotkeysAction editHotkeysAction,
        EditActivitySettingsAction editActivitySettingsAction,
        OpenScriptsFolderAction openScriptsFolderAction)
    {
        _mainForm = mainForm;
        _appIconService = appIconService;
        _notifications = notifications;
        _openScriptsFolderAction = openScriptsFolderAction;

        _appIconService.Changed += AppIcon_Changed;
        _notifications.Requested += Notification_Requested;

        // Minimize and X hide to the systray
        _mainForm.HideOnMinimize();
        _mainForm.HideOnUserClose();

        var menu = new ContextMenuStrip();
        var open = AddItem(menu.Items, "&Open", ShowMainForm);
        open.Font = new Font(open.Font, FontStyle.Bold);
        AddAction(menu.Items, "Check folder for &standard...", checkFolderAction);
        AddAction(menu.Items, "&Fix folder for standard...", fixFolderAction);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_scriptsMenu);
        _editHotkeysItem = AddAction(menu.Items, "Edit &hotkeys...", editHotkeysAction);

        // For when the file watcher misses a change, e.g. on a network drive
        AddItem(menu.Items, "&Reload hotkeys", () => ReloadHotkeysRequested?.Invoke(this, EventArgs.Empty));
        AddAction(menu.Items, "Edit &activity settings...", editActivitySettingsAction);
        menu.Items.Add(new ToolStripSeparator());
        AddItem(menu.Items, "&Exit", Exit);

        _notifyIcon = new NotifyIcon
        {
            Icon = _appIconService.Current,
            Text = TrayPresenter.AppTitle,
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ShowMainForm();

        ShowScripts([]);
    }

    public event EventHandler? ReloadHotkeysRequested;

    /// <summary>
    ///     Scripts: one item per script, then the folder to add or edit them.
    /// </summary>
    public void ShowScripts(IReadOnlyList<ScriptMenuItemModel> scripts)
    {
        ClearAndDispose(_scriptsMenu.DropDownItems);

        foreach (var script in scripts)
        {
            _scriptsMenu.DropDownItems.Add(new ToolStripMenuItem(
                script.Name,
                null,
                async (_, _) => await script.RunAsync()));
        }

        if (scripts.Count > 0)
        {
            _scriptsMenu.DropDownItems.Add(new ToolStripSeparator());
        }

        AddAction(_scriptsMenu.DropDownItems, "Open scripts &folder", _openScriptsFolderAction);
    }

    public void ShowHotkeyProblems(IReadOnlyList<string> problems)
    {
        if (_hotkeyProblemsItem is not null)
        {
            _notifyIcon.ContextMenuStrip!.Items.Remove(_hotkeyProblemsItem);
            _hotkeyProblemsItem.Dispose();
            _hotkeyProblemsItem = null;
        }

        if (problems.Count == 0)
        {
            return;
        }

        var items = _notifyIcon.ContextMenuStrip!.Items;
        _hotkeyProblemsItem = new ToolStripMenuItem(
            $"Hotkey &problems ({problems.Count})...",
            null,
            (_, _) => MessageBox.Show(
                string.Join(Environment.NewLine + Environment.NewLine, problems),
                TrayPresenter.HotkeyProblemsTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning));
        items.Insert(items.IndexOf(_editHotkeysItem), _hotkeyProblemsItem);
    }

    public void ShowNotification(string title, string text, bool isWarning)
    {
        ShowBalloon(title, text, isWarning ? ToolTipIcon.Warning : ToolTipIcon.Info);
    }

    private static ToolStripMenuItem AddItem(ToolStripItemCollection items, string text, Action onClick)
    {
        var item = new ToolStripMenuItem(text, null, (_, _) => onClick());
        items.Add(item);

        return item;
    }

    private static ToolStripMenuItem AddAction(ToolStripItemCollection items, string text, IAppAction action)
    {
        var item = new ToolStripMenuItem(text, null, async (_, _) => await action.ExecuteAsync());
        items.Add(item);

        return item;
    }

    // Clear() doesn't dispose the items
    private static void ClearAndDispose(ToolStripItemCollection items)
    {
        List<ToolStripItem> oldItems = [.. items.Cast<ToolStripItem>()];
        items.Clear();

        foreach (var item in oldItems)
        {
            item.Dispose();
        }
    }

    /// <summary>
    ///     Every notification, from the presenter or from scripts: a warning stays longer.
    /// </summary>
    private void ShowBalloon(string title, string text, ToolTipIcon icon)
    {
        int milliseconds = icon == ToolTipIcon.Warning ? WarningNotificationMilliseconds : InfoNotificationMilliseconds;

        _notifyIcon.ShowBalloonTip(milliseconds, title, text, icon);
    }

    private void Notification_Requested(object? sender, TrayNotificationModel notification)
    {
        ShowBalloon(notification.Title, notification.Text, notification.Icon);
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
