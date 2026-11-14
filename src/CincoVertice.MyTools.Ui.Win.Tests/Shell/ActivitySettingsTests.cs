using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Shell;

public sealed class ActivitySettingsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "ActivitySettingsTests_" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Read_TakesEverySetting()
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read(
            "# My settings\nInterval = 1000\nidleticksbeforemove = 0\r\nSpeedMin = 30\nSpeedMax = 40 # slow\n"
            + "MoveImmediately = false\n");

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(new MouseMoveSettings(1000, 0, 30, 40, false), result.Settings);
    }

    [Theory]
    [InlineData("MoveImmediately = true", true)]
    [InlineData("MoveImmediately = false", false)]
    [InlineData("moveimmediately = FALSE", false)]
    [InlineData("MoveImmediately = True # right away", true)]
    [InlineData("MoveImmediately = false # wait", false)]
    public void Read_MoveImmediately_IsTrueOrFalseInAnyCase(string text, bool expected)
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read(text);

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(expected, result.Settings.MoveImmediately);
    }

    [Theory]
    [InlineData("MoveImmediately = yes")]
    [InlineData("MoveImmediately = 1")]
    [InlineData("MoveImmediately =")]
    public void Read_MoveImmediatelyThatIsNotTrueOrFalse_KeepsTheDefault(string text)
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read(text);

        // Assert
        Assert.StartsWith("MoveImmediately: must be true or false", Assert.Single(result.Problems));
        Assert.Equal(MouseMoveSettings.Default, result.Settings);
    }

    [Fact]
    public void Read_MoveImmediatelyTwice_UsesTheFirst()
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read(
            "MoveImmediately = false\nMoveImmediately = true");

        // Assert
        Assert.StartsWith("MoveImmediately: set more than once", Assert.Single(result.Problems));
        Assert.False(result.Settings.MoveImmediately);
    }

    [Fact]
    public void Read_ASettingLeftOut_KeepsItsDefault()
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read("Interval = 1000");

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(MouseMoveSettings.Default with { Interval = 1000 }, result.Settings);
    }

    [Fact]
    public void Read_AnEmptyFile_GivesTheDefaults()
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read(string.Empty);

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(MouseMoveSettings.Default, result.Settings);
    }

    [Theory]
    [InlineData("Interval = 99", "Interval: must be a whole number from 100 to 3600000")]
    [InlineData("Interval = 3600001", "Interval: must be a whole number")]
    [InlineData("Interval = 0", "Interval: must be a whole number")]
    [InlineData("Interval = fast", "Interval: must be a whole number")]
    [InlineData("Interval = 1.5", "Interval: must be a whole number")]
    [InlineData("Interval = 99999999999", "Interval: must be a whole number")]
    [InlineData("Interval =", "Interval: must be a whole number")]
    [InlineData("IdleTicksBeforeMove = -1", "IdleTicksBeforeMove: must be a whole number from 0 to 100000")]
    [InlineData("SpeedMin = 19", "SpeedMin: must be a whole number from 20 to 100")]
    [InlineData("SpeedMax = 101", "SpeedMax: must be a whole number from 20 to 100")]
    [InlineData("Speed = 50", "Speed: unknown setting")]
    [InlineData("Interval 1000", "Line 1: 'Interval 1000' is not 'Name = Value'")]
    public void Read_ALineThatCantBeUsed_IsReportedAndKeepsTheDefaults(string text, string expectedProblemStart)
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read(text);

        // Assert
        Assert.StartsWith(expectedProblemStart, Assert.Single(result.Problems));
        Assert.Equal(MouseMoveSettings.Default, result.Settings);
    }

    [Fact]
    public void Read_AQuoteThatIsNeverClosed_IsReportedAndKeepsTheDefault()
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read(
            "SpeedMax = 50\nInterval = [\"1000\nSpeedMin = 30");

        // Assert
        Assert.StartsWith("Interval: the [\" is never closed", Assert.Single(result.Problems));
        Assert.Equal(MouseMoveSettings.Default with { SpeedMax = 50 }, result.Settings);
    }

    [Fact]
    public void Read_ABadLine_DoesNotAffectTheOthers()
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read("Interval =\nSpeedMax = 50\nSpeedMin = 30");

        // Assert
        Assert.Single(result.Problems);
        Assert.Equal(MouseMoveSettings.Default with { SpeedMin = 30, SpeedMax = 50 }, result.Settings);
    }

    [Fact]
    public void Read_ASettingTwice_UsesTheFirst()
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read("Interval = 1000\nINTERVAL = 2000");

        // Assert
        Assert.StartsWith("INTERVAL: set more than once", Assert.Single(result.Problems));
        Assert.Equal(1000, result.Settings.Interval);
    }

    [Fact]
    public void Read_SpeedMinAboveSpeedMax_UsesBothDefaults()
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read("SpeedMin = 80\nSpeedMax = 40");

        // Assert: Random.Next would throw on these on the timer's thread, ending the app
        Assert.StartsWith("SpeedMin (80) is above SpeedMax (40)", Assert.Single(result.Problems));
        Assert.Equal(MouseMoveSettings.Default, result.Settings);
    }

    [Fact]
    public void Read_SpeedMinAboveTheDefaultSpeedMax_IsCaughtToo()
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read("SpeedMax = 30\nSpeedMin = 50");

        // Assert
        Assert.Single(result.Problems);
        Assert.Equal(MouseMoveSettings.Default, result.Settings);
    }

    [Fact]
    public void Read_SpeedMinEqualToSpeedMax_IsFine()
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read("SpeedMin = 50\nSpeedMax = 50");

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal((50, 50), (result.Settings.SpeedMin, result.Settings.SpeedMax));
    }

    [Fact]
    public void Load_WhenFileIsMissing_CreatesItWithTheDefaults()
    {
        // Arrange
        var service = new ActivitySettingsService(Path.Combine(_folder, "config", "activity.cfg"));

        // Act
        ActivitySettingsResultModel result = service.Load();

        // Assert
        Assert.True(File.Exists(service.FilePath));
        Assert.Empty(result.Problems);
        Assert.Equal(MouseMoveSettings.Default, result.Settings);
    }

    [Fact]
    public void Load_ReadsTheFileEveryTime()
    {
        // Arrange
        var service = new ActivitySettingsService(Path.Combine(_folder, "activity.cfg"));
        service.Load();

        // Act
        File.WriteAllText(service.FilePath, "Interval = 1000");
        ActivitySettingsResultModel result = service.Load();

        // Assert
        Assert.Equal(1000, result.Settings.Interval);
    }

    [Fact]
    public void Load_WhenTheFileCantBeRead_ReportsItAndGivesTheDefaults()
    {
        // Arrange: a folder where the file should be
        string path = Path.Combine(_folder, "activity.cfg");
        Directory.CreateDirectory(path);

        // Act
        ActivitySettingsResultModel result = new ActivitySettingsService(path).Load();

        // Assert
        Assert.StartsWith($"Could not use {path}", Assert.Single(result.Problems));
        Assert.Equal(MouseMoveSettings.Default, result.Settings);
    }

    [Fact]
    public void DefaultText_ReadsAsTheDefaultsWithoutProblems()
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read(ActivitySettingsService.CreateDefaultText());

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(MouseMoveSettings.Default, result.Settings);
    }

    [Theory]
    [InlineData("MoveMouse = false")]
    [InlineData("KeepAwake = false # older file")]
    [InlineData("movemouse = true\nkeepawake = maybe")]
    public void Read_OlderFilesSwitches_AreIgnored_WithoutProblems(string text)
    {
        // Act: from when activity also kept awake; that's its own toggle now
        ActivitySettingsResultModel result = ActivitySettingsReader.Read(text);

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(MouseMoveSettings.Default, result.Settings);
    }

    [Fact]
    public void Read_UnknownSetting_DoesntOfferTheOlderSwitches()
    {
        // Act
        ActivitySettingsResultModel result = ActivitySettingsReader.Read("Speed = 3");

        // Assert
        string problem = Assert.Single(result.Problems);
        Assert.DoesNotContain("KeepAwake", problem);
        Assert.DoesNotContain("MoveMouse", problem);
    }

    [Fact]
    public void TheNewFile_HasNoProblems_AndTheDefaults()
    {
        // Act
        string text = ActivitySettingsService.CreateDefaultText();
        ActivitySettingsResultModel result = ActivitySettingsReader.Read(text);

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(MouseMoveSettings.Default, result.Settings);
        Assert.Contains("ToggleKeepAwake", text);
    }

    [Fact]
    public void KeepAwake_IsSeparateFromTheMouseMover()
    {
        // Arrange: the toggles run on this thread, as on the UI thread
        Directory.CreateDirectory(_folder);
        string path = Path.Combine(_folder, "activity.cfg");
        File.WriteAllText(path, "MoveImmediately = false");
        using var simulator = new MouseMoveSimulator();
        var keeper = new AwakeKeeper();
        using var icons = new AppIconService();
        var notifications = new TrayNotificationService();
        var mouseMover = new ToggleActivityAction(
            simulator,
            icons,
            new ActivitySettingsService(path),
            notifications);
        var keepAwake = new ToggleKeepAwakeAction(keeper, icons, notifications);

        try
        {
            // Act: keep awake on, with the mouse mover off
            keepAwake.ExecuteAsync();

            // Assert
            Assert.True(keeper.IsOn);
            Assert.False(simulator.IsRunning);
            Assert.Equal("My Tools\nKeeping awake", icons.ToolTipText);

            // And the mouse mover on and off leaves it on
            mouseMover.ExecuteAsync();
            Assert.True(simulator.IsRunning);
            Assert.Equal("My Tools\nMouse mover on\nKeeping awake", icons.ToolTipText);
            mouseMover.ExecuteAsync();
            Assert.False(simulator.IsRunning);
            Assert.True(keeper.IsOn);

            // And keep awake off
            keepAwake.ExecuteAsync();
            Assert.False(keeper.IsOn);
            Assert.Equal("My Tools", icons.ToolTipText);
        }
        finally
        {
            simulator.Stop();
            keeper.TurnOff();
        }
    }

    [Fact]
    public void MouseMover_DoesntKeepAwake()
    {
        // Arrange
        Directory.CreateDirectory(_folder);
        string path = Path.Combine(_folder, "activity.cfg");
        File.WriteAllText(path, "KeepAwake = true\nMoveImmediately = false");
        using var simulator = new MouseMoveSimulator();
        using var icons = new AppIconService();
        var mouseMover = new ToggleActivityAction(
            simulator,
            icons,
            new ActivitySettingsService(path),
            new TrayNotificationService());

        try
        {
            // Act
            mouseMover.ExecuteAsync();

            // Assert: the red icon, and nothing about keeping awake
            Assert.True(mouseMover.IsOn);
            Assert.Equal("My Tools\nMouse mover on", icons.ToolTipText);
        }
        finally
        {
            simulator.Stop();
        }
    }
}
