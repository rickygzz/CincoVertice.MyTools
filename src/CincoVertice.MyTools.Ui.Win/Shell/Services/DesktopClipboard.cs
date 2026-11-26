using System.Diagnostics;
using System.Runtime.InteropServices;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Hotkey;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Copying from and pasting into the window in front through the clipboard, as Ctrl+C and Ctrl+V do, and leaving
///     the clipboard as it was: for scripts and the text transforms. On the UI thread, which the clipboard needs.
/// </summary>
public static class DesktopClipboard
{
    // How long the hotkey's keys may still be held: they would add to Ctrl+C or Ctrl+V
    private const int ReleaseTimeoutMilliseconds = 3000;

    // How long a program has to read the clipboard after Ctrl+V, before the old content goes back
    private const int PasteSettleMilliseconds = 500;

    // How long a program has to put the selection in the clipboard after Ctrl+C
    private const int CopyTimeoutMilliseconds = 1000;

    private static readonly KeyCode[] _modifierKeys =
        [KeyCode.VK_CONTROL, KeyCode.VK_MENU, KeyCode.VK_SHIFT, KeyCode.VK_LWIN, KeyCode.VK_RWIN];

    // One at a time: another paste or copy in between would see the wrong text and leave it on the clipboard
    private static readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    ///     Pastes the text in the window in front, with Windows' line endings, which every program takes; the clipboard
    ///     is as it was after.
    /// </summary>
    public static async Task PasteAsync(string text)
    {
        await _gate.WaitAsync();

        try
        {
            // Copied now: the clipboard's own data object stops working once the clipboard changes
            DataObject? saved = CopyClipboard();

            SetText(text.ReplaceLineEndings("\r\n"));
            await WaitForModifierKeysReleasedAsync();
            InputSimulator.SendShortcut(FSModifiers.MOD_CONTROL, KeyCode.V);

            // The program reads the clipboard when it handles Ctrl+V, a moment after the keys were sent
            await Task.Delay(PasteSettleMilliseconds);
            Restore(saved);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <returns>
    ///     The text selected in the window in front, as Ctrl+C copies it; null when it copies no text, as with nothing
    ///     selected. The clipboard is as it was after.
    /// </returns>
    public static async Task<string?> CopySelectionAsync()
    {
        await _gate.WaitAsync();

        try
        {
            DataObject? saved = CopyClipboard();

            // Emptied first, so the text there after Ctrl+C is the selection, not what was there before
            Clipboard.Clear();
            await WaitForModifierKeysReleasedAsync();
            InputSimulator.SendShortcut(FSModifiers.MOD_CONTROL, KeyCode.C);

            string? text = null;
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < CopyTimeoutMilliseconds)
            {
                await Task.Delay(20);

                if (Clipboard.ContainsText())
                {
                    text = Clipboard.GetText();

                    break;
                }
            }

            Restore(saved);

            return text;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Puts the text in the clipboard; an empty one empties it, as SetText refuses it.</summary>
    public static void SetText(string text)
    {
        if (text.Length == 0)
        {
            Clipboard.Clear();
        }
        else
        {
            Clipboard.SetText(text);
        }
    }

    /// <summary>Waits until Ctrl, Alt, Shift and Win are up, as far as 3 seconds: a hotkey's keys are often still down.</summary>
    public static async Task WaitForModifierKeysReleasedAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        while (_modifierKeys.Any(key => User32.GetAsyncKeyState(key) < 0)
            && stopwatch.ElapsedMilliseconds < ReleaseTimeoutMilliseconds)
        {
            await Task.Delay(20);
        }
    }

    private static void Restore(DataObject? saved)
    {
        if (saved is null)
        {
            Clipboard.Clear();
        }
        else
        {
            Clipboard.SetDataObject(saved, copy: true);
        }
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
}
