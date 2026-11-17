using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Scripting;

public class DesktopScriptHostTests
{
    [Fact]
    public void FullPath_TakesAPathThatIsntFull_InTheScriptsFolder()
    {
        // Act
        string path = DesktopScriptHost.FullPath(@"notes\today.txt");

        // Assert
        Assert.Equal(Path.Combine(ScriptCatalog.FolderPath, "notes", "today.txt"), path);
    }

    [Fact]
    public void AppendFile_CreatesTheFile_ThenAddsToIt_AndFileExistsSaysSo()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), $"append-{Guid.NewGuid():N}.txt");
        var host = new DesktopScriptHost("Test", new TrayNotificationService(), null!);

        try
        {
            // Act
            bool before = host.FileExists(path);
            host.AppendFile(path, "one\n");
            host.AppendFile(path, "two\n");

            // Assert
            Assert.False(before);
            Assert.True(host.FileExists(path));
            Assert.Equal("one\ntwo\n", host.ReadFile(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FullPath_ExpandsVariables_AndKeepsAFullPath()
    {
        // Act
        string expanded = DesktopScriptHost.FullPath(@"%USERPROFILE%\notes.txt");
        string full = DesktopScriptHost.FullPath(@" C:\work\notes.txt ");

        // Assert
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Assert.Equal(Path.Combine(profile, "notes.txt"), expanded);
        Assert.Equal(@"C:\work\notes.txt", full);
    }
}
