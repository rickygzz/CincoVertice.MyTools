using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Shell;

public class HotkeyConfigReaderTests
{
    private static readonly string[] _actions = ["ShowCalculator", "ToggleActivity", "CheckFolder"];

    [Fact]
    public void Read_ReturnsBindingForEachLine()
    {
        // Arrange
        string text = "# Hotkeys\nShowCalculator = Ctrl+Alt+A\n\ntoggleactivity = ctrl+alt+m\n";

        // Act
        HotkeyConfigResultModel result = HotkeyConfigReader.Read(text, _actions);

        // Assert
        const FSModifiers ctrlAlt = FSModifiers.MOD_CONTROL | FSModifiers.MOD_ALT;
        Assert.Empty(result.Problems);
        Assert.Equal(
            [
                new HotkeyBindingModel("ShowCalculator", "Ctrl+Alt+A", ctrlAlt, KeyCode.A),
                new HotkeyBindingModel("ToggleActivity", "ctrl+alt+m", ctrlAlt, KeyCode.M),
            ],
            result.Bindings);
    }

    [Fact]
    public void Read_AcceptsCrlfAndCommentAfterShortcut()
    {
        // Act
        HotkeyConfigResultModel result = HotkeyConfigReader.Read("CheckFolder = Shift+F5   # check\r\n", _actions);

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(KeyCode.VK_F5, Assert.Single(result.Bindings).Key);
    }

    [Fact]
    public void Read_KeepsCharacterKeyForTheParser()
    {
        // Act: the key depends on this machine's keyboard layout, but every layout has a key for "<"
        HotkeyConfigResultModel result = HotkeyConfigReader.Read("CheckFolder = Ctrl+Alt+<\n", _actions);

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal("Ctrl+Alt+<", Assert.Single(result.Bindings).Shortcut);
    }

    [Theory]
    [InlineData("ShowCalculator =\nToggleActivity = Ctrl+Alt+M")]
    [InlineData("ShowCalculator =   \r\nToggleActivity = Ctrl+Alt+M\r\n")]
    [InlineData("ShowCalculator = # none\nToggleActivity = Ctrl+Alt+M")]
    public void Read_EmptyBinding_KeepsTheNextLine(string text)
    {
        // Act
        HotkeyConfigResultModel result = HotkeyConfigReader.Read(text, _actions);

        // Assert
        Assert.Equal("ToggleActivity", Assert.Single(result.Bindings).ActionName);
        Assert.StartsWith("ShowCalculator: No shortcut", Assert.Single(result.Problems));
    }

    [Theory]
    [InlineData("123Search")]
    [InlineData("_private")]
    public void Read_BindsActionsStartingWithADigitOrUnderscore(string action)
    {
        // Act
        HotkeyConfigResultModel result = HotkeyConfigReader.Read($"{action} = Ctrl+Alt+1", [action]);

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(action, Assert.Single(result.Bindings).ActionName);
    }

    [Fact]
    public void Read_ArgumentsThatDifferOnlyInCase_AreDifferentBindings()
    {
        // Act
        HotkeyConfigResultModel result = HotkeyConfigReader.Read(
            "CheckFolder = Ctrl+Alt+1, A\ncheckfolder = Ctrl+Alt+2, a",
            _actions);

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(["A", "a"], result.Bindings.Select(binding => binding.Argument));
    }

    [Fact]
    public void Read_SameActionAndArgumentInAnotherCaseOfTheAction_IsADuplicate()
    {
        // Act
        HotkeyConfigResultModel result = HotkeyConfigReader.Read(
            "CheckFolder = Ctrl+Alt+1, a\ncheckfolder = Ctrl+Alt+2, a",
            _actions);

        // Assert
        Assert.Single(result.Bindings);
        Assert.Contains("set more than once", Assert.Single(result.Problems));
    }

    [Theory]
    [InlineData("CheckFolder = Ctrl+Alt+#", "Ctrl+Alt+#", null)]
    [InlineData("CheckFolder = Ctrl+Alt+# # note", "Ctrl+Alt+#", null)]
    [InlineData("CheckFolder = Ctrl + Alt + # # note", "Ctrl + Alt + #", null)]
    [InlineData("CheckFolder = Ctrl+Alt+#, app1", "Ctrl+Alt+#", "app1")]
    [InlineData("CheckFolder = Ctrl+Alt+1, app#1", "Ctrl+Alt+1", "app#1")]
    [InlineData("CheckFolder = Ctrl+Alt+1, app#1 # note", "Ctrl+Alt+1", "app#1")]
    [InlineData("CheckFolder = Ctrl+Alt+1 #note", "Ctrl+Alt+1", null)]
    public void Read_HashIsACommentOnlyAfterASpaceAndNotAsTheKey(string text, string shortcut, string? argument)
    {
        // Act: # is Shift+3 here, as on a US keyboard; on others it can need AltGr, which can't be a hotkey
        HotkeyConfigResultModel result = HotkeyConfigReader.Read(
            text,
            _actions,
            c => c == '#' ? (short)0x133 : (short)-1);

        // Assert
        Assert.Empty(result.Problems);
        HotkeyBindingModel binding = Assert.Single(result.Bindings);
        Assert.Equal((shortcut, argument), (binding.Shortcut, binding.Argument));
    }

    [Fact]
    public void Read_HashKeyThatNeedsAltGr_SaysSo()
    {
        // Act: Ctrl and Alt in the shift state, as on a keyboard that types # with AltGr
        HotkeyConfigResultModel result = HotkeyConfigReader.Read(
            "CheckFolder = Ctrl+Alt+#",
            _actions,
            c => c == '#' ? (short)0x633 : (short)-1);

        // Assert
        Assert.Empty(result.Bindings);
        Assert.Contains("'#' is typed with AltGr", Assert.Single(result.Problems));
    }

    [Theory]
    [InlineData("Unknown = Ctrl+Alt+A", "Unknown: unknown action")]
    [InlineData("ShowCalculator = Ctrl+Alt+Foo", "ShowCalculator: 'Foo' is not a key")]
    [InlineData("ShowCalculator =", "ShowCalculator: No shortcut")]
    [InlineData("Ctrl+Alt+A = ShowCalculator", "Line 1: 'Ctrl+Alt+A = ShowCalculator'")]
    [InlineData("ShowCalculator Ctrl+Alt+A", "Line 1: 'ShowCalculator Ctrl+Alt+A'")]
    public void Read_ReportsLineThatCantBeUsed(string text, string expectedProblemStart)
    {
        // Act
        HotkeyConfigResultModel result = HotkeyConfigReader.Read(text, _actions);

        // Assert
        Assert.Empty(result.Bindings);
        Assert.StartsWith(expectedProblemStart, Assert.Single(result.Problems));
    }

    [Theory]
    [InlineData("CheckFolder = Ctrl+Alt+1, app1", "Ctrl+Alt+1", KeyCode.D1, "app1")]
    [InlineData("CheckFolder = Ctrl+Alt+1 ,  my app  # comment", "Ctrl+Alt+1", KeyCode.D1, "my app")]
    [InlineData("CheckFolder = Ctrl+Alt+1,", "Ctrl+Alt+1", KeyCode.D1, null)]
    [InlineData("CheckFolder = Ctrl+Alt+,", "Ctrl+Alt+,", KeyCode.VK_OEM_COMMA, null)]
    [InlineData("CheckFolder = Ctrl+Alt+,, app1", "Ctrl+Alt+,", KeyCode.VK_OEM_COMMA, "app1")]
    public void Read_SplitsTheArgumentAfterTheShortcut(string text, string shortcut, KeyCode key, string? argument)
    {
        // Act
        HotkeyConfigResultModel result = HotkeyConfigReader.Read(text, _actions);

        // Assert
        Assert.Empty(result.Problems);
        var binding = Assert.Single(result.Bindings);
        Assert.Equal((shortcut, key, argument), (binding.Shortcut, binding.Key, binding.Argument));
    }

    [Fact]
    public void Read_AllowsAnActionTwiceWithDifferentArguments()
    {
        // Act
        HotkeyConfigResultModel result = HotkeyConfigReader.Read(
            "CheckFolder = Ctrl+Alt+1, app1\nCheckFolder = Ctrl+Alt+2, app2\nCheckFolder = Ctrl+Alt+3, app1",
            _actions);

        // Assert
        Assert.Equal(["app1", "app2"], result.Bindings.Select(b => b.Argument));
        Assert.Equal(
            "CheckFolder with app1: set more than once; the first line is used.",
            Assert.Single(result.Problems));
    }

    [Fact]
    public void Read_WhenActionIsSetTwice_UsesFirstLine()
    {
        // Act
        HotkeyConfigResultModel result = HotkeyConfigReader.Read(
            "ShowCalculator = Ctrl+Alt+A\nShowCalculator = Ctrl+Alt+B",
            _actions);

        // Assert
        Assert.Equal(KeyCode.A, Assert.Single(result.Bindings).Key);
        Assert.Contains("more than once", Assert.Single(result.Problems));
    }

    [Fact]
    public void Read_WhenShortcutIsUsedTwice_KeepsFirstAction()
    {
        // Act
        HotkeyConfigResultModel result = HotkeyConfigReader.Read(
            "ShowCalculator = Ctrl+Alt+A\nToggleActivity = Alt+Ctrl+A",
            _actions);

        // Assert
        Assert.Equal("ShowCalculator", Assert.Single(result.Bindings).ActionName);
        Assert.Equal("ToggleActivity: Alt+Ctrl+A is already used by ShowCalculator.", Assert.Single(result.Problems));
    }

    [Fact]
    public void Read_KeepsValidLinesWhenOthersHaveProblems()
    {
        // Act
        HotkeyConfigResultModel result = HotkeyConfigReader.Read(
            "Unknown = Ctrl+Alt+Z\nShowCalculator = Ctrl+Alt+A\nbroken line",
            _actions);

        // Assert
        Assert.Equal("ShowCalculator", Assert.Single(result.Bindings).ActionName);
        Assert.Equal(2, result.Problems.Count);
    }
}
