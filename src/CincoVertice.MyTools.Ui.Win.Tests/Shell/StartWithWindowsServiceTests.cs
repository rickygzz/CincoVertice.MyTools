using CincoVertice.MyTools.Ui.Win.Shell.Services;
using Microsoft.Win32;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Shell;

public sealed class StartWithWindowsServiceTests : IDisposable
{
    private const string ExePath = @"C:\Program Files\My Tools\MyTools.exe";

    // Never the real keys: these tests must not make anything start with the computer
    private readonly string _root = @"Software\CincoVerticeTests_" + Guid.NewGuid();
    private readonly string _runKey;
    private readonly string _approvedKey;
    private readonly StartWithWindowsService _service;

    public StartWithWindowsServiceTests()
    {
        _runKey = _root + @"\Run";
        _approvedKey = _root + @"\Approved";
        _service = new StartWithWindowsService(ExePath, _runKey, _approvedKey);
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_root, throwOnMissingSubKey: false);
    }

    private void SetApproved(params byte[] state)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(_approvedKey);

        key.SetValue("MyTools", state, RegistryValueKind.Binary);
    }

    private void SetRunValue(string command)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(_runKey);

        key.SetValue("MyTools", command);
    }

    private object? RunValue()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_runKey);

        return key?.GetValue("MyTools");
    }

    private object? ApprovedValue()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_approvedKey);

        return key?.GetValue("MyTools");
    }

    [Fact]
    public void IsEnabled_WithNothingSet_IsOff()
    {
        Assert.False(_service.IsEnabled);
    }

    [Fact]
    public void SetEnabled_TurnsItOn_WithThePathInQuotes_AndTheStartupArgument()
    {
        // Act
        _service.SetEnabled(true);

        // Assert: Windows would stop at the first space otherwise
        Assert.True(_service.IsEnabled);
        Assert.Equal($"\"{ExePath}\" --startup", RunValue());
    }

    [Fact]
    public void SetEnabled_TurnsItOff_LeavingNothingBehind()
    {
        // Arrange
        _service.SetEnabled(true);

        // Act
        _service.SetEnabled(false);

        // Assert
        Assert.False(_service.IsEnabled);
        Assert.Null(RunValue());
    }

    [Fact]
    public void SetEnabled_OffWhenItsAlreadyOff_IsFine()
    {
        // Act
        _service.SetEnabled(false);

        // Assert
        Assert.False(_service.IsEnabled);
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(7, false)]
    [InlineData(2, true)]
    [InlineData(6, true)]
    public void IsEnabled_FollowsWhatWindowsSays(byte state, bool expected)
    {
        // Arrange: Task Manager or Settings changed it
        _service.SetEnabled(true);
        SetApproved(state, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        // Act & Assert
        Assert.Equal(expected, _service.IsEnabled);
    }

    [Fact]
    public void SetEnabled_On_OverrulesHavingBeenTurnedOffInWindows()
    {
        // Arrange
        _service.SetEnabled(true);
        SetApproved(3, 0, 0, 0);

        // Act
        _service.SetEnabled(true);

        // Assert
        Assert.True(_service.IsEnabled);
        Assert.Null(ApprovedValue());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SetEnabled_WhenTheRunKeyCantBeChanged_KeepsItTurnedOffInWindows(bool enabled)
    {
        // Arrange: a name the registry refuses stands for a Run key that can't be written. Forgetting the mark anyway
        // would turn back on an older command that's still in the real key
        SetApproved(3, 0, 0, 0);
        var service = new StartWithWindowsService(ExePath, new string('x', 300), _approvedKey);

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => service.SetEnabled(enabled));
        Assert.NotNull(ApprovedValue());
    }

    [Fact]
    public void SetEnabled_Off_ForgetsHavingBeenTurnedOffInWindows()
    {
        // Arrange
        _service.SetEnabled(true);
        SetApproved(3, 0, 0, 0);

        // Act
        _service.SetEnabled(false);

        // Assert
        Assert.Null(RunValue());
        Assert.Null(ApprovedValue());
    }

    [Fact]
    public void IsEnabled_ForAnotherCopyOfTheApp_IsOff_AndTurningItOnReplacesIt()
    {
        // Arrange: this copy was moved, or another is registered
        new StartWithWindowsService(@"C:\Old\MyTools.exe", _runKey, _approvedKey).SetEnabled(true);

        // Act & Assert
        Assert.False(_service.IsEnabled);

        _service.SetEnabled(true);

        Assert.True(_service.IsEnabled);
        Assert.Equal($"\"{ExePath}\" --startup", RunValue());
    }

    [Fact]
    public void IsEnabled_WithTheCommandOfAnOlderVersion_IsOn()
    {
        // Arrange: before --startup
        SetRunValue($"\"{ExePath}\"");

        // Act & Assert
        Assert.True(_service.IsEnabled);
    }

    [Fact]
    public void UpdateCommand_GivesTheCommandOfAnOlderVersionTheStartupArgument_KeepingItTurnedOffInWindows()
    {
        // Arrange
        SetRunValue($"\"{ExePath}\"");
        SetApproved(3, 0, 0, 0);

        // Act
        _service.UpdateCommand();

        // Assert
        Assert.Equal($"\"{ExePath}\" --startup", RunValue());
        Assert.NotNull(ApprovedValue());
        Assert.False(_service.IsEnabled);
    }

    [Fact]
    public void UpdateCommand_LeavesAnotherCopyAlone()
    {
        // Arrange
        SetRunValue(@"""C:\Old\MyTools.exe""");

        // Act
        _service.UpdateCommand();

        // Assert
        Assert.Equal(@"""C:\Old\MyTools.exe""", RunValue());
    }

    [Fact]
    public void UpdateCommand_WhenItsOff_AddsNothing()
    {
        // Act
        _service.UpdateCommand();

        // Assert
        Assert.Null(RunValue());
    }

    [Fact]
    public void IsEnabled_IgnoresCaseInThePath()
    {
        // Arrange: paths on Windows don't care
        _service.SetEnabled(true);

        // Act & Assert
        Assert.True(new StartWithWindowsService(ExePath.ToUpperInvariant(), _runKey, _approvedKey).IsEnabled);
    }
}
