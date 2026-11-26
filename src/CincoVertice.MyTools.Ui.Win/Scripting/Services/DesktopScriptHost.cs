using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using CincoVertice.Common.Application.Scripting.Interfaces;
using CincoVertice.Common.Application.Scripting.Models;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Hotkey;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Zones.Services;
using CincoVertice.MyTools.Ui.Zones.Services;

namespace CincoVertice.MyTools.Ui.Win.Scripting.Services;

/// <summary>
///     What scripts do on the desktop: dialogs, starting programs, the clipboard, typing. One per script run, so
///     dialogs are titled with the script's name. Runs on the UI thread, which the clipboard needs.
/// </summary>
public sealed class DesktopScriptHost : IScriptHost
{
    private readonly string _title;
    private readonly TrayNotificationService _notifications;
    private readonly ZoneMover _zones;

    public DesktopScriptHost(string scriptName, TrayNotificationService notifications, ZoneMover zones)
    {
        _title = $"My Tools: {scriptName}";
        _notifications = notifications;
        _zones = zones;
    }

    public Task<string?> AskAsync(string prompt, string defaultText) =>
        Task.FromResult(InputDialog.Show(_title, prompt, defaultText));

    public Task<IReadOnlyList<string>?> FormAsync(string title, IReadOnlyList<FormFieldModel> fields) =>
        Task.FromResult(InputDialog.Show(_title, title, fields));

    public Task SayAsync(string text)
    {
        using var owner = CreateTopMostOwner();
        MessageBox.Show(owner, text, _title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string question)
    {
        using var owner = CreateTopMostOwner();
        var answer = MessageBox.Show(owner, question, _title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        return Task.FromResult(answer == DialogResult.Yes);
    }

    public void Open(string target)
    {
        using var process = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    public void Run(string program, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo(program, arguments) { UseShellExecute = true });
    }

    public string GetClipboard() => Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;

    public void SetClipboard(string text) => DesktopClipboard.SetText(text);

    public async Task TypeAsync(string text)
    {
        // The hotkey's Ctrl or Alt is usually still down, which would turn the typed text into shortcuts
        await DesktopClipboard.WaitForModifierKeysReleasedAsync();

        // Each character as itself: SendKeys pressed the key that types it on this app's keyboard layout, and the
        // window may have another, where that key types something else, as AltGr+2 for @ on a Spanish one
        InputSimulator.SendUnicodeText(text);
    }

    // Scripts write lines with \n, pasted with Windows' line endings
    public Task PasteTextAsync(string text) => DesktopClipboard.PasteAsync(text);

    public async Task PressAsync(string keys, int times)
    {
        if (!HotkeyParser.TryParseKeys(
            keys,
            out FSModifiers modifiers,
            out bool altGr,
            out KeyCode key,
            out string error))
        {
            throw new ArgumentException(error);
        }

        // The hotkey's own keys would add to the ones pressed: Ctrl+Alt+V instead of Ctrl+V
        await DesktopClipboard.WaitForModifierKeysReleasedAsync();

        InputSimulator.SendShortcut(modifiers, key, times, altGr);
    }

    public string GetActiveWindowTitle() => WindowHelper.GetForegroundWindowTitle();

    public void MoveActiveWindowToZone(int zone)
    {
        if (_zones.MoveActiveWindowTo(zone) is string problem)
        {
            throw new ArgumentException(problem);
        }
    }

    public void MoveActiveWindowToPointer()
    {
        if (_zones.MoveActiveWindowToPointer() is string problem)
        {
            throw new ArgumentException(problem);
        }
    }

    public Task WaitAsync(int milliseconds) => Task.Delay(milliseconds);

    public async Task<SystemLoadModel> MeasureLoadAsync(int milliseconds)
    {
        using var meter = new SystemLoadMeter();

        meter.Start();
        await Task.Delay(milliseconds);
        var (cpu, disk) = meter.Read();

        return new SystemLoadModel((decimal)cpu, (decimal)disk);
    }

    public void Notify(string text) => _notifications.Show(_title, text);

    public DateTime Now => DateTime.Now;

    public bool ActivateWindow(string titlePart)
    {
        nint window = WindowHelper.FindWindowByTitle(titlePart);

        return window != 0 && WindowHelper.BringToFront(window);
    }

    public string ReadFile(string path) => File.ReadAllText(FullPath(path));

    public void WriteFile(string path, string text) =>
        AtomicFile.WriteAllText(FullPath(path), text, new UTF8Encoding(false));

    public void AppendFile(string path, string text) =>
        File.AppendAllText(FullPath(path), text, new UTF8Encoding(false));

    public bool FileExists(string path) => File.Exists(FullPath(path));

    public bool SetActiveWindowOnTop(bool? isOnTop) => ActiveWindowOnTop.Set(isOnTop).IsOnTop;

    public bool IsProcessRunning(string processName) => ScriptWindows.IsProcessRunning(processName);

    public IReadOnlyList<long> GetProcessWindows(string processName) =>
        [.. ScriptWindows.OfProcess(processName).Select(window => (long)window)];

    public long FindWindow(string titlePart) => ScriptWindows.ByTitle(titlePart);

    public long GetActiveWindow() => WinUser.GetForegroundWindow();

    public ScriptWindowState GetWindowState(long window) => ScriptWindows.StateOf((nint)window);

    public Task<bool> ChangeWindowAsync(long window, ScriptWindowChange change) =>
        ScriptWindows.ChangeAsync((nint)window, change);

    public bool ActivateWindow(long window) => ScriptWindows.Activate((nint)window);

    public string GetWindowTitle(long window) =>
        User32.IsWindow((nint)window) ? WindowHelper.GetWindowTitle((nint)window) : string.Empty;

    public bool SetWindowOnTop(long window, bool? isOnTop) => ScriptWindows.SetOnTop((nint)window, isOnTop);

    /// <summary>A script's file: %VARIABLES% expanded, and a path that isn't full taken in the scripts folder.</summary>
    public static string FullPath(string path)
    {
        string expanded = Environment.ExpandEnvironmentVariables(path.Trim());

        return Path.IsPathFullyQualified(expanded)
            ? expanded
            : Path.GetFullPath(Path.Combine(ScriptCatalog.FolderPath, expanded));
    }

    /// <summary>
    ///     An invisible, top-most owner window in the foreground, so a message box shows above the application the
    ///     user is in and gets the keyboard focus. It must be shown to take the foreground, so it is transparent and
    ///     1 pixel big, in the middle of the screen with the mouse cursor, where the message box then centers.
    /// </summary>
    private static Form CreateTopMostOwner()
    {
        Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;

        var owner = new Form
        {
            TopMost = true,
            ShowInTaskbar = false,
            FormBorderStyle = FormBorderStyle.None,
            Opacity = 0,
            StartPosition = FormStartPosition.Manual,
            Bounds = new Rectangle(area.Left + area.Width / 2, area.Top + area.Height / 2, 1, 1),
        };

        owner.Show();
        WindowHelper.ForceForeground(owner.Handle);

        return owner;
    }
}
