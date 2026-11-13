using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Presenters;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Help;

public class HelpPresenterTests
{
    private readonly FakeHelpView _view = new();

    private readonly FakeHelpPageStore _store = new(new Dictionary<string, string>
    {
        ["02-hotkeys.md"] = "# Hotkeys\n\n## Default hotkeys\n\nSee [zones](01-zones.md#usage).",
        ["01-zones.md"] = "# Window Zones\n\n## Usage\n\n[Up](#window-zones), [web](https://example.com)",
        ["README.md"] = "# User Guide\n\n- [Zones](01-zones.md)\n- [Missing](99-none.md)\n- [File](a.txt)",
        ["notes.md"] = "No heading here.",
    });

    [Fact]
    public void Start_ListsThePages_TheContentsFirst_ByTheirTitles_AndShowsTheContents()
    {
        // Act
        new HelpPresenter(_view, _store).Start();

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
        new HelpPresenter(_view, _store).Start("02-hotkeys.md");

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
        new HelpPresenter(_view, _store).Start("01-zones.md");

        // Act
        _view.ClickLink("#window-zones");
        _view.Back();

        // Assert
        Assert.Single(_view.Shown);
        Assert.Equal(["window-zones", null], _view.Scrolls);
        Assert.Equal((false, true), _view.Navigation);
    }

    [Fact]
    public void AWebLink_OpensOutside()
    {
        // Arrange
        new HelpPresenter(_view, _store).Start("01-zones.md");

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
        new HelpPresenter(_view, _store).Start();

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
        new HelpPresenter(_view, _store).Start();
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
        var presenter = new HelpPresenter(_view, _store);
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
        var presenter = new HelpPresenter(_view, new FakeHelpPageStore([]));

        // Act
        presenter.Start();

        // Assert
        Assert.Empty(_view.Pages);
        Assert.Contains("docs\\user-guide", string.Concat(_view.Document!.Blocks.Select(block => block.PlainText)));
        Assert.Equal((false, false), _view.Navigation);
    }
}
