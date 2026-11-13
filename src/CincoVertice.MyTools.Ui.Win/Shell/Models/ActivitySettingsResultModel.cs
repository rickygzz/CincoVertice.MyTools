using CincoVertice.Common.WinApi.Helpers;

namespace CincoVertice.MyTools.Ui.Win.Shell.Models;

/// <summary>
///     What reading activity.cfg gave.
/// </summary>
/// <param name="Settings">The settings to use: each one that can't be used has its default.</param>
/// <param name="Problems">One per line or setting that can't be used, saying why; empty when all can.</param>
public sealed record ActivitySettingsResultModel(
    MouseMoveSettings Settings,
    IReadOnlyList<string> Problems)
{
    /// <summary>The problems found in the file's text, with their lines.</summary>
    public IReadOnlyList<ConfigLineProblemModel> LineProblems { get; init; } = [];

    /// <summary>Whether activity moves the mouse while the user is away, so they look present.</summary>
    public bool MoveMouse { get; init; } = true;

    /// <summary>Whether activity keeps the computer awake and the screen on, even with the screen locked.</summary>
    public bool KeepAwake { get; init; } = true;
}
