namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     An item of the File menu's Recent: a diagram opened or saved lately, or unsaved changes to get back.
/// </summary>
/// <param name="Text">What the menu shows, e.g. "flow.svg  —  C:\Work" or "Unsaved: Untitled, 14/11/2026 15:30".</param>
/// <param name="Path">The diagram's file; null for unsaved changes.</param>
/// <param name="RecoveryId">The copy of the unsaved changes; null for a file.</param>
public sealed record DiagramRecentModel(string Text, string? Path, string? RecoveryId);
