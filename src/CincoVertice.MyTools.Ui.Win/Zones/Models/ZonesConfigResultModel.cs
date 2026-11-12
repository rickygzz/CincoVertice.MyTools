using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Win.Zones.Models;

/// <summary>What zones.cfg asks for: the layout, and a problem per line that was skipped.</summary>
public sealed class ZonesConfigResultModel
{
    public ZoneLayoutModel Layout { get; set; } = ZoneLayoutModel.Default;

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
