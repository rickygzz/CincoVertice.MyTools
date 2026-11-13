using CincoVertice.Common.Utils.Config;
using CincoVertice.MyTools.Ui.Win.Shell.Models;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

/// <summary>
///     Reads startup.cfg with the config lexer: one "Run = Action" per line, or "Run = Action, value" to pass the
///     script a value, # for comments. Every line that can't be used is reported as a problem and skipped.
/// </summary>
public static class StartupConfigReader
{
    public const string RunName = "Run";

    /// <param name="text">The content of startup.cfg.</param>
    /// <param name="actionNames">The names of the actions that can be run.</param>
    public static StartupConfigResultModel Read(string text, IReadOnlyCollection<string> actionNames)
    {
        var result = new StartupConfigResultModel();
        ConfigLexer lexer = new(text);
        ConfigItems items = lexer.Items;
        HashSet<string> runs = new(StringComparer.Ordinal);

        for (int i = 0; i < items.KeysCount; i++)
        {
            string name = items.GetKeyName(i);
            int line = items.GetLine(i);

            if (items.GetValueType(i) == ConfigTokenType.ErrorQuotedStringNotClosed)
            {
                result.AddProblem(line, $"{name}: the [\" is never closed, so the rest of the file is ignored.");

                continue;
            }

            if (!name.Equals(RunName, StringComparison.OrdinalIgnoreCase))
            {
                result.AddProblem(line, $"{name}: each line is {RunName} = Action.");

                continue;
            }

            var (actionText, argument) = SplitArgument(WithoutComment(items.GetValue(i)));

            if (actionText.Length == 0)
            {
                result.AddProblem(line, $"{RunName}: which action? {RunName} = Action.");

                continue;
            }

            string? action = actionNames.FirstOrDefault(a => a.Equals(actionText, StringComparison.OrdinalIgnoreCase));

            if (action is null)
            {
                result.AddProblem(line, $"{actionText}: unknown action. Actions: {string.Join(", ", actionNames)}.");

                continue;
            }

            if (!runs.Add(action + "\0" + argument))
            {
                string with = argument is null ? string.Empty : $" with {argument}";
                result.AddProblem(line, $"{action}{with}: runs more than once; the first line is used.");

                continue;
            }

            result.Actions.Add(new StartupActionModel(action, argument));
        }

        foreach (var (number, badLine) in lexer.BadLines)
        {
            result.AddProblem(number, $"'{badLine}' is not '{RunName} = Action'.");
        }

        return result;
    }

    /// <summary>"StartLater, slack" is the action and its value; the value is null when there is none.</summary>
    private static (string Action, string? Argument) SplitArgument(string value)
    {
        int comma = value.IndexOf(',');

        if (comma < 0)
        {
            return (value.Trim(), null);
        }

        string argument = value[(comma + 1)..].Trim();

        return (value[..comma].Trim(), argument.Length > 0 ? argument : null);
    }

    /// <summary>A # at the start of the value or after a space starts a comment, so "app#1" is a value.</summary>
    private static string WithoutComment(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '#' && (i == 0 || char.IsWhiteSpace(value[i - 1])))
            {
                return value[..i].TrimEnd();
            }
        }

        return value.Trim();
    }
}
