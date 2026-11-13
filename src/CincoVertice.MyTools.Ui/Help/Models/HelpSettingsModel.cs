using System.Drawing;

namespace CincoVertice.MyTools.Ui.Help.Models;

/// <summary>What the help window keeps for the next time it opens.</summary>
/// <param name="Page">The page it showed last; null for none.</param>
/// <param name="Bounds">Where it was on the screen, as it is when not maximized; null for the usual place.</param>
/// <param name="IsMaximized">Whether it was maximized.</param>
public sealed record HelpSettingsModel(string? Page, Rectangle? Bounds, bool IsMaximized);
