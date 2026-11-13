using System.Drawing;
using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Help;

public sealed class JsonFileHelpSettingsStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "HelpSettingsTests_" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void WhatsSaved_IsLoaded()
    {
        // Arrange
        var store = new JsonFileHelpSettingsStore(Path.Combine(_folder, "config", "help.json"));
        var settings = new HelpSettingsModel("08-scripts.md", new Rectangle(-1200, 40, 900, 700), true);

        // Act
        store.Save(settings);

        // Assert
        Assert.Equal(settings, store.Load());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not json")]
    public void NoFile_OrABrokenOne_GivesNoSettings(string? text)
    {
        // Arrange
        string path = Path.Combine(_folder, "help.json");

        if (text is not null)
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(path, text);
        }

        // Act and assert
        Assert.Null(new JsonFileHelpSettingsStore(path).Load());
    }

    [Fact]
    public void NoBounds_LoadsAsNone()
    {
        // Arrange
        var store = new JsonFileHelpSettingsStore(Path.Combine(_folder, "help.json"));

        // Act
        store.Save(new HelpSettingsModel("README.md", null, false));

        // Assert
        Assert.Equal(new HelpSettingsModel("README.md", null, false), store.Load());
    }
}
