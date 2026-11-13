using CincoVertice.Common.Application.CodeStandard.Models;
using CincoVertice.MyTools.Ui.CodeReview.Interfaces;
using CincoVertice.MyTools.Ui.CodeReview.Models;
using CincoVertice.MyTools.Ui.CodeReview.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.CodeReview;

public class FixerSelectionTests
{
    private readonly MemoryStore _store = new();

    [Fact]
    public void Choices_AreEveryFixerOfEveryLanguage_Once_ByTitle_AllOnAtFirst()
    {
        // Act
        IReadOnlyList<FixerChoiceModel> choices = new FixerSelection(_store, CodeLanguageModel.All).Choices;

        // Assert
        string[] names = [.. CodeLanguageModel.All.SelectMany(language => language.Fixers.Names).Distinct()];
        Assert.Equal(names.Order(), choices.Select(choice => choice.Name).Order());
        Assert.Equal(
            choices.Select(choice => choice.Title).Order(StringComparer.CurrentCultureIgnoreCase),
            choices.Select(choice => choice.Title));
        Assert.All(choices, choice => Assert.True(choice.IsOn));
    }

    [Theory]
    [InlineData("TrailingWhitespace", "C# and JavaScript")]
    [InlineData("BracePlacement", "C#")]
    [InlineData("BraceIndentation", "JavaScript")]
    public void AChoice_SaysTheLanguagesItFixes(string name, string languages)
    {
        // Act
        FixerChoiceModel choice = new FixerSelection(_store, CodeLanguageModel.All).Choices.Single(
            choice => choice.Name == name);

        // Assert
        Assert.Equal(languages, choice.Languages);
    }

    [Fact]
    public void EveryFixer_HasItsWordsInTheCatalog()
    {
        Assert.All(
            CodeLanguageModel.All.SelectMany(language => language.Fixers.Names),
            name => Assert.True(FixerCatalog.Knows(name), $"{name} isn't in FixerCatalog."));
    }

    [Fact]
    public void TurningAFixerOff_LeavesItOut_AndIsSaved_ForTheNextTime()
    {
        // Arrange
        var selection = new FixerSelection(_store, CodeLanguageModel.All);

        // Act
        selection.Set("Indentation", false);
        var next = new FixerSelection(_store, CodeLanguageModel.All);

        // Assert
        Assert.DoesNotContain("Indentation", next.On);
        Assert.Contains("TrailingWhitespace", next.On);
        Assert.Equal(["Indentation"], next.OffTitles);
        Assert.Equal(["Indentation"], _store.Off);
    }

    [Fact]
    public void AllOff_ThenOne_On_RunsOnlyThatOne()
    {
        // Arrange
        var selection = new FixerSelection(_store, CodeLanguageModel.All);

        // Act
        selection.SetAll(false);
        selection.Set("TrailingWhitespace", true);

        // Assert
        Assert.Equal(["TrailingWhitespace"], selection.On);

        // Act: all on again
        selection.SetAll(true);
        Assert.Empty(selection.OffTitles);
    }

    [Fact]
    public void AFixerAddedLater_IsOn_AndANameNoLongerThere_IsIgnored()
    {
        // Arrange: saved before any choice about Indentation, with a fixer turned off that's gone since
        _store.Off = ["Gone"];

        // Act
        var selection = new FixerSelection(_store, CodeLanguageModel.All);

        // Assert
        Assert.Contains("Indentation", selection.On);
        Assert.Empty(selection.OffTitles);
    }

    [Theory]
    [InlineData("BracePlacement", "Braces on their own lines")]
    [InlineData("SomethingNew", "Something new")]
    public void ATitle_IsTheCatalogs_OrTheNameInWords(string name, string title)
    {
        Assert.Equal(title, FixerCatalog.TitleOf(name));
    }

    private sealed class MemoryStore : IFixerSelectionStore
    {
        public IReadOnlyCollection<string> Off { get; set; } = [];

        public IReadOnlyCollection<string> LoadOff() => Off;

        public void SaveOff(IReadOnlyCollection<string> names) => Off = [.. names];
    }
}
