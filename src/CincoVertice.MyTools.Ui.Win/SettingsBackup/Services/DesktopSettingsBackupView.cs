using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.SettingsBackup.Models;
using CincoVertice.MyTools.Ui.SettingsBackup.Views;
using CincoVertice.MyTools.Ui.Shell.Presenters;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.Diff.Actions;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.SettingsBackup.Services;

/// <summary>
///     Backing up and restoring from the tray or a hotkey, with no window of its own before: each dialog is owned by
///     an invisible window on the pointer's screen (<see cref="CursorScreenMessageBox"/>), so it's there, in front.
/// </summary>
public sealed class DesktopSettingsBackupView : ISettingsBackupView
{
    private const string ZipFilter = "My Tools backup (*.zip)|*.zip";

    private readonly AppIconService _appIcon;
    private readonly HelpWindowService _help;
    private readonly CompareFilesAction _compareFiles;

    public DesktopSettingsBackupView(AppIconService appIcon, HelpWindowService help, CompareFilesAction compareFiles)
    {
        _appIcon = appIcon;
        _help = help;
        _compareFiles = compareFiles;
    }

    public string? AskBackupPath(string fileName)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Back up My Tools settings",
            Filter = ZipFilter,
            DefaultExt = "zip",
            AddExtension = true,
            FileName = fileName,
            OverwritePrompt = true,
        };

        return WithOwner(owner => dialog.ShowDialog(owner)) == DialogResult.OK ? dialog.FileName : null;
    }

    public string? AskRestorePath()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Restore My Tools settings from a backup",
            Filter = $"{ZipFilter}|All files (*.*)|*.*",
            CheckFileExists = true,
        };

        return WithOwner(owner => dialog.ShowDialog(owner)) == DialogResult.OK ? dialog.FileName : null;
    }

    public SettingsRestoreChoiceModel? ReviewRestore(SettingsRestorePlanModel plan)
    {
        using var form = new SettingsRestoreForm(plan, Compare) { Icon = _appIcon.Current };
        _help.OpenOnF1(form, () => HelpTopics.SettingsBackup);
        form.CenterOnCursorScreen();
        form.ActivateWhenShown();

        return form.ShowDialog() == DialogResult.OK
            ? new SettingsRestoreChoiceModel(form.DeleteFilesNotInBackup)
            : null;
    }

    public void ShowMessage(string message) =>
        CursorScreenMessageBox.Show(message, TrayPresenter.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

    public void ShowProblem(string message) =>
        CursorScreenMessageBox.Show(message, TrayPresenter.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private void Compare(IWin32Window owner, SettingsFileModel file) => _compareFiles.CompareTexts(
        file.Change == SettingsChangeKind.Added ? $"Now: {file.Path} (none)" : $"Now: {file.Path}",
        file.CurrentText!,
        $"Backup: {file.Path}",
        file.BackupText!,
        owner);

    private static DialogResult WithOwner(Func<Form, DialogResult> show)
    {
        using Form owner = CursorScreenMessageBox.CreateOwner();
        owner.Show();
        WindowHelper.ForceForeground(owner.Handle);
        owner.Activate();

        return show(owner);
    }
}
