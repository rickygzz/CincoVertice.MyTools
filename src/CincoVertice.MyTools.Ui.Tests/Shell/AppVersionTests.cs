using CincoVertice.MyTools.Ui.Shell.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Shell;

public class AppVersionTests
{
    [Theory]
    [InlineData("v0.0.6", "0.0.6")]
    [InlineData("V1.2.3", "1.2.3")]
    [InlineData("0.0.6", "0.0.6")]
    [InlineData("0.0.6+3f2a1c9d", "0.0.6")]
    [InlineData("1.4.0-beta.1", "1.4.0")]
    [InlineData(" 2.1 ", "2.1.0")]
    [InlineData("0.0.6.0", "0.0.6")]
    public void Parse_GivesMajorMinorPatch(string text, string expected)
    {
        // Act
        Version? version = AppVersion.Parse(text);

        // Assert
        Assert.Equal(Version.Parse(expected), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("latest")]
    [InlineData("v")]
    public void Parse_WithoutAVersion_GivesNothing(string? text)
    {
        Assert.Null(AppVersion.Parse(text));
    }

    [Fact]
    public void Parse_TheSameVersionWrittenTwoWays_IsEqual()
    {
        // Arrange: Version alone counts 0.0.6 as older than 0.0.6.0
        Version? fromTag = AppVersion.Parse("v0.0.6");
        Version? fromApp = AppVersion.Parse("0.0.6.0");

        // Act & Assert
        Assert.False(fromTag > fromApp);
        Assert.False(fromApp > fromTag);
    }

    [Fact]
    public void Of_ReadsTheAssemblysVersion()
    {
        // Act
        Version version = AppVersion.Of(typeof(AppVersion).Assembly);

        // Assert: always three parts, whatever the build adds
        Assert.Equal(3, version.ToString().Split('.').Length);
    }

    [Fact]
    public void RepositoryUrlOf_AnAssemblyBuiltWithoutOne_GivesNothing()
    {
        // The test assembly's csproj has no <RepositoryUrl>
        Assert.Null(AppVersion.RepositoryUrlOf(typeof(AppVersionTests).Assembly));
    }

    [Fact]
    public void Format_ShowsThreeParts()
    {
        Assert.Equal("0.0.6", AppVersion.Format(new Version(0, 0, 6)));
    }
}
