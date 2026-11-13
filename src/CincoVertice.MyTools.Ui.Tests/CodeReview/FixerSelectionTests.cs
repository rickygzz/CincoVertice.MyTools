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

    [Fact]
    public void ALineEndingAndAnEncoding_AreSaved_AndGoInTheOptions()
    {
        // Arrange
        var selection = new FixerSelection(_store, CodeLanguageModel.All);
        selection.Set("Indentation", false);

        // Act
        selection.SetLineEnding(LineEndingEnum.CRLF);
        selection.SetEncoding(FileEncodingEnum.Utf8Bom);
        var next = new FixerSelection(_store, CodeLanguageModel.All);

        // Assert
        Assert.Equal((LineEndingEnum.CRLF, FileEncodingEnum.Utf8Bom), (next.LineEnding, next.Encoding));
        FixOptionsModel options = next.Options;
        Assert.Equal((LineEndingEnum.CRLF, FileEncodingEnum.Utf8Bom), (options.LineEnding, options.Encoding));
        Assert.DoesNotContain("Indentation", options.Fixers!);
    }

    [Fact]
    public void AtFirst_LineEndingsAndEncodings_AreKept()
    {
        // Act
        var selection = new FixerSelection(_store, CodeLanguageModel.All);

        // Assert
        Assert.Equal((LineEndingEnum.None, FileEncodingEnum.Keep), (selection.LineEnding, selection.Encoding));
        Assert.Equal("Keep as they are", FixerSelection.TitleOf(selection.LineEnding));
        Assert.Equal("Keep as it is", FixerSelection.TitleOf(selection.Encoding));
    }

    [Fact]
    public void TheFile_KeepsTheChoices()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), "FixerSelectionTests_" + Guid.NewGuid() + ".json");
        var store = new JsonFileFixerSelectionStore(path);
        var saved = new FixerSettingsModel(["Indentation"], LineEndingEnum.LF, FileEncodingEnum.Utf8);

        try
        {
            // Act
            store.Save(saved);
            FixerSettingsModel loaded = store.Load();

            // Assert
            Assert.Equal(saved.Off, loaded.Off);
            Assert.Equal((LineEndingEnum.LF, FileEncodingEnum.Utf8), (loaded.LineEnding, loaded.Encoding));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class MemoryStore : IFixerSelectionStore
    {
        public IReadOnlyCollection<string> Off { get; set; } = [];

        public LineEndingEnum LineEnding { get; set; }

        public FileEncodingEnum Encoding { get; set; }

        public FixerSettingsModel Load() => new(Off, LineEnding, Encoding);

        public void Save(FixerSettingsModel settings)
        {
            Off = [.. settings.Off];
            LineEnding = settings.LineEnding;
            Encoding = settings.Encoding;
        }
    }
}
