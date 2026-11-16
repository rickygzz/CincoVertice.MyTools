using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.ConfigEditor.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.ConfigEditor;

public class ConfigSuggesterTests
{
    private static readonly IReadOnlyList<ConfigSettingModel> _settings =
    [
        new("Layout", "A ready layout", [new("Columns 3", "3 columns"), new("Rows 2", "2 rows")]),
        new("Zone", "A zone", [new("0, 0, 50, 100", "The left half")], IsRepeatable: true),
        new("Gap", "Pixels between zones", [new("8", "Default")]),
        new("Monitor", "A monitor's own layout", [new("0, 0, 1920, 1080", "Monitor 1")], true, true),
        new(
            "Run",
            "An action",
            [new("MoveToZone", "Moves"), new("ShowCalculator", "Calculator")],
            IsRepeatable: true,
            ArgumentsOf: action => action == "MoveToZone" ? [new("1", "Zone 1"), new("2", "Zone 2")] : []),
    ];

    [Fact]
    public void ANewLine_OffersEveryName_WithItsEqualsSign()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("", SuggestionTrigger.NewLine);

        // Assert
        Assert.Equal(["Layout", "Zone", "Gap", "Monitor", "Run"], Texts(suggestions));
        Assert.Equal("Layout = ", suggestions!.Items[0].Insert);
    }

    [Fact]
    public void WhileTyping_ANameStarted_IsNarrowed_ButNothingTypedGetsNothing()
    {
        // Act
        CodeSuggestionsModel? started = Suggest("Ga", SuggestionTrigger.Typing);
        CodeSuggestionsModel? empty = Suggest("x = 1\n", SuggestionTrigger.Typing);

        // Assert
        Assert.Equal(["Gap"], Texts(started));
        Assert.Null(empty);
    }

    [Fact]
    public void NamesSetAlready_AreLeftOut_UnlessTheyRepeat_InTheSameSectionOnly()
    {
        // Arrange: the default layout sets Layout and Gap; the monitor's section sets Layout
        string text = "Layout = Columns 3\nGap = 8\nZone = 0, 0, 50, 100\nMonitor = 0, 0, 1920, 1080\nLayout = Rows 2\n";

        // Act
        CodeSuggestionsModel? inMonitor = Suggest(text, SuggestionTrigger.Asked);
        CodeSuggestionsModel? inDefault = Suggest("Layout = Columns 3\n\nMonitor = 0, 0, 1920, 1080", 19);

        // Assert
        Assert.Equal(["Zone", "Gap", "Monitor", "Run"], Texts(inMonitor));
        Assert.Equal(["Zone", "Gap", "Monitor", "Run"], Texts(inDefault));
    }

    [Fact]
    public void AfterTheEqualsSign_TheValuesOfThatName()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("Layout = Ro", SuggestionTrigger.Typing);

        // Assert
        Assert.Equal(["Rows 2"], Texts(suggestions));
        Assert.Equal((9, 2), (suggestions!.Start, suggestions.Length));
    }

    [Fact]
    public void AValueWithCommas_IsOneValue_WhenItsNameTakesNoArguments()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("Zone = 0, 0", SuggestionTrigger.Typing);

        // Assert
        Assert.Equal(["0, 0, 50, 100"], Texts(suggestions));
    }

    [Fact]
    public void AfterAValuesComma_WhatThatValueTakes()
    {
        // Act
        CodeSuggestionsModel? zones = Suggest("Run = MoveToZone, ", SuggestionTrigger.Typing);
        CodeSuggestionsModel? none = Suggest("Run = ShowCalculator, ", SuggestionTrigger.Typing);

        // Assert
        Assert.Equal(["1", "2"], Texts(zones));
        Assert.Null(none);
    }

    [Theory]
    [InlineData("# Lay")]
    [InlineData("Unknown = ")]
    [InlineData("Layout Columns")]
    public void Elsewhere_Nothing(string text)
    {
        // Act / Assert
        Assert.Null(Suggest(text, SuggestionTrigger.Asked));
    }

    private static CodeSuggestionsModel? Suggest(string text, SuggestionTrigger trigger) =>
        ConfigSuggester.Suggest(text, text.Length, trigger, _settings);

    private static CodeSuggestionsModel? Suggest(string text, int caret) =>
        ConfigSuggester.Suggest(text, caret, SuggestionTrigger.Asked, _settings);

    private static IReadOnlyList<string> Texts(CodeSuggestionsModel? suggestions) =>
        [.. suggestions!.Items.Select(item => item.Text)];
}
