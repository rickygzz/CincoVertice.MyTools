using System.Diagnostics;
using System.Text;
using CincoVertice.Common.Application.Scripting.Interfaces;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Scripting.Services;

/// <summary>
///     What scripts do on the desktop: dialogs, starting programs, the clipboard, typing. One per script run, so
///     dialogs are titled with the script's name. Runs on the UI thread, which the clipboard needs.
/// </summary>
public sealed class DesktopScriptHost : IScriptHost
{
    // How long type() waits for the user to release the hotkey's keys
    private const int ReleaseTimeoutMilliseconds = 3000;

    private static readonly KeyCode[] _modifierKeys =
        [KeyCode.VK_CONTROL, KeyCode.VK_MENU, KeyCode.VK_SHIFT, KeyCode.VK_LWIN, KeyCode.VK_RWIN];

    private readonly string _title;
    private readonly TrayNotificationService _notifications;

    public DesktopScriptHost(string scriptName, TrayNotificationService notifications)
    {
        _title = $"My Tools: {scriptName}";
        _notifications = notifications;
    }

    public Task<string?> AskAsync(string prompt, string defaultText) =>
        Task.FromResult(InputDialog.Show(_title, prompt, defaultText));

    public Task<IReadOnlyList<string>?> FormAsync(
        string title, IReadOnlyList<string> prompts, IReadOnlyList<string> defaultTexts) =>
        Task.FromResult(InputDialog.Show(_title, title, prompts, defaultTexts));

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

        SendKeys.SendWait(EscapeForSendKeys(text));
    }

    public Task WaitAsync(int milliseconds) => Task.Delay(milliseconds);

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

    private static async Task WaitForModifierKeysReleasedAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        while (_modifierKeys.Any(key => User32.GetAsyncKeyState(key) < 0)
            && stopwatch.ElapsedMilliseconds < ReleaseTimeoutMilliseconds)
        {
            await Task.Delay(20);
        }
    }

    /// <summary>
    ///     SendKeys gives + ^ % ~ ( ) { } [ ] a meaning, so they are wrapped in braces to be typed as they are.
    ///     New lines and tabs become Enter and Tab.
    /// </summary>
    private static string EscapeForSendKeys(string text)
    {
        var sb = new StringBuilder(text.Length);

        foreach (char c in text)
        {
            switch (c)
            {
                case '+' or '^' or '%' or '~' or '(' or ')' or '{' or '}' or '[' or ']':
                    sb.Append('{').Append(c).Append('}');
                    break;

                case '\n':
                    sb.Append("{ENTER}");
                    break;

                case '\t':
                    sb.Append("{TAB}");
                    break;

                case '\r':
                    break;

                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }
}
