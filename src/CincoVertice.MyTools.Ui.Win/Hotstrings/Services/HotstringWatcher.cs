using System.ComponentModel;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Hotstrings.Models;
using CincoVertice.MyTools.Ui.Hotstrings.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Hotstrings.Services;

/// <summary>
///     Replaces the hotstrings of hotstrings.cfg as they're typed, in any program: the keys typed go to
///     <see cref="HotstringTracker"/>, and at the ending key of one, that key is kept from the program, the name is
///     erased with Backspace, and the text is typed with the ending key after it. The keyboard is watched only while the
///     file has hotstrings; it's read again when saved. Start it on the UI thread: the keys come through its messages.
/// </summary>
public sealed class HotstringWatcher : IDisposable
{
    public const string NotificationTitle = "My Tools hotstrings";

    private readonly HotstringsConfigService _config;
    private readonly TrayNotificationService _notifications;
    private readonly HotstringTracker _tracker = new();

    private InputHook? _input;
    private FileSystemWatcher? _fileWatcher;
    private SynchronizationContext? _uiThread;

    public HotstringWatcher(HotstringsConfigService config, TrayNotificationService notifications)
    {
        _config = config;
        _notifications = notifications;
    }

    public void Start()
    {
        _uiThread = SynchronizationContext.Current;
        Reload();

        string folder = Path.GetDirectoryName(_config.FilePath)!;

        try
        {
            _fileWatcher = new FileSystemWatcher(folder, Path.GetFileName(_config.FilePath))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            };
            _fileWatcher.Changed += OnFileChanged;
            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Renamed += OnFileChanged;
            _fileWatcher.EnableRaisingEvents = true;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException)
        {
            // Without it, changes apply at the next start
        }
    }

    public void Dispose()
    {
        _fileWatcher?.Dispose();
        _fileWatcher = null;
        StopWatchingKeys();
    }

    /// <summary>The ending key's text after the hotstring's: what was kept from the program, typed after it.</summary>
    private static string EndingOf(Keys key) => key switch
    {
        Keys.Return => "\n",
        Keys.Tab => "\t",
        _ => " ",
    };

    // Ctrl, Alt or Win held: the key is a shortcut, not typing. Shift is typing, except on a digit's key
    private static bool IsDown(KeyCode key) => User32.GetAsyncKeyState(key) < 0;

    private void Reload()
    {
        HotstringsResultModel result = _config.Load();

        _tracker.Use(result.Hotstrings);

        if (_tracker.IsEmpty)
        {
            StopWatchingKeys();
        }
        else
        {
            StartWatchingKeys();
        }

        if (result.Problems.Count > 0)
        {
            string lines = result.Problems.Count == 1 ? "A line was" : $"{result.Problems.Count} lines were";

            _notifications.Show(
                NotificationTitle,
                $"{lines} skipped in hotstrings.cfg. Open it in the config editor to see why.",
                ToolTipIcon.Warning);
        }
    }

    private void StartWatchingKeys()
    {
        if (_input is not null)
        {
            return;
        }

        try
        {
            _input = new InputHook();
            _input.KeyPressed += OnKeyPressed;
            _input.MouseDown += OnMouseDown;
        }
        catch (Win32Exception ex)
        {
            _notifications.Show(NotificationTitle, $"Hotstrings are off: {ex.Message}", ToolTipIcon.Warning);
        }
    }

    private void StopWatchingKeys()
    {
        _input?.Dispose();
        _input = null;
    }

    // Saving often writes the file twice; reading it twice changes nothing
    private void OnFileChanged(object sender, FileSystemEventArgs e) => _uiThread?.Post(_ => Reload(), null);

    // The pointer may have put the caret somewhere else
    private void OnMouseDown(object? sender, (Point Point, MouseButtons Button) e) => _tracker.Reset();

    /// <summary>In the hook: quick, as Windows waits for it before the key goes on.</summary>
    private void OnKeyPressed(object? sender, HookKeyEventArgs e)
    {
        // What this app or another types itself, as a hotstring's text
        if (e.IsInjected || Follow(_tracker, e.Key, IsDown) is not HotstringModel hotstring)
        {
            return;
        }

        e.Handled = true;
        string ending = EndingOf(e.Key);

        // Out of the hook: what's sent from inside it would come before the key it waits for
        _uiThread?.Post(_ => Replace(hotstring, ending), null);
    }

    /// <summary>A key typed, for the tracker: a letter or digit, Backspace, an ending key, or a new word.</summary>
    /// <param name="isDown">Whether a key is held, as Ctrl, which makes the key a shortcut.</param>
    /// <returns>The hotstring this ending key ends; null for anything else.</returns>
    internal static HotstringModel? Follow(HotstringTracker tracker, Keys key, Func<KeyCode, bool> isDown)
    {
        if (key is Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey or Keys.CapsLock)
        {
            return null;
        }

        if (isDown(KeyCode.VK_CONTROL) || isDown(KeyCode.VK_MENU) || isDown(KeyCode.VK_LWIN)
            || isDown(KeyCode.VK_RWIN))
        {
            tracker.Reset();

            return null;
        }

        switch (key)
        {
            case >= Keys.A and <= Keys.Z:
                tracker.Type((char)key);
                break;

            // Shift with a digit's key is a symbol, as ! or @
            case >= Keys.D0 and <= Keys.D9 when !isDown(KeyCode.VK_SHIFT):
                tracker.Type((char)('0' + (key - Keys.D0)));
                break;

            case >= Keys.NumPad0 and <= Keys.NumPad9:
                tracker.Type((char)('0' + (key - Keys.NumPad0)));
                break;

            case Keys.Back:
                tracker.Erase();
                break;

            case Keys.Space or Keys.Return or Keys.Tab:
                return tracker.End();

            default:
                tracker.Reset();
                break;
        }

        return null;
    }

    internal static void Replace(HotstringModel hotstring, string ending)
    {
        try
        {
            InputSimulator.SendShortcut(0, KeyCode.VK_BACK, hotstring.Abbreviation.Length);
            InputSimulator.SendUnicodeText(hotstring.Text + ending);
        }
        catch (InvalidOperationException)
        {
            // Windows didn't take the keys, as with a program running as administrator in front: nothing to undo
        }
    }
}
