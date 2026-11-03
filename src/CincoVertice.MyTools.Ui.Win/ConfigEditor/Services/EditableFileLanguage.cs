using CincoVertice.Common.Application.Scripting;
using CincoVertice.Common.Utils.Config;
using CincoVertice.Common.Utils.Highlighting;
using CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

namespace CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;

/// <summary>
///     Colors and checks each kind of file with what reads it for real: the problems are the ones the app would report.
/// </summary>
public sealed class EditableFileLanguage : IEditableFileLanguage
{
    private static readonly ICodeHighlighter _scriptHighlighter = new ScriptHighlighter();
    private static readonly ICodeHighlighter _configHighlighter = new ConfigHighlighter();

    private readonly Func<IReadOnlyCollection<string>> _actionNames;

    /// <param name="actionNames">The names hotkeys.cfg can bind now, which change as scripts come and go.</param>
    public EditableFileLanguage(Func<IReadOnlyCollection<string>> actionNames)
    {
        _actionNames = actionNames;
    }

    public IReadOnlyList<CodeSpan> Highlight(EditableFileKind kind, string text) =>
        (kind == EditableFileKind.Script ? _scriptHighlighter : _configHighlighter).Find(text);

    public IReadOnlyList<EditableFileProblemModel> Check(EditableFileKind kind, string text) => kind switch
    {
        EditableFileKind.Hotkeys => InLineOrder(HotkeyConfigReader.Read(text, _actionNames()).LineProblems),
        EditableFileKind.ActivitySettings => InLineOrder(ActivitySettingsReader.Read(text).LineProblems),
        _ => ScriptEngine.FindSyntaxError(text) is ScriptException error
            ? [new EditableFileProblemModel(error.Line, error.Message, error.Column)]
            : [],
    };

    private static List<EditableFileProblemModel> InLineOrder(IEnumerable<ConfigLineProblemModel> problems) =>
        [.. problems
            .OrderBy(problem => problem.Line)
            .Select(problem => new EditableFileProblemModel(problem.Line, problem.Message))];
}
