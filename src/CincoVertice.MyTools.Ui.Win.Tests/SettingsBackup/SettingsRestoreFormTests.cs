using System.Drawing;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.SettingsBackup.Models;
using CincoVertice.MyTools.Ui.Win.SettingsBackup;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.SettingsBackup;

public class SettingsRestoreFormTests
{
    private static readonly SettingsRestorePlanModel _plan = new(
        @"C:\Backups\MyTools-settings.zip",
        [
            new SettingsFileModel("activity.cfg", SettingsChangeKind.Same, [1], "a", "a"),
            new SettingsFileModel("global.vars", SettingsChangeKind.NotInBackup, null, null, null),
            new SettingsFileModel("hotkeys.cfg", SettingsChangeKind.Changed, [1], "old", "new"),
            new SettingsFileModel(@"images\icon.png", SettingsChangeKind.Changed, [1], null, null),
            new SettingsFileModel(@"scripts\Added.mts", SettingsChangeKind.Added, [1], string.Empty, "added"),
        ]);

    [Fact]
    public void Rows_AreWhatTheRestoreWrites_ThenWhatItKeeps_NotTheSameFiles()
    {
        // Arrange
        using var form = new SettingsRestoreForm(_plan, (_, _) => { });

        // Act / Assert
        Assert.Equal(
            [
                "hotkeys.cfg: Replaced",
                @"images\icon.png: Replaced",
                @"scripts\Added.mts: Added",
                "global.vars: Kept: not in the backup",
            ],
            form.Rows);
        Assert.False(form.DeleteFilesNotInBackup);
    }

    [Fact]
    public void TickingDelete_ShowsTheFilesNotInTheBackupAsDeleted()
    {
        // Arrange
        using var form = new SettingsRestoreForm(_plan, (_, _) => { });

        // Act
        form.TickDelete();

        // Assert
        Assert.Equal("global.vars: Deleted", form.Rows[^1]);
        Assert.True(form.DeleteFilesNotInBackup);
    }

    [Fact]
    public void Compare_IsOnlyForTextFiles_AndGivesTheFileSelected_OverTheForm()
    {
        // Arrange
        (IWin32Window Owner, SettingsFileModel File)? compared = null;
        using var form = new SettingsRestoreForm(_plan, (owner, file) => compared = (owner, file));
        form.Location = new Point(-3000, -3000);
        form.Show();

        // Act / Assert: an image can't be compared, nor a file only in the settings
        form.Select(1);
        Assert.False(form.CanCompare);
        form.Select(3);
        Assert.False(form.CanCompare);

        form.Select(0);
        Assert.True(form.CanCompare);
        form.Compare();

        Assert.Same(form, compared?.Owner);
        Assert.Equal("hotkeys.cfg", compared?.File.Path);
    }
}
