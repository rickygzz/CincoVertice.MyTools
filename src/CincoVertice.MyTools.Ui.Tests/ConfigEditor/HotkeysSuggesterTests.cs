using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.ConfigEditor.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.ConfigEditor;

public class HotkeysSuggesterTests
{
    private static readonly IReadOnlyList<CodeSuggestionModel> _actions =
    [
        new("MoveToNextZone", "Next zone"),
        new("MoveToZone", "A zone"),
        new("ShowCalculator", "Calculator"),
        new("ToggleActivity", "Mouse mover"),
    ];

    private static readonly IReadOnlyList<string> _keys = ["A", "C", "M", "F1", "F12", "Space"];

    [Fact]
    public void AtTheStartOfANewLine_TheActions_ThatHaveWhatWasTyped_WithTheirEqualsSign()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("# Hotkeys\nzone");

        // Assert: those that start with it first
        Assert.Equal(["MoveToNextZone", "MoveToZone"], Texts(suggestions));
        Assert.Equal("MoveToNextZone = ", suggestions!.Items[0].Insert);
        Assert.Equal((10, 4), (suggestions.Start, suggestions.Length));
    }

    [Fact]
    public void AnActionRenamed_KeepsTheRestOfItsLine()
    {
        // Arrange
        string text = "Show = Ctrl+Alt+C";

        // Act
        CodeSuggestionsModel? suggestions = HotkeysSuggester.Suggest(text, 4, false, _actions, _keys, 3);

        // Assert
        Assert.Equal("ShowCalculator", Assert.Single(suggestions!.Items).Insert);
    }

    [Fact]
    public void NothingTyped_OffersOnlyWhenAskedFor()
    {
        // Act
        CodeSuggestionsModel? typing = HotkeysSuggester.Suggest("", 0, false, _actions, _keys, 3);
        CodeSuggestionsModel? asked = HotkeysSuggester.Suggest("", 0, true, _actions, _keys, 3);

        // Assert
        Assert.Null(typing);
        Assert.Equal(4, asked!.Items.Count);
    }

    [Fact]
    public void AfterTheEqualsSign_TheModifiers_ThenTheKeys()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("ShowCalculator = ");

        // Assert
        Assert.Equal(["Ctrl", "Alt", "Shift", "Win", "A", "C", "M", "F1", "F12", "Space"], Texts(suggestions));
        Assert.Equal("Ctrl+", suggestions!.Items[0].Insert);
        Assert.Equal("Modifier", suggestions.Items[0].Detail);
    }

    [Fact]
    public void AfterAModifier_NotThatOneAgain_AndNarrowedByWhatWasTyped()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("ShowCalculator = Ctrl+Alt+F");

        // Assert
        Assert.Equal(["F1", "F12"], Texts(suggestions));
        Assert.Equal(1, suggestions!.Length);
    }

    [Fact]
    public void AKey_SaysWhichActionAlreadyHasTheShortcutItMakes()
    {
        // Arrange
        string text = "ToggleActivity = Alt+Ctrl+M\nShowCalculator = Ctrl+Alt+";

        // Act
        CodeSuggestionsModel? suggestions = HotkeysSuggester.Suggest(text, text.Length, false, _actions, _keys, 3);

        // Assert
        Assert.Equal("Key; already used by ToggleActivity", suggestions!.Items.Single(item => item.Text == "M").Detail);
        Assert.Equal("Key", suggestions.Items.Single(item => item.Text == "C").Detail);
    }

    [Fact]
    public void AfterMoveToZonesComma_TheZoneNumbers()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("MoveToZone = Ctrl+Alt+1, ");

        // Assert
        Assert.Equal(["1", "2", "3"], Texts(suggestions));
        Assert.Equal("Zone 1", suggestions!.Items[0].Detail);
    }

    [Theory]
    [InlineData("# Show")]
    [InlineData("ShowCalculator = Ctrl+Alt+C, ")]
    [InlineData("Show Calc")]
    public void Elsewhere_Nothing(string text)
    {
        // Act / Assert
        Assert.Null(Suggest(text));
    }

    private static CodeSuggestionsModel? Suggest(string text) =>
        HotkeysSuggester.Suggest(text, text.Length, false, _actions, _keys, 3);

    private static IReadOnlyList<string> Texts(CodeSuggestionsModel? suggestions) =>
        [.. suggestions!.Items.Select(item => item.Text)];
}
