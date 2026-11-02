using System.ComponentModel;
using System.Diagnostics;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Opens a config file for the user to edit.
/// </summary>
public static class ConfigFileOpener
{
    /// <summary>Opens the file in its associated editor, or in Notepad when .cfg files have none.</summary>
    public static void Open(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Win32Exception)
        {
            // No program is associated with .cfg files
            Process.Start("notepad.exe", $"\"{filePath}\"");
        }
    }
}
