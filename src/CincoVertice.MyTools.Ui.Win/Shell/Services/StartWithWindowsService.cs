using System.Security;
using CincoVertice.MyTools.Ui.Shell.Interfaces;
using Microsoft.Win32;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Starts the app at sign-in through the current user's Run key, which needs no administrator rights. Windows keeps
///     what Task Manager and Settings turned off in another key, so a value in the Run key alone doesn't mean it starts.
///     A value for another copy of the app, as after moving it, counts as off, and turning it on replaces it. The command
///     passes <see cref="StartupArgument"/>, so the app knows it was started at sign-in.
/// </summary>
public sealed class StartWithWindowsService : IStartWithWindows
{
    /// <summary>Tells the app it was started at sign-in, so it runs config\startup.cfg.</summary>
    public const string StartupArgument = "--startup";

    private const string ValueName = "MyTools";

    private const string CurrentUserRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private const string CurrentUserApprovedKey =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    private readonly string _command;

    // What versions before StartupArgument wrote: it still starts this copy
    private readonly string _olderCommand;
    private readonly string _runKey;
    private readonly string _approvedKey;

    /// <param name="exePath">The program to start.</param>
    /// <param name="runKey">The key under HKEY_CURRENT_USER that starts programs.</param>
    /// <param name="approvedKey">The key under HKEY_CURRENT_USER that says which of them were turned off.</param>
    public StartWithWindowsService(string exePath, string runKey, string approvedKey)
    {
        // In quotes, since the path may have spaces
        _olderCommand = $"\"{exePath}\"";
        _command = $"{_olderCommand} {StartupArgument}";
        _runKey = runKey;
        _approvedKey = approvedKey;
    }

    public bool IsEnabled
    {
        get
        {
            try
            {
                using RegistryKey? run = Registry.CurrentUser.OpenSubKey(_runKey);
                string? command = run?.GetValue(ValueName) as string;

                return IsThisCopy(command) && !IsTurnedOffInWindows();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                return false;
            }
        }
    }

    /// <summary>For this copy of the app, for the current user.</summary>
    public static StartWithWindowsService ForCurrentUser() =>
        new(Environment.ProcessPath!, CurrentUserRunKey, CurrentUserApprovedKey);

    public void SetEnabled(bool enabled)
    {
        using RegistryKey run = Registry.CurrentUser.CreateSubKey(_runKey);

        if (enabled)
        {
            run.SetValue(ValueName, _command);
        }
        else
        {
            run.DeleteValue(ValueName, throwOnMissingValue: false);
        }

        // Asking for it here overrules having turned it off in Windows, and turning it off leaves nothing behind. Last:
        // if the Run key can't be changed, forgetting this would turn back on an older command that's still in it
        ForgetTurnedOffInWindows();
    }

    public void UpdateCommand()
    {
        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(_runKey, writable: true);

        // Only the Run key: whether Windows turned it off is kept by the value's name, which stays
        if (string.Equals(run?.GetValue(ValueName) as string, _olderCommand, StringComparison.OrdinalIgnoreCase))
        {
            run!.SetValue(ValueName, _command);
        }
    }

    private bool IsThisCopy(string? command) =>
        string.Equals(command, _command, StringComparison.OrdinalIgnoreCase)
        || string.Equals(command, _olderCommand, StringComparison.OrdinalIgnoreCase);

    // An odd first byte means turned off: Windows writes 3 for that and 2 for on
    private bool IsTurnedOffInWindows()
    {
        using RegistryKey? approved = Registry.CurrentUser.OpenSubKey(_approvedKey);

        return approved?.GetValue(ValueName) is byte[] { Length: > 0 } state && (state[0] & 1) == 1;
    }

    private void ForgetTurnedOffInWindows()
    {
        using RegistryKey? approved = Registry.CurrentUser.OpenSubKey(_approvedKey, writable: true);

        approved?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
