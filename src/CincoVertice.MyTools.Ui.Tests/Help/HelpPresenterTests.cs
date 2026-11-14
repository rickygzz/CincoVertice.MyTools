using System.Drawing;
using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Presenters;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Help;

public class HelpPresenterTests
{
    private readonly FakeHelpView _view = new();
    private readonly FakeHelpSettingsStore _settings = new();

    private readonly FakeHelpPageStore _store = new(new Dictionary<string, string>
    {
        ["02-hotkeys.md"] = "# Hotkeys\n\n## Default hotkeys\n\nSee [zones](01-zones.md#usage).",
        ["01-zones.md"] = "# Window Zones\n\n## Usage\n\n[Up](#window-zones), [web](https://example.com)",
        ["README.md"] = "# User Guide\n\n- [Zones](01-zones.md)\n- [Missing](99-none.md)\n- [File](a.txt)",
        ["notes.md"] = "No heading here.",
    });

    [Fact]
    public void Search_ListsThePagesWithTheText_TheMostFirst_ThenInTheGuidesOrder()
    {
        // Arrange
        new HelpPresenter(_view, _store, _settings).Start();

        // Act: in a heading, a list and a paragraph, whatever its case
        _view.SearchFor("  ZONES ");

        // Assert
        Assert.Equal("ZONES", _view.Search!.Value.Text);
        Assert.Equal(
            [
                new HelpSearchResultModel("README.md", "User Guide", "Zones", 1),
                new HelpSearchResultModel("01-zones.md", "Window Zones", "Window Zones", 1),
                new HelpSearchResultModel("02-hotkeys.md", "Hotkeys", "See zones.", 1),
            ],
            _view.Search.Value.Results);
    }

    [Fact]
    public void ChoosingAFoundPage_OpensIt_AtTheText_AndAnEmptySearch_ListsThePagesAgain()
    {
        // Arrange
        new HelpPresenter(_view, _store, _settings).Start();
        _view.SearchFor("usage");

        // Act
        _view.ChooseResult(Assert.Single(_view.Search!.Value.Results));
        _view.SearchFor("");

        // Assert
        Assert.Equal(("01-zones.md", "Window Zones", null), _view.Shown[^1]);
        Assert.Equal(["usage"], _view.Highlights);
        Assert.Null(_view.Search);
        Assert.Equal(4, _view.Pages.Count);
    }

    [Fact]
    public void ThemeButton_ShowsTheOther_AndTheNextWindowOpensWithIt()
    {
        // Arrange: dark, as Windows is
        _view.IsDark = true;
        var presenter = new HelpPresenter(_view, _store, _settings);
        presenter.RestoreWindow();
        presenter.Start();

        // Act
        _view.ToggleTheme();
        _view.Close();
        var next = new FakeHelpView { IsDark = true };
        new HelpPresenter(next, _store, _settings).RestoreWindow();

        // Assert
        Assert.Equal([false], _view.ThemesShown);
        Assert.False(_settings.Saved!.IsDark);
        Assert.Equal([false], next.ThemesShown);
    }

    [Fact]
    public void Theme_NeverChosen_FollowsWindows()
    {
        // Arrange
        var presenter = new HelpPresenter(_view, _store, _settings);

        // Act
        presenter.RestoreWindow();
        presenter.Start();
        _view.Close();

        // Assert
        Assert.Empty(_view.ThemesShown);
        Assert.Null(_settings.Saved!.IsDark);
    }

    [Fact]
    public void Closing_SavesThePageAndTheWindow_WhichTheNextOneOpensWith()
    {
        // Arrange
        new HelpPresenter(_view, _store, _settings).Start("02-hotkeys.md");
        _view.WindowBounds = new Rectangle(100, 50, 900, 700);
        _view.IsMaximized = true;

        // Act
        _view.Close();
        var next = new FakeHelpView();
        var presenter = new HelpPresenter(next, _store, _settings);
        presenter.RestoreWindow();
        presenter.Start();

        // Assert
        Assert.Equal((new Rectangle(100, 50, 900, 700), true), next.Placed);
        Assert.Equal(("02-hotkeys.md", "Hotkeys", null), next.Shown[^1]);
    }

    [Fact]
    public void Start_AtAPage_OrWithALastPageNoLongerThere_DoesntUseTheLastPage()
    {
        // Arrange
        _settings.Saved = new HelpSettingsModel("02-hotkeys.md", null, false);
        var gone = new FakeHelpView();
        var settingsOfGone = new FakeHelpSettingsStore { Saved = new HelpSettingsModel("old.md", null, false) };

        // Act
        new HelpPresenter(_view, _store, _settings).Start("01-zones.md");
        new HelpPresenter(gone, _store, settingsOfGone).Start();

        // Assert
        Assert.Equal("01-zones.md", _view.Shown[^1].Name);
        Assert.Equal("README.md", gone.Shown[^1].Name);
        Assert.Empty(gone.Problems);
    }

    [Fact]
    public void Start_ListsThePages_TheContentsFirst_ByTheirTitles_AndShowsTheContents()
    {
        // Act
        new HelpPresenter(_view, _store, _settings).Start();

        // Assert
        Assert.Equal(
            [
                new HelpPageModel("README.md", "User Guide"),
                new HelpPageModel("01-zones.md", "Window Zones"),
                new HelpPageModel("02-hotkeys.md", "Hotkeys"),
                new HelpPageModel("notes.md", "notes"),
            ],
            _view.Pages);
        Assert.Equal([("README.md", "User Guide", null)], _view.Shown);
        Assert.Equal((false, false), _view.Navigation);
    }

    [Fact]
    public void ALink_OpensItsPage_AtItsHeading_AndBackGoesBack()
    {
        // Arrange
        new HelpPresenter(_view, _store, _settings).Start("02-hotkeys.md");

        // Act
        _view.ClickLink("01-zones.md#usage");

        // Assert
        Assert.Equal(("01-zones.md", "Window Zones", "usage"), _view.Shown[^1]);
        Assert.Equal((true, false), _view.Navigation);

        // Act: back, then forward again
        _view.Back();
        Assert.Equal(("02-hotkeys.md", "Hotkeys", null), _view.Shown[^1]);
        Assert.Equal((false, true), _view.Navigation);

        _view.Forward();
        Assert.Equal(("01-zones.md", "Window Zones", "usage"), _view.Shown[^1]);
        Assert.Equal((true, false), _view.Navigation);
    }

    [Fact]
    public void ALinkToAHeadingOfThePageShown_ScrollsThere_WithoutShowingItAgain()
    {
        // Arrange
        new HelpPresenter(_view, _store, _settings).Start("01-zones.md");

        // Act
        _view.ClickLink("#window-zones");
        _view.Back();

        // Assert: back where it was, at the top
        Assert.Single(_view.Shown);
        Assert.Equal(["window-zones"], _view.Scrolls);
        Assert.Equal([0], _view.Positions);
        Assert.Equal((false, true), _view.Navigation);
    }

    [Fact]
    public void BackAndForward_ShowEachPlace_ScrolledWhereItWasLeft()
    {
        // Arrange: scrolled down a page, then a link followed, and that page scrolled too
        new HelpPresenter(_view, _store, _settings).Start("02-hotkeys.md");
        _view.TopPosition = 500;
        _view.ClickLink("01-zones.md#usage");
        _view.TopPosition = 42;

        // Act and assert
        _view.Back();
        Assert.Equal(("02-hotkeys.md", "Hotkeys", null), _view.Shown[^1]);
        Assert.Equal([500], _view.Positions);

        _view.Forward();
        Assert.Equal(("01-zones.md", "Window Zones", "usage"), _view.Shown[^1]);
        Assert.Equal([500, 42], _view.Positions);
    }

    [Fact]
    public void Start_AtAHeading_ShowsThePageThere()
    {
        // Act
        new HelpPresenter(_view, _store, _settings).Start("01-zones.md", "usage");

        // Assert
        Assert.Equal([("01-zones.md", "Window Zones", "usage")], _view.Shown);
    }

    [Fact]
    public void AWebLink_OpensOutside()
    {
        // Arrange
        new HelpPresenter(_view, _store, _settings).Start("01-zones.md");

        // Act
        _view.ClickLink("https://example.com");

        // Assert
        Assert.Equal(["https://example.com"], _view.Opened);
        Assert.Single(_view.Shown);
    }

    [Theory]
    [InlineData("99-none.md", "The user guide has no page 99-none.md.")]
    [InlineData("a.txt", "a.txt isn't a page of the user guide.")]
    public void ALinkToNoPage_IsAProblem_AndStaysWhereItIs(string target, string problem)
    {
        // Arrange
        new HelpPresenter(_view, _store, _settings).Start();

        // Act
        _view.ClickLink(target);

        // Assert
        Assert.Equal([problem], _view.Problems);
        Assert.Single(_view.Shown);
        Assert.Equal((false, false), _view.Navigation);
    }

    [Fact]
    public void ChoosingAPage_AfterGoingBack_DropsWhatWasForward()
    {
        // Arrange
        new HelpPresenter(_view, _store, _settings).Start();
        _view.ChoosePage("01-zones.md");
        _view.Back();

        // Act
        _view.ChoosePage("02-hotkeys.md");

        // Assert
        Assert.Equal(("02-hotkeys.md", "Hotkeys", null), _view.Shown[^1]);
        Assert.Equal((true, false), _view.Navigation);
    }

    [Fact]
    public void Open_TheSamePageAgain_AddsNothingToGoBackTo()
    {
        // Arrange
        var presenter = new HelpPresenter(_view, _store, _settings);
        presenter.Start();

        // Act
        presenter.Open("readme.md");

        // Assert
        Assert.Single(_view.Shown);
        Assert.Equal((false, false), _view.Navigation);
    }

    [Fact]
    public void Start_WithoutTheGuide_SaysWhereItShouldBe()
    {
        // Arrange
        var presenter = new HelpPresenter(_view, new FakeHelpPageStore([]), _settings);

        // Act
        presenter.Start();

        // Assert
        Assert.Empty(_view.Pages);
        Assert.Contains("docs\\user-guide", string.Concat(_view.Document!.Blocks.Select(block => block.PlainText)));
        Assert.Equal((false, false), _view.Navigation);
    }
}
