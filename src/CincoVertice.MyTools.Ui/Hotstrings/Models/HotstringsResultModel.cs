namespace CincoVertice.MyTools.Ui.Hotstrings.Models;

/// <summary>What hotstrings.cfg holds: the hotstrings to use, and a problem per line that was skipped.</summary>
public sealed record HotstringsResultModel(
    IReadOnlyList<HotstringModel> Hotstrings,
    IReadOnlyList<HotstringProblemModel> Problems);
