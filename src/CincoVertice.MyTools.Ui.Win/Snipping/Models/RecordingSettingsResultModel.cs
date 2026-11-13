using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Models;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Models;

/// <summary>What reading recording.cfg gave.</summary>
/// <param name="Settings">The settings to use: each one that can't be used has its default.</param>
/// <param name="Problems">One per line or setting that can't be used, saying why; empty when all can.</param>
/// <param name="LineProblems">The same, with their lines, for the config editor.</param>
public sealed record RecordingSettingsResultModel(
    RecordingSettingsModel Settings,
    IReadOnlyList<string> Problems,
    IReadOnlyList<ConfigLineProblemModel> LineProblems);
