using CincoVertice.Common.WinApi.Helpers;

namespace CincoVertice.MyTools.Ui.Win.Shell.Models;

/// <summary>
///     What reading activity.cfg gave.
/// </summary>
/// <param name="Settings">The settings to use: each one that can't be used has its default.</param>
/// <param name="Problems">One per line or setting that can't be used, saying why; empty when all can.</param>
public sealed record ActivitySettingsResultModel(
    MouseMoveSettings Settings,
    IReadOnlyList<string> Problems);
