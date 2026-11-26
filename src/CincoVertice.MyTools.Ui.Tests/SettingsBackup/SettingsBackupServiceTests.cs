using System.IO.Compression;
using System.Text;
using CincoVertice.MyTools.Ui.SettingsBackup.Models;
using CincoVertice.MyTools.Ui.SettingsBackup.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.SettingsBackup;

public sealed class SettingsBackupServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"settings-{Guid.NewGuid():N}");
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
    public void Export_ThenPlan_OfTheSameSettings_ChangesNothing()
    {
        // Arrange
        WriteConfig("hotkeys.cfg", "ShowHelp = Ctrl+Alt+H");
        WriteConfig(Path.Combine("scripts", "Hello.mts"), "print(\"hi\")");
        SettingsBackupService service = NewService();
        string zip = Path.Combine(_root, "backup.zip");

        // Act
        service.Export(zip);
        SettingsRestorePlanModel plan = service.Plan(zip);

        // Assert: entry names use /, whatever the system's separator
        Assert.Equal(["hotkeys.cfg", "scripts/Hello.mts"], EntriesOf(zip));
        Assert.All(plan.Files, file => Assert.Equal(SettingsChangeKind.Same, file.Change));
        Assert.Empty(plan.ToWrite);
        Assert.Empty(plan.NotInBackup);
    }

    [Fact]
    public void Export_IntoTheConfigFolder_LeavesTheZipItselfOut()
    {
        // Arrange
        WriteConfig("hotkeys.cfg", "x");
        string zip = Path.Combine(Config, "backup.zip");

        // Act
        NewService().Export(zip);

        // Assert
        Assert.Equal(["hotkeys.cfg"], EntriesOf(zip));
    }

    [Fact]
    public void Plan_TellsAddedChangedSameAndNotInBackup_WithTheTextsToCompare()
    {
        // Arrange
        string zip = Zip(("hotkeys.cfg", "new"), ("activity.cfg", "same"), ("scripts/Added.mts", "added"));
        WriteConfig("hotkeys.cfg", "old");
        WriteConfig("activity.cfg", "same");
        WriteConfig("global.vars", "{}");

        // Act
        SettingsRestorePlanModel plan = NewService().Plan(zip);

        // Assert
        Assert.Equal(
            [
                ("activity.cfg", SettingsChangeKind.Same),
                ("global.vars", SettingsChangeKind.NotInBackup),
                ("hotkeys.cfg", SettingsChangeKind.Changed),
                (Path.Combine("scripts", "Added.mts"), SettingsChangeKind.Added),
            ],
            plan.Files.Select(file => (file.Path, file.Change)));

        SettingsFileModel changed = plan.Files.Single(file => file.Path == "hotkeys.cfg");
        Assert.Equal(("old", "new"), (changed.CurrentText, changed.BackupText));
        Assert.True(changed.CanCompare);

        SettingsFileModel added = plan.Files.Single(file => file.Change == SettingsChangeKind.Added);
        Assert.Equal((string.Empty, "added"), (added.CurrentText, added.BackupText));
        Assert.True(added.CanCompare);

        Assert.False(plan.Files.Single(file => file.Path == "global.vars").CanCompare);
    }

    [Fact]
    public void Plan_OfAFileThatIsntText_HasNothingToCompare()
    {
        // Arrange: an image, with zeros in it
        string zip = Zip(("images/icon.png", "\u0089PNG\0\0\0\rIHDR"));
        WriteConfig(Path.Combine("images", "icon.png"), "\u0089PNG\0\0\0\rIHDX");

        // Act
        SettingsFileModel file = Assert.Single(NewService().Plan(zip).Files);

        // Assert
        Assert.Equal(SettingsChangeKind.Changed, file.Change);
        Assert.False(file.CanCompare);
    }

    [Fact]
    public void Plan_ChangesNothing()
    {
        // Arrange
        string zip = Zip(("hotkeys.cfg", "new"));
        WriteConfig("hotkeys.cfg", "old");

        // Act
        NewService().Plan(zip);

        // Assert
        Assert.Equal("old", ReadConfig("hotkeys.cfg"));
    }

    [Theory]
    [InlineData("../outside.cfg")]
    [InlineData("scripts/../../outside.cfg")]
    [InlineData("/rooted.cfg")]
    [InlineData("C:/Windows/evil.cfg")]
    [InlineData("hotkeys.cfg:stream")]
    [InlineData("scripts//double.mts")]
    [InlineData("./hotkeys.cfg")]
    public void Plan_OfAPathOutsideTheSettings_RefusesTheBackup(string entryName)
    {
        // Arrange
        string zip = Zip(("hotkeys.cfg", "fine"), (entryName, "evil"));

        // Act / Assert
        InvalidDataException problem = Assert.Throws<InvalidDataException>(() => NewService().Plan(zip));
        Assert.Contains(entryName, problem.Message);
    }

    [Fact]
    public void Plan_OfAPathTwice_RefusesTheBackup()
    {
        // Arrange: the same file, by another case
        string zip = Zip(("hotkeys.cfg", "a"), ("HOTKEYS.cfg", "b"));

        // Act / Assert
        Assert.Throws<InvalidDataException>(() => NewService().Plan(zip));
    }

    [Fact]
    public void Plan_OfAFileThatIsntAZip_SaysSo()
    {
        // Arrange
        string path = Path.Combine(_root, "notes.zip");
        Directory.CreateDirectory(_root);
        File.WriteAllText(path, "not a zip");

        // Act / Assert
        InvalidDataException problem = Assert.Throws<InvalidDataException>(() => NewService().Plan(path));
        Assert.Equal("That isn't a zip file.", problem.Message);
    }

    [Fact]
    public void Plan_OfABackupLargerThanTheLimit_RefusesIt_CountingWhatUnzips()
    {
        // Arrange: zeros compress to almost nothing, so only what unzips tells the size
        string zip = Path.Combine(_root, "large.zip");
        Directory.CreateDirectory(_root);

        using (ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
            using Stream entry = archive.CreateEntry("big.bin", CompressionLevel.Optimal).Open();
            byte[] chunk = new byte[1024 * 1024];

            for (long written = 0; written <= SettingsBackupService.MaxTotalBytes; written += chunk.Length)
            {
                entry.Write(chunk);
            }
        }

        // Act / Assert
        InvalidDataException problem = Assert.Throws<InvalidDataException>(() => NewService().Plan(zip));
        Assert.Contains("256 MB", problem.Message);
    }

    [Fact]
    public void Apply_WritesAddedAndChanged_KeepsWhatTheBackupHasnt_UnlessAsked()
    {
        // Arrange
        string zip = Zip(("hotkeys.cfg", "new"), ("scripts/Added.mts", "added"));
        WriteConfig("hotkeys.cfg", "old");
        WriteConfig("global.vars", "{}");
        SettingsBackupService service = NewService();
        SettingsRestorePlanModel plan = service.Plan(zip);

        // Act
        (int written, int deleted) = service.Apply(plan, deleteFilesNotInBackup: false);

        // Assert
        Assert.Equal((2, 0), (written, deleted));
        Assert.Equal("new", ReadConfig("hotkeys.cfg"));
        Assert.Equal("added", ReadConfig(Path.Combine("scripts", "Added.mts")));
        Assert.Equal("{}", ReadConfig("global.vars"));
        Assert.Empty(Directory.GetFiles(Config, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public void Apply_DeletesWhatTheBackupHasnt_WhenAsked()
    {
        // Arrange
        string zip = Zip(("hotkeys.cfg", "same"));
        WriteConfig("hotkeys.cfg", "same");
        WriteConfig("global.vars", "{}");
        SettingsBackupService service = NewService();

        // Act
        (int written, int deleted) = service.Apply(service.Plan(zip), deleteFilesNotInBackup: true);

        // Assert
        Assert.Equal((0, 1), (written, deleted));
        Assert.False(File.Exists(Path.Combine(Config, "global.vars")));
        Assert.Equal("same", ReadConfig("hotkeys.cfg"));
    }

    [Fact]
    public void SaveSafetyCopy_SavesTheSettings_NamedByTheTime()
    {
        // Arrange
        WriteConfig("hotkeys.cfg", "mine");

        // Act
        string? path = NewService().SaveSafetyCopy();

        // Assert
        Assert.Equal(Path.Combine(Backups, "settings-before-restore-2026-11-25-093015.zip"), path);
        Assert.Equal(["hotkeys.cfg"], EntriesOf(path!));
    }

    [Fact]
    public void SaveSafetyCopy_AtTheSameSecond_KeepsBoth()
    {
        // Arrange
        WriteConfig("hotkeys.cfg", "mine");
        SettingsBackupService service = NewService();

        // Act
        string? first = service.SaveSafetyCopy();
        string? second = service.SaveSafetyCopy();

        // Assert
        Assert.NotEqual(first, second);
        Assert.True(File.Exists(first));
        Assert.True(File.Exists(second));
    }

    [Fact]
    public void SaveSafetyCopy_KeepsOnlyTheNewest()
    {
        // Arrange
        WriteConfig("hotkeys.cfg", "mine");
        SettingsBackupService service = NewService();

        // Act
        for (int i = 0; i < SettingsBackupService.SafetyCopiesKept + 3; i++)
        {
            service.SaveSafetyCopy();
            _time.Now += TimeSpan.FromMinutes(1);
        }

        // Assert: the three oldest are gone
        string[] kept =
            [.. new DirectoryInfo(Backups).GetFiles().Select(file => file.Name).Order(StringComparer.Ordinal)];
        Assert.Equal(SettingsBackupService.SafetyCopiesKept, kept.Length);
        Assert.Equal("settings-before-restore-2026-11-25-093315.zip", kept[0]);
    }

    [Fact]
    public void SaveSafetyCopy_WithNoSettings_SavesNothing()
    {
        // Act / Assert
        Assert.Null(NewService().SaveSafetyCopy());
        Assert.False(Directory.Exists(Backups));
    }

    private SettingsBackupService NewService() => new(Config, Backups, _time);

    private void WriteConfig(string path, string text)
    {
        string full = Path.Combine(Config, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, Encoding.Latin1.GetBytes(text));
    }

    private string ReadConfig(string path) => File.ReadAllText(Path.Combine(Config, path));

    private string Zip(params (string Name, string Text)[] entries)
    {
        string zip = Path.Combine(_root, $"{Guid.NewGuid():N}.zip");
        Directory.CreateDirectory(_root);

        using ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Create);

        foreach ((string name, string text) in entries)
        {
            using Stream entry = archive.CreateEntry(name).Open();
            entry.Write(Encoding.Latin1.GetBytes(text));
        }

        return zip;
    }

    private static string[] EntriesOf(string zip)
    {
        using ZipArchive archive = ZipFile.OpenRead(zip);

        return [.. archive.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal)];
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
