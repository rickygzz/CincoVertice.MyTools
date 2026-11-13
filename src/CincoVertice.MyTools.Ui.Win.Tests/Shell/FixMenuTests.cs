using System.Windows.Forms;
using CincoVertice.Common.Application.CodeStandard.Models;
using CincoVertice.MyTools.Ui.CodeReview.Interfaces;
using CincoVertice.MyTools.Ui.CodeReview.Services;
using CincoVertice.MyTools.Ui.Help.Interfaces;
using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Win.CodeReview.Actions;
using CincoVertice.MyTools.Ui.Win.CodeReview.Services;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using CincoVertice.MyTools.Ui.Win.Shell;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Shell;

/// <summary>The tray menu's Fix folder for standard: the fix, then every fixer to tick.</summary>
public sealed class FixMenuTests : IDisposable
{
    private readonly MemoryStore _store = new();
    private readonly FixerSelection _selection;
    private readonly AppIconService _appIcon = new();
    private readonly ToolStripMenuItem _menu;

    public FixMenuTests()
    {
        _selection = new FixerSelection(_store, CodeLanguageModel.All);
        var help = new HelpWindowService(new HelpPageStore(Path.GetTempPath()), new NoSettings(), _appIcon);
        var windows = new CodeReviewWindowService(
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            help);
        _menu = TrayApplicationContext.CreateFixMenu(new FixFolderAction(windows, _selection), _selection);
    }

    public void Dispose()
    {
        _menu.Dispose();
        _appIcon.Dispose();
    }

    [Fact]
    public void TheMenu_IsTheFix_AllOnOrOff_ThenEveryFixer_TickedWhenOn()
    {
        // Act
        List<string> texts = [.. _menu.DropDownItems.Cast<ToolStripItem>().Select(item => item.Text ?? "-")];

        // Assert
        Assert.Equal(["&Fix a folder...", "", "Turn &all on", "Turn all &off", ""], texts.Take(5));
        Assert.Equal(_selection.Choices.Select(choice => choice.Title), texts.Skip(5));
        Assert.All(FixerItems(), item => Assert.True(item.Checked));
    }

    [Fact]
    public void ClickingAFixer_TurnsItOff_AndAgain_On()
    {
        // Arrange
        ToolStripMenuItem indentation = FixerItems().Single(item => (string)item.Tag! == "Indentation");

        // Act and assert
        indentation.PerformClick();
        Assert.False(indentation.Checked);
        Assert.DoesNotContain("Indentation", _selection.On);
        Assert.Equal(["Indentation"], _store.Off);

        indentation.PerformClick();
        Assert.True(indentation.Checked);
        Assert.Contains("Indentation", _selection.On);
    }

    [Fact]
    public void AllOff_UnticksEveryFixer_AndAllOn_TicksThemAgain()
    {
        // Act and assert
        Item("Turn all &off").PerformClick();
        Assert.All(FixerItems(), item => Assert.False(item.Checked));
        Assert.Empty(_selection.On);

        Item("Turn &all on").PerformClick();
        Assert.All(FixerItems(), item => Assert.True(item.Checked));
    }

    [Fact]
    public void AFixersTooltip_SaysWhatItDoes_AndForWhichLanguages()
    {
        // Act
        ToolStripMenuItem item = FixerItems().Single(item => (string)item.Tag! == "TrailingWhitespace");

        // Assert
        Assert.Equal(
            $"Removes spaces and tabs at the end of every line.{Environment.NewLine}C# and JavaScript",
            item.ToolTipText);
    }

    private ToolStripMenuItem Item(string text) =>
        _menu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => item.Text == text);

    private List<ToolStripMenuItem> FixerItems() =>
        [.. _menu.DropDownItems.OfType<ToolStripMenuItem>().Skip(3)];

    private sealed class MemoryStore : IFixerSelectionStore
    {
        public IReadOnlyCollection<string> Off { get; private set; } = [];

        public IReadOnlyCollection<string> LoadOff() => Off;

        public void SaveOff(IReadOnlyCollection<string> names) => Off = [.. names];
    }

    private sealed class NoSettings : IHelpSettingsStore
    {
        public HelpSettingsModel? Load() => null;

        public void Save(HelpSettingsModel settings)
        {
        }
    }
}
