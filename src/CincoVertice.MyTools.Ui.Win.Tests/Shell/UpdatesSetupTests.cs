using CincoVertice.MyTools.Ui.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Setup;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Shell;

public class UpdatesSetupTests
{
    [Fact]
    public void RepositoryUrl_IsTheCsprojsGitHubRepository()
    {
        // Arrange: without one, or with one that isn't on GitHub, the app couldn't start
        string url = UiWinSetupExtensions.RepositoryUrl;

        // Act
        var source = new GitHubReleaseSource(new HttpClient(), url, new Version(0, 0, 0));

        // Assert
        Assert.Equal($"{url.TrimEnd('/')}/releases", source.ReleasesPageUrl);
    }
}
