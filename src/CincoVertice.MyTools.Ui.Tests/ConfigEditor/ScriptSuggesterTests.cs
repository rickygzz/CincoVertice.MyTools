using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.ConfigEditor.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.ConfigEditor;

public class ScriptSuggesterTests
{
    private static readonly IReadOnlyList<ScriptFunctionModel> _functions =
    [
        new("clipboard.gettext", "clipboard.gettext()", "Reads the clipboard", false),
        new("clipboard.settext", "clipboard.settext(text)", "Writes the clipboard", true),
        new("keyboard.press", "keyboard.press(keys)", "Presses keys", true),
        new("text.upper", "text.upper(text)", "In capitals", true),
        new("text", "text(value)", "Any value as text", true),
    ];

    private static readonly IReadOnlyList<string> _keywords = ["if", "else", "end", "true"];

    [Fact]
    public void AfterAModulesDot_ItsFunctions_WithHowTheyreCalled()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("x = clipboard.");

        // Assert
        Assert.Equal(["gettext", "settext"], Texts(suggestions));
        Assert.Equal("clipboard.gettext(): Reads the clipboard", suggestions!.Items[0].Detail);
        Assert.Equal("gettext()", suggestions.Items[0].Insert);
        Assert.Equal("settext(", suggestions.Items[1].Insert);
    }

    [Fact]
    public void AFunctionStarted_IsNarrowed()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("x = clipboard.se");

        // Assert
        Assert.Equal("settext", Assert.Single(suggestions!.Items).Text);
        Assert.Equal((14, 2), (suggestions.Start, suggestions.Length));
    }

    [Fact]
    public void AWord_TheVariablesSet_TheKeywords_TheModules_AndTheGlobalFunctions()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("total = 1\ntitle = \"a\"\nt", explicitly: false);

        // Assert
        Assert.Equal(["title", "total", "true", "text", "text(value)"], Texts(suggestions));
        Assert.Equal("Variable", suggestions!.Items[0].Detail);
        Assert.Equal("text.", suggestions.Items[3].Insert);
        Assert.Equal("Module: upper", suggestions.Items[3].Detail);
        Assert.Equal("text(", suggestions.Items[4].Insert);
    }

    [Fact]
    public void TheLineBeingTyped_IsNoVariableYet()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("total = to");

        // Assert
        Assert.Null(suggestions);
    }

    [Theory]
    [InlineData("# a comment with clip")]
    [InlineData("x = \"clip")]
    [InlineData("x = 12")]
    [InlineData("x = \"\"\"\n    clip")]
    [InlineData("")]
    public void InCommentsTextsAndNumbers_OrWithNothingTyped_Nothing(string text)
    {
        // Act / Assert
        Assert.Null(Suggest(text, explicitly: false));
    }

    [Fact]
    public void AfterAClosedTextOfSeveralLines_WordsAgain()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("x = \"\"\"\n    a\n    \"\"\"\ntex");

        // Assert
        Assert.Contains("text", Texts(suggestions));
    }

    [Fact]
    public void InScriptInclude_TheLibraries()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("script.include(\"_P");

        // Assert
        Assert.Equal("_PrCommon", Assert.Single(suggestions!.Items).Text);
        Assert.Equal(2, suggestions.Length);
    }

    [Fact]
    public void InKeyboardPress_TheModifiers_AltGrToo_ThenTheKeys()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("keyboard.press(\"Ctrl+");

        // Assert
        Assert.Equal(["Alt", "Shift", "Win", "AltGr", "V", "Tab"], Texts(suggestions));
    }

    [Fact]
    public void AfterAClosedText_WordsAgain()
    {
        // Act
        CodeSuggestionsModel? suggestions = Suggest("x = \"a \\\" b\" + tex");

        // Assert
        Assert.Contains("text", Texts(suggestions));
    }

    private static CodeSuggestionsModel? Suggest(string text, bool explicitly = false) =>
        ScriptSuggester.Suggest(text, text.Length, explicitly, _functions, _keywords, ["_PrCommon"], ["V", "Tab"]);

    private static IReadOnlyList<string> Texts(CodeSuggestionsModel? suggestions) =>
        [.. suggestions!.Items.Select(item => item.Text)];
}
