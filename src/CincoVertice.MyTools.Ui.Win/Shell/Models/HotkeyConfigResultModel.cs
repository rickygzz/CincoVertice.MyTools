namespace CincoVertice.MyTools.Ui.Win.Shell.Models;

/// <summary>
///     What hotkeys.cfg asks for: the valid bindings, and a problem per line that was skipped.
/// </summary>
public sealed class HotkeyConfigResultModel
{
    public List<HotkeyBindingModel> Bindings { get; } = [];

    public List<string> Problems { get; } = [];

    /// <summary>The problems found in the file's text, with their lines.</summary>
    public List<ConfigLineProblemModel> LineProblems { get; } = [];

    /// <param name="line">Its line.</param>
    /// <param name="message">What's wrong.</param>
    /// <param name="prefix">Goes before the message only in <see cref="Problems"/>, which has no line next to it.</param>
    public void AddProblem(int line, string message, string prefix = "")
    {
        Problems.Add(prefix + message);
        LineProblems.Add(new ConfigLineProblemModel(line, message));
    }
}
