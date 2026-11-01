using System.ComponentModel;
using System.Diagnostics;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Shell.Actions;

/// <summary>
///     Opens hotkeys.cfg in its associated editor, or in Notepad when .cfg files have none. Changes apply after a
///     restart, as the file's header says.
/// </summary>
public sealed class EditHotkeysAction : IAppAction
{
    private readonly HotkeyConfigService _hotkeyConfigService;

    public EditHotkeysAction(HotkeyConfigService hotkeyConfigService)
    {
        _hotkeyConfigService = hotkeyConfigService;
    }

    public string Name => "EditHotkeys";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        string filePath = _hotkeyConfigService.FilePath;

        if (!File.Exists(filePath))
        {
            MessageBox.Show(
                $"{filePath} does not exist. It is created when MyTools starts.",
                "Edit hotkeys",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return Task.CompletedTask;
        }

        try
        {
            Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
        }
        catch (Win32Exception)
        {
            // No program is associated with .cfg files
            Process.Start("notepad.exe", $"\"{filePath}\"");
        }

        return Task.CompletedTask;
    }
}
