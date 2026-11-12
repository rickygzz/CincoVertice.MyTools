namespace CincoVertice.MyTools.Ui.Win.Shell.Models;

/// <summary>
///     What startup.cfg asks for: the actions to run, in order, and a problem per line that was skipped.
/// </summary>
public sealed class StartupConfigResultModel
{
    public List<StartupActionModel> Actions { get; } = [];

    public List<string> Problems { get; } = [];

    /// <summary>The problems found in the file's text, with their lines.</summary>
    public List<ConfigLineProblemModel> LineProblems { get; } = [];

    /// <param name="line">Its line.</param>
    /// <param name="message">What's wrong.</param>
    public void AddProblem(int line, string message)
    {
        Problems.Add($"Line {line}: {message}");
        LineProblems.Add(new ConfigLineProblemModel(line, message));
    }
}
