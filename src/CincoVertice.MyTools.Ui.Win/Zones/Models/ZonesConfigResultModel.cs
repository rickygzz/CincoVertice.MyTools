using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Win.Zones.Models;

/// <summary>What zones.cfg asks for: the layouts, and a problem per line that was skipped.</summary>
public sealed class ZonesConfigResultModel
{
    public ZoneLayoutsModel Layouts { get; set; } = ZoneLayoutsModel.Default;

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
