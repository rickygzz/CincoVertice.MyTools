using System.Diagnostics;
using System.Runtime.InteropServices;
using CincoVertice.Common.Application.Scripting.Interfaces;
using CincoVertice.Common.Application.Scripting.Models;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Hotkey;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Zones.Services;

namespace CincoVertice.MyTools.Ui.Win.Scripting.Services;

/// <summary>
///     What scripts do on the desktop: dialogs, starting programs, the clipboard, typing. One per script run, so
///     dialogs are titled with the script's name. Runs on the UI thread, which the clipboard needs.
/// </summary>
public sealed class DesktopScriptHost : IScriptHost
{
    // How long type() waits for the user to release the hotkey's keys
    private const int ReleaseTimeoutMilliseconds = 3000;

    // How long pasteastext() gives the program to read the clipboard before putting the old content back
    private const int PasteSettleMilliseconds = 500;

    private static readonly KeyCode[] _modifierKeys = [
        KeyCode.VK_CONTROL,
        KeyCode.VK_MENU,
        KeyCode.VK_SHIFT,
        KeyCode.VK_LWIN,
        KeyCode.VK_RWIN ];

    // One paste at a time across all scripts, each of which has its own host: another script's paste in between would
    // send the wrong text and leave temporary text on the clipboard
    private static readonly SemaphoreSlim _pasteGate = new(1, 1);

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

    public void SetClipboard(string text)
    {
        // SetText does not accept an empty text
        if (text.Length == 0)
        {
            Clipboard.Clear();
        }
        else
        {
            Clipboard.SetText(text);
        }
    }

    public async Task TypeAsync(string text)
    {
        // The hotkey's Ctrl or Alt is usually still down, which would turn the typed text into shortcuts
        await WaitForModifierKeysReleasedAsync();

        // Each character as itself: SendKeys pressed the key that types it on this app's keyboard layout, and the
        // window may have another, where that key types something else, as AltGr+2 for @ on a Spanish one
        InputSimulator.SendUnicodeText(text);
    }

    public async Task PasteTextAsync(string text)
    {
        await _pasteGate.WaitAsync();

        try
        {
            // Copied now: the clipboard's own data object stops working once the clipboard changes
            DataObject? saved = CopyClipboard();

            // Windows line endings, which every program accepts; scripts write lines with \n
            SetClipboard(text.ReplaceLineEndings("\r\n"));

            await WaitForModifierKeysReleasedAsync();
            InputSimulator.SendShortcut(FSModifiers.MOD_CONTROL, KeyCode.V);

            // The program reads the clipboard when it handles Ctrl+V, a moment after the keys were sent
            await Task.Delay(PasteSettleMilliseconds);

            if (saved is null)
            {
                Clipboard.Clear();
            }
            else
            {
                Clipboard.SetDataObject(saved, copy: true);
            }
        }
        finally
        {
            _pasteGate.Release();
        }
    }

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
        await WaitForModifierKeysReleasedAsync();

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

    /// <summary>
    ///     A copy of everything in the clipboard, in every format it has (text, formatted text, HTML, pictures...).
    ///     A format that can't be read, e.g. an embedded object, is left out. Null when the clipboard is empty.
    /// </summary>
    private static DataObject? CopyClipboard()
    {
        var current = Clipboard.GetDataObject();

        if (current is null)
        {
            return null;
        }

        var copy = new DataObject();

        foreach (string format in current.GetFormats(autoConvert: false))
        {
            try
            {
                if (current.GetData(format, autoConvert: false) is { } data)
                {
                    copy.SetData(format, autoConvert: false, data);
                }
            }
            catch (Exception ex) when (ex is ExternalException or NotSupportedException or InvalidOperationException)
            {
                // Left out; the other formats are still put back
            }
        }

        return copy.GetFormats().Length == 0 ? null : copy;
    }

    private static async Task WaitForModifierKeysReleasedAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        while (_modifierKeys.Any(key => User32.GetAsyncKeyState(key) < 0)
            && stopwatch.ElapsedMilliseconds < ReleaseTimeoutMilliseconds)
        {
            await Task.Delay(20);
        }
    }
}
