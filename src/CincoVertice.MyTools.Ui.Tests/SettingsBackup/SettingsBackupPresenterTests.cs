using System.IO.Compression;
using System.Text;
using CincoVertice.MyTools.Ui.SettingsBackup.Models;
using CincoVertice.MyTools.Ui.SettingsBackup.Presenters;
using CincoVertice.MyTools.Ui.SettingsBackup.Services;
using CincoVertice.MyTools.Ui.SettingsBackup.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.SettingsBackup;

public sealed class SettingsBackupPresenterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"settings-{Guid.NewGuid():N}");
    private readonly FakeView _view = new();
    private readonly FixedTime _time = new(new DateTimeOffset(2026, 11, 25, 9, 30, 15, TimeSpan.Zero));

    private string Config => Path.Combine(_root, "config");

    private string Backups => Path.Combine(_root, "backups");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void BackUp_OffersANameWithTheDate_AndSaysWhereItSaved()
    {
        // Arrange
        WriteConfig("hotkeys.cfg", "mine");
        string zip = Path.Combine(_root, "chosen.zip");
        _view.BackupPath = zip;

        // Act
        NewPresenter().BackUp();

        // Assert
        Assert.Equal("MyTools-settings-2026-11-25.zip", _view.OfferedName);
        Assert.True(File.Exists(zip));
        Assert.Equal($"Settings backed up to {zip}.", Assert.Single(_view.Messages));
    }

    [Fact]
    public void BackUp_Cancelled_SavesNothing()
    {
        // Act
        NewPresenter().BackUp();

        // Assert
        Assert.Empty(_view.Messages);
        Assert.Empty(_view.Problems);
    }

    [Fact]
    public void Restore_ShowsWhatItWouldDo_ThenSavesTheSettingsFirst_ThenWrites()
    {
        // Arrange
        WriteConfig("hotkeys.cfg", "old");
        _view.RestorePath = Zip(("hotkeys.cfg", "new"));
        _view.Choice = new SettingsRestoreChoiceModel(DeleteFilesNotInBackup: false);

        // Act
        NewPresenter().Restore();

        // Assert: the review saw the plan, the settings were saved as they were, then restored
        SettingsFileModel reviewed = Assert.Single(_view.Reviewed!.Files);
        Assert.Equal(SettingsChangeKind.Changed, reviewed.Change);

        string safetyCopy = Assert.Single(Directory.GetFiles(Backups));
        Assert.Equal("old", ReadEntry(safetyCopy, "hotkeys.cfg"));
        Assert.Equal("new", File.ReadAllText(Path.Combine(Config, "hotkeys.cfg")));

        string message = Assert.Single(_view.Messages);
        Assert.Contains("1 file written", message);
        Assert.Contains(safetyCopy, message);
        Assert.Contains("Exit and reopen", message);
    }

    [Fact]
    public void Restore_Cancelled_InTheReview_ChangesNothing_NorSavesACopy()
    {
        // Arrange
        WriteConfig("hotkeys.cfg", "old");
        _view.RestorePath = Zip(("hotkeys.cfg", "new"));

        // Act
        NewPresenter().Restore();

        // Assert
        Assert.NotNull(_view.Reviewed);
        Assert.Equal("old", File.ReadAllText(Path.Combine(Config, "hotkeys.cfg")));
        Assert.False(Directory.Exists(Backups));
        Assert.Empty(_view.Messages);
    }

    [Fact]
    public void Restore_OfTheSameSettings_SaysThereIsNothingToDo_WithoutAReview()
    {
        // Arrange
        WriteConfig("hotkeys.cfg", "same");
        _view.RestorePath = Zip(("hotkeys.cfg", "same"));

        // Act
        NewPresenter().Restore();

        // Assert
        Assert.Null(_view.Reviewed);
        Assert.Contains("nothing to restore", Assert.Single(_view.Messages));
    }

    [Fact]
    public void Restore_DeletesWhatTheBackupHasnt_OnlyWhenChosen()
    {
        // Arrange
        WriteConfig("hotkeys.cfg", "same");
        WriteConfig("global.vars", "{}");
        _view.RestorePath = Zip(("hotkeys.cfg", "same"));
        _view.Choice = new SettingsRestoreChoiceModel(DeleteFilesNotInBackup: true);

        // Act
        NewPresenter().Restore();

        // Assert
        Assert.False(File.Exists(Path.Combine(Config, "global.vars")));
        Assert.Contains("0 files written, 1 file deleted", Assert.Single(_view.Messages));
    }

    [Fact]
    public void Restore_OfAnUnsafeBackup_SaysWhy_AndChangesNothing()
    {
        // Arrange
        WriteConfig("hotkeys.cfg", "old");
        _view.RestorePath = Zip(("../outside.cfg", "evil"));

        // Act
        NewPresenter().Restore();

        // Assert
        Assert.Null(_view.Reviewed);
        Assert.Contains("leads outside the settings", Assert.Single(_view.Problems));
        Assert.False(File.Exists(Path.Combine(_root, "outside.cfg")));
    }

    [Fact]
    public void Restore_Cancelled_AtTheFile_DoesNothing()
    {
        // Act
        NewPresenter().Restore();

        // Assert
        Assert.Null(_view.Reviewed);
        Assert.Empty(_view.Messages);
        Assert.Empty(_view.Problems);
    }

    private SettingsBackupPresenter NewPresenter() =>
        new(_view, new SettingsBackupService(Config, Backups, _time), _time);

    private void WriteConfig(string path, string text)
    {
        string full = Path.Combine(Config, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    private string Zip(params (string Name, string Text)[] entries)
    {
        string zip = Path.Combine(_root, $"{Guid.NewGuid():N}.zip");
        Directory.CreateDirectory(_root);

        using ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Create);

        foreach ((string name, string text) in entries)
        {
            using Stream entry = archive.CreateEntry(name).Open();
            entry.Write(Encoding.UTF8.GetBytes(text));
        }

        return zip;
    }

    private static string ReadEntry(string zip, string name)
    {
        using ZipArchive archive = ZipFile.OpenRead(zip);
        using var reader = new StreamReader(archive.GetEntry(name)!.Open());

        return reader.ReadToEnd();
    }

    private sealed class FakeView : ISettingsBackupView
    {
        public string? BackupPath { get; set; }

        public string? RestorePath { get; set; }

        public SettingsRestoreChoiceModel? Choice { get; set; }

        public string? OfferedName { get; private set; }

        public SettingsRestorePlanModel? Reviewed { get; private set; }

        public List<string> Messages { get; } = [];

        public List<string> Problems { get; } = [];

        public string? AskBackupPath(string fileName)
        {
            OfferedName = fileName;

            return BackupPath;
        }

        public string? AskRestorePath() => RestorePath;

        public SettingsRestoreChoiceModel? ReviewRestore(SettingsRestorePlanModel plan)
        {
            Reviewed = plan;

            return Choice;
        }

        public void ShowMessage(string message) => Messages.Add(message);

        public void ShowProblem(string message) => Problems.Add(message);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => now;
    }
}
