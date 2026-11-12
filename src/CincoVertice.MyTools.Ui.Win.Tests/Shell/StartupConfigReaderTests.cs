using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Shell;

public class StartupConfigReaderTests
{
    private static readonly string[] _actions = ["StartLater", "ShowCalculator"];

    [Fact]
    public void Read_GivesTheActionsInOrder_WithTheirValues_AsTheActionsSpellThem()
    {
        // Act
        StartupConfigResultModel result = StartupConfigReader.Read(
            "# Startup\nRun = showcalculator\n\nrun = StartLater, slack # note\nRun = StartLater, app#1",
            _actions);

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(
            [
                new StartupActionModel("ShowCalculator", null),
                new StartupActionModel("StartLater", "slack"),
                new StartupActionModel("StartLater", "app#1"),
            ],
            result.Actions);
    }

    [Fact]
    public void Read_AProgramsPath_IsTheValue_AsWritten()
    {
        // Act: as in the user guide
        StartupConfigResultModel result = StartupConfigReader.Read(
            @"Run = StartLater, C:\Program Files\Slack\slack.exe",
            _actions);

        // Assert
        Assert.Equal(@"C:\Program Files\Slack\slack.exe", Assert.Single(result.Actions).Argument);
    }

    [Fact]
    public void Read_TheNewFile_RunsNothing_AndHasNoProblems()
    {
        // Act
        StartupConfigResultModel result = StartupConfigReader.Read(StartupConfigService.CreateDefaultText(), _actions);

        // Assert
        Assert.Empty(result.Actions);
        Assert.Empty(result.Problems);
    }

    [Theory]
    [InlineData("Run = Nope", "Nope: unknown action. Actions: StartLater, ShowCalculator.")]
    [InlineData("Start = StartLater", "Start: each line is Run = Action.")]
    [InlineData("Run =", "Run: which action? Run = Action.")]
    [InlineData("StartLater", "'StartLater' is not 'Run = Action'.")]
    public void Read_ALineThatCantBeUsed_IsAProblemWithItsLine(string line, string message)
    {
        // Act
        StartupConfigResultModel result = StartupConfigReader.Read("Run = ShowCalculator\n" + line, _actions);

        // Assert
        Assert.Equal("ShowCalculator", Assert.Single(result.Actions).ActionName);
        Assert.Equal(new ConfigLineProblemModel(2, message), Assert.Single(result.LineProblems));
        Assert.Equal($"Line 2: {message}", Assert.Single(result.Problems));
    }

    [Fact]
    public void Read_TheSameActionAndValueTwice_RunsOnce()
    {
        // Act
        StartupConfigResultModel result = StartupConfigReader.Read(
            "Run = StartLater, a\nRun = startlater, a\nRun = StartLater, b",
            _actions);

        // Assert
        Assert.Equal(["a", "b"], result.Actions.Select(action => action.Argument));
        Assert.Equal(
            "Line 2: StartLater with a: runs more than once; the first line is used.",
            Assert.Single(result.Problems));
    }
}
