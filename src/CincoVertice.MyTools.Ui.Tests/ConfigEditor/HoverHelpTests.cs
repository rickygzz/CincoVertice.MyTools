using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.ConfigEditor.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.ConfigEditor;

public class HoverHelpTests
{
    private static readonly IReadOnlyList<ScriptFunctionModel> _functions =
    [
        new("clipboard.gettext", "clipboard.gettext()", "The clipboard's text", false),
        new("clipboard.settext", "clipboard.settext(text)", "Puts text in the clipboard", true),
        new("text.upper", "text.upper(text)", "The text in capitals", true),
        new("text", "text(value)", "Any value as text", true),
    ];

    [Theory]
    [InlineData("x = clipboard.gettext()", 8, "clipboard.gettext")]
    [InlineData("x = clipboard.gettext()", 18, "clipboard.gettext")]
    [InlineData("ShowCalculator = Ctrl+Alt+C", 3, "ShowCalculator")]
    [InlineData("ShowCalculator = Ctrl+Alt+C", 14, "")]
    [InlineData("end.", 1, "end")]
    [InlineData("ab", 2, "")]
    public void WordAt_IsTheNameThere_DotsIncluded(string text, int position, string expected)
    {
        // Act / Assert
        Assert.Equal(expected, HoverHelp.WordAt(text, position));
    }

    [Fact]
    public void Script_AFunction_IsItsSignatureAndWhatItDoes()
    {
        // Act / Assert
        Assert.Equal(
            "clipboard.settext(text)" + Environment.NewLine + "Puts text in the clipboard",
            HoverHelp.Script("Clipboard.SetText", _functions));
        Assert.Equal(
            "text(value)" + Environment.NewLine + "Any value as text",
            HoverHelp.Script("text", _functions));
    }

    [Fact]
    public void Script_AModule_IsItsFunctions_AndAVariableIsNothing()
    {
        // Act / Assert
        Assert.Equal("clipboard: gettext, settext", HoverHelp.Script("clipboard", _functions));
        Assert.Null(HoverHelp.Script("total", _functions));
    }

    [Fact]
    public void NamedAndSetting_AreTheNameAndWhatItIs()
    {
        // Arrange
        IReadOnlyList<CodeSuggestionModel> actions = [new("ShowCalculator", "Shows the calculator")];
        IReadOnlyList<ConfigSettingModel> settings = [new("Gap", "Pixels between zones", [])];

        // Act / Assert
        Assert.Equal("ShowCalculator: Shows the calculator", HoverHelp.Named("showcalculator", actions));
        Assert.Equal("Gap: Pixels between zones", HoverHelp.Setting("Gap", settings));
        Assert.Null(HoverHelp.Named("Unknown", actions));
    }
}
