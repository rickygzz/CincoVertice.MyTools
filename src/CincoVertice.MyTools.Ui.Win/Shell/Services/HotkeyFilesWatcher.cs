using CincoVertice.MyTools.Ui.Win.Scripting.Services;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Watches hotkeys.cfg and the scripts folder, and raises <see cref="Changed"/> on the UI thread once they have
///     been quiet for a moment, since one save in an editor often writes the file several times.
/// </summary>
public sealed class HotkeyFilesWatcher : IDisposable
{
    private const int QuietMilliseconds = 500;

    private readonly string _hotkeysFilePath;
    private readonly string _scriptsFolderPath;
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly System.Windows.Forms.Timer _quietTimer = new() { Interval = QuietMilliseconds };

    private SynchronizationContext? _uiContext;

    public HotkeyFilesWatcher(string hotkeysFilePath, string scriptsFolderPath)
    {
        _hotkeysFilePath = hotkeysFilePath;
        _scriptsFolderPath = scriptsFolderPath;
    }

    /// <summary>Raised on the UI thread after hotkeys.cfg changed, or a script was added, removed or renamed.</summary>
    public event EventHandler? Changed;

    /// <summary>
    ///     Starts watching. Must be called on the UI thread. A folder that can't be watched is skipped: the tray
    ///     menu's Reload hotkeys still works.
    /// </summary>
    public void Start()
    {
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("Start the watcher on the UI thread.");
        _quietTimer.Tick += QuietTimer_Tick;

        Watch(
            Path.GetDirectoryName(_hotkeysFilePath)!,
            Path.GetFileName(_hotkeysFilePath),
            NotifyFilters.FileName | NotifyFilters.LastWrite);

        // Editing a script needs no reload, since it is read on every run; adding, removing or renaming one does
        Watch(_scriptsFolderPath, "*" + ScriptCatalog.Extension, NotifyFilters.FileName);
    }

    private void Watch(string folderPath, string filter, NotifyFilters notifyFilter)
    {
        try
        {
            Directory.CreateDirectory(folderPath);

            var watcher = new FileSystemWatcher(folderPath, filter) { NotifyFilter = notifyFilter };
            watcher.Changed += File_Changed;
            watcher.Created += File_Changed;
            watcher.Deleted += File_Changed;
            watcher.Renamed += File_Changed;
            watcher.EnableRaisingEvents = true;

            _watchers.Add(watcher);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Not watched; Reload hotkeys in the tray menu still works
        }
    }

    // Raised on a thread-pool thread: restart the quiet period on the UI thread
    private void File_Changed(object sender, FileSystemEventArgs e)
    {
        _uiContext?.Post(
            _ =>
            {
                _quietTimer.Stop();
                _quietTimer.Start();
            },
            null);
    }

    private void QuietTimer_Tick(object? sender, EventArgs e)
    {
        _quietTimer.Stop();

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
        {
            watcher.Dispose();
        }

        _quietTimer.Dispose();
    }
}
