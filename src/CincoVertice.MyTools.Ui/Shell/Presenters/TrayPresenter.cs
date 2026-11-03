using System.Security;
using CincoVertice.MyTools.Ui.Shell.Interfaces;
using CincoVertice.MyTools.Ui.Shell.Views;

namespace CincoVertice.MyTools.Ui.Shell.Presenters;

/// <summary>
///     Keeps the tray in step with the hotkeys: registers them at start and on every reload (the menu's Reload
///     hotkeys, or a change to hotkeys.cfg or to the scripts), fills the Scripts menu, and reports hotkeys that are
///     off, in a notification and in the menu, since the notification soon disappears.
/// </summary>
public class TrayPresenter
{
    public const string AppTitle = "My Tools";

    public const string HotkeyProblemsTitle = "My Tools: some hotkeys are off";

    // A notification shows about 255 characters; the first problems are enough to find the file
    private const int MaxProblemsInNotification = 3;

    private readonly ITrayView _view;
    private readonly IHotkeyRegistrar _hotkeys;
    private readonly IStartWithWindows _startWithWindows;

    public TrayPresenter(ITrayView view, IHotkeyRegistrar hotkeys, IStartWithWindows startWithWindows)
    {
        _view = view;
        _hotkeys = hotkeys;
        _startWithWindows = startWithWindows;

        _view.ReloadHotkeysRequested += OnReloadHotkeysRequested;
        _view.MenuOpening += OnMenuOpening;
        _view.StartWithWindowsRequested += OnStartWithWindowsRequested;
        _hotkeys.FilesChanged += OnHotkeyFilesChanged;
    }

    /// <summary>
    ///     Registers the hotkeys, quietly unless some are off, then reloads them whenever their files change. Call
    ///     once, on the UI thread.
    /// </summary>
    public void Start()
    {
        Reload(announceSuccess: false);
        _hotkeys.WatchFiles();
    }

    /// <summary>
    ///     Registers the hotkeys again and says how it went: "Hotkeys reloaded", or the hotkeys that are off.
    /// </summary>
    public void Reload()
    {
        Reload(announceSuccess: true);
    }

    /// <summary>
    ///     The notification text for hotkey problems: the first few, how many more there are, and where to see all.
    /// </summary>
    public static string SummarizeProblems(IReadOnlyList<string> problems)
    {
        List<string> lines = [.. problems.Take(MaxProblemsInNotification)];

        if (problems.Count > MaxProblemsInNotification)
        {
            lines.Add($"...and {problems.Count - MaxProblemsInNotification} more.");
        }

        lines.Add("Tray menu: Hotkey problems, Edit hotkeys.");

        return string.Join(Environment.NewLine, lines);
    }

    private void Reload(bool announceSuccess)
    {
        var registration = _hotkeys.Register();

        _view.ShowScripts(registration.Scripts);
        _view.ShowHotkeyProblems(registration.Problems);

        if (registration.Problems.Count > 0)
        {
            _view.ShowNotification(HotkeyProblemsTitle, SummarizeProblems(registration.Problems), isWarning: true);
        }
        else if (announceSuccess)
        {
            _view.ShowNotification(AppTitle, "Hotkeys reloaded.", isWarning: false);
        }
    }

    private void OnReloadHotkeysRequested(object? sender, EventArgs e)
    {
        Reload();
    }

    private void OnHotkeyFilesChanged(object? sender, EventArgs e)
    {
        Reload();
    }

    // Task Manager and Settings can turn it off too
    private void OnMenuOpening(object? sender, EventArgs e)
    {
        _view.ShowStartWithWindows(_startWithWindows.IsEnabled);
    }

    private void OnStartWithWindowsRequested(object? sender, bool enabled)
    {
        try
        {
            _startWithWindows.SetEnabled(enabled);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            _view.ShowNotification(AppTitle, $"Could not change Start with Windows: {ex.Message}", isWarning: true);
        }

        _view.ShowStartWithWindows(_startWithWindows.IsEnabled);
    }
}
