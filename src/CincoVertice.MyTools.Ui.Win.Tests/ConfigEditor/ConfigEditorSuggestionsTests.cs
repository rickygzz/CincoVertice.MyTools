using System.Drawing;
using CincoVertice.Common.Application.Scripting;
using CincoVertice.Common.Application.Scripting.Functions;
using CincoVertice.Common.Application.Scripting.Services;
using CincoVertice.Common.WinApi.Hotkey;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;
using CincoVertice.MyTools.Ui.Win.Scripting.Actions;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Setup;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;
using CincoVertice.MyTools.Ui.Win.Zones.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.ConfigEditor;

/// <summary>The lists the suggestions come from stay those of what the app really reads.</summary>
public class ConfigEditorSuggestionsTests
{
    [Fact]
    public void ScriptFunctionHelp_HasEveryFunction_AndOnlyThose()
    {
        // Arrange
        IReadOnlyDictionary<string, ScriptFunction> functions =
            BuiltInFunctions.Create(null!, new MemoryVariableStore(), string.Empty, _ => Task.CompletedTask);

        // Act
        IEnumerable<string> described = ScriptFunctionHelp.Functions.Select(function => function.Name);

        // Assert
        Assert.Equal(functions.Keys.Order(StringComparer.Ordinal), described.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ScriptFunctionHelp_Keywords_AreTheLanguagesKeywords()
    {
        // Act / Assert
        Assert.All(ScriptFunctionHelp.Keywords, keyword => Assert.True(ScriptLexer.IsKeyword(keyword), keyword));
    }

    [Fact]
    public void ShortcutKeyNames_AreKeysOfAShortcut()
    {
        // Act / Assert
        Assert.All(
            ShortcutKeyNames.Keys,
            key => Assert.True(HotkeyParser.TryParse("Ctrl+Alt+" + key, out _, out _, out string error), error));
    }

    [Fact]
    public void ActionHelp_DescribesEveryBuiltInAction()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddMyToolsUiWinLayer();
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        string[] missing =
        [
            .. provider.GetRequiredService<AppActionCatalog>().GetActions()
                .Where(action => action is not ScriptAction && ActionHelp.DescriptionOf(action.Name) is null)
                .Select(action => action.Name),
        ];

        // Assert
        Assert.Empty(missing);
    }

    [Fact]
    public void ConfigSettingsHelp_NamesEverySettingOfANewFile()
    {
        // Act / Assert
        Assert.Empty(NamesNotIn(ActivitySettingsService.CreateDefaultText(), ConfigSettingsHelp.Activity));
        Assert.Empty(NamesNotIn(RecordingSettingsService.CreateDefaultText(), ConfigSettingsHelp.Recording));
        Assert.Empty(NamesNotIn(ZonesConfigService.CreateDefaultText(), ConfigSettingsHelp.Zones([])));
    }

    [Fact]
    public void ConfigSettingsHelp_OffersOnlyValuesTheReadersTake()
    {
        // Arrange
        IReadOnlyList<ConfigSettingModel> zones = ConfigSettingsHelp.Zones([new Rectangle(-1920, 0, 1920, 1080)]);
        IReadOnlyList<ConfigSettingModel> startup = ConfigSettingsHelp.Startup([new("ShowCalculator", "")], 3);

        // Act
        string[] problems =
        [
            .. Lines(ConfigSettingsHelp.Activity)
                .SelectMany(line => Messages(ActivitySettingsReader.Read(line).LineProblems)),
            .. Lines(ConfigSettingsHelp.Recording)
                .SelectMany(line => Messages(RecordingSettingsReader.Read(line).LineProblems)),
            .. Lines(zones).SelectMany(line => Messages(ZonesConfigReader.Read(line).LineProblems)),
            .. Lines(startup).SelectMany(line =>
                Messages(StartupConfigReader.Read(line, ["ShowCalculator"]).LineProblems)),
        ];

        // Assert
        Assert.Empty(problems);
        Assert.Equal("-1920, 0, 1920, 1080", zones.Single(setting => setting.Name == "Monitor").Values[0].Text);
    }

    // The names set by the lines of a text, comments left out, that the settings don't have
    private static IEnumerable<string> NamesNotIn(string text, IReadOnlyList<ConfigSettingModel> settings) =>
        text.Split('\n')
            .Where(line => line.Contains('=') && !line.TrimStart().StartsWith('#'))
            .Select(line => line[..line.IndexOf('=')].Trim())
            .Where(name => !settings.Any(setting => setting.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

    // A line for each value offered
    private static IEnumerable<string> Lines(IReadOnlyList<ConfigSettingModel> settings) =>
        settings.SelectMany(setting => setting.Values.Select(value => $"{setting.Name} = {value.Text}"));

    private static IEnumerable<string> Messages(IEnumerable<ConfigLineProblemModel> problems) =>
        problems.Select(problem => problem.Message);

    [Fact]
    public void Language_SuggestsActions_InHotkeys_AndFunctions_InScripts()
    {
        // Arrange
        var language = new EditableFileLanguage(() => ["ShowCalculator", "MyScript"]);

        // Act
        CodeSuggestionsModel? actions = language.Suggest(EditableFileKind.Hotkeys, "Sh", 2, SuggestionTrigger.Typing);
        CodeSuggestionsModel? scripts = language.Suggest(EditableFileKind.Hotkeys, "My", 2, SuggestionTrigger.Typing);
        CodeSuggestionsModel? functions =
            language.Suggest(EditableFileKind.Script, "clipboard.paste", 15, SuggestionTrigger.Typing);
        CodeSuggestionsModel? activity =
            language.Suggest(EditableFileKind.ActivitySettings, "Sp", 2, SuggestionTrigger.Typing);
        CodeSuggestionsModel? newLine = language.Suggest(EditableFileKind.Hotkeys, "", 0, SuggestionTrigger.NewLine);
        CodeSuggestionsModel? scriptLine = language.Suggest(EditableFileKind.Script, "", 0, SuggestionTrigger.NewLine);

        // Assert
        Assert.Equal("Shows the calculator", Assert.Single(actions!.Items).Detail);
        Assert.Equal("Runs the script MyScript", Assert.Single(scripts!.Items).Detail);
        Assert.Equal(["paste", "pasteastext"], functions!.Items.Select(item => item.Text));
        Assert.Equal(["SpeedMin", "SpeedMax"], activity!.Items.Select(item => item.Text));
        Assert.Equal(2, newLine!.Items.Count);
        Assert.Null(scriptLine);
    }
}
