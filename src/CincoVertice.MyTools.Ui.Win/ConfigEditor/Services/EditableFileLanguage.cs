using CincoVertice.Common.Application.Scripting;
using CincoVertice.Common.Utils.Config;
using CincoVertice.Common.Utils.Highlighting;
using CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.ConfigEditor.Services;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;
using CincoVertice.MyTools.Ui.Win.Zones.Services;

namespace CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;

/// <summary>
///     Colors and checks each kind of file with what reads it for real: the problems are the ones the app would report.
/// </summary>
public sealed class EditableFileLanguage : IEditableFileLanguage
{
    private static readonly ICodeHighlighter _scriptHighlighter = new ScriptHighlighter();
    private static readonly ICodeHighlighter _configHighlighter = new ConfigHighlighter();

    private readonly Func<IReadOnlyCollection<string>> _actionNames;
    private readonly Func<int> _zoneCount;
    private readonly Func<IReadOnlyList<string>> _libraries;
    private readonly Func<IReadOnlyList<Rectangle>> _monitors;

    /// <param name="actionNames">The names hotkeys.cfg can bind now, which change as scripts come and go.</param>
    /// <param name="zoneCount">The most zones a monitor has now, for MoveToZone's value; 0 for none.</param>
    /// <param name="libraries">The scripts made to be included (_*.mts), by name.</param>
    /// <param name="monitors">The monitors connected now, in physical pixels, for zones.cfg's Monitor lines.</param>
    public EditableFileLanguage(
        Func<IReadOnlyCollection<string>> actionNames,
        Func<int>? zoneCount = null,
        Func<IReadOnlyList<string>>? libraries = null,
        Func<IReadOnlyList<Rectangle>>? monitors = null)
    {
        _actionNames = actionNames;
        _zoneCount = zoneCount ?? (() => 0);
        _libraries = libraries ?? (() => []);
        _monitors = monitors ?? (() => []);
    }

    public IReadOnlyList<CodeSpan> Highlight(EditableFileKind kind, string text) =>
        (kind == EditableFileKind.Script ? _scriptHighlighter : _configHighlighter).Find(text);

    public IReadOnlyList<EditableFileProblemModel> Check(EditableFileKind kind, string text) => kind switch
    {
        EditableFileKind.Hotkeys => InLineOrder(HotkeyConfigReader.Read(text, _actionNames()).LineProblems),
        EditableFileKind.ActivitySettings => InLineOrder(ActivitySettingsReader.Read(text).LineProblems),
        EditableFileKind.StartupActions => InLineOrder(StartupConfigReader.Read(text, _actionNames()).LineProblems),
        EditableFileKind.WindowZones => InLineOrder(ZonesConfigReader.Read(text).LineProblems),
        EditableFileKind.RecordingSettings => InLineOrder(RecordingSettingsReader.Read(text).LineProblems),
        _ => ScriptEngine.FindSyntaxError(text) is ScriptException error
            ? [new EditableFileProblemModel(error.Line, error.Message, error.Column)]
            : [],
    };

    public CodeSuggestionsModel? Suggest(EditableFileKind kind, string text, int caret, SuggestionTrigger trigger)
    {
        return kind switch
        {
            EditableFileKind.Hotkeys => HotkeysSuggester.Suggest(
                text,
                caret,
                isExplicit: trigger != SuggestionTrigger.Typing,
                Actions(),
                ShortcutKeyNames.Keys,
                _zoneCount()),
            EditableFileKind.Script => ScriptSuggester.Suggest(
                text,
                caret,
                isExplicit: trigger == SuggestionTrigger.Asked,
                ScriptFunctionHelp.Functions,
                ScriptFunctionHelp.Keywords,
                _libraries(),
                ShortcutKeyNames.Keys),
            EditableFileKind.ActivitySettings =>
                ConfigSuggester.Suggest(text, caret, trigger, ConfigSettingsHelp.Activity),
            EditableFileKind.RecordingSettings =>
                ConfigSuggester.Suggest(text, caret, trigger, ConfigSettingsHelp.Recording),
            EditableFileKind.StartupActions =>
                ConfigSuggester.Suggest(text, caret, trigger, ConfigSettingsHelp.Startup(Actions(), _zoneCount())),
            EditableFileKind.WindowZones =>
                ConfigSuggester.Suggest(text, caret, trigger, ConfigSettingsHelp.Zones(_monitors())),
            _ => null,
        };
    }

    // Sorted by name, each with what it does
    private List<CodeSuggestionModel> Actions() =>
        [.. _actionNames().Order(StringComparer.OrdinalIgnoreCase).Select(Describe)];

    private static CodeSuggestionModel Describe(string action) =>
        new(action, ActionHelp.DescriptionOf(action) ?? $"Runs the script {action}");

    private static List<EditableFileProblemModel> InLineOrder(IEnumerable<ConfigLineProblemModel> problems) =>
        [.. problems
            .OrderBy(problem => problem.Line)
            .Select(problem => new EditableFileProblemModel(problem.Line, problem.Message))];
}
