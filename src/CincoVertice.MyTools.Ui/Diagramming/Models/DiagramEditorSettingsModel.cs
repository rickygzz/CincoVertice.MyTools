using System.Drawing;

namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What the diagram editor remembers between windows and runs. Nothing about any one diagram.
/// </summary>
/// <param name="Style">How new shapes and connectors look: what was used last.</param>
/// <param name="Routing">How new connectors go between shapes.</param>
/// <param name="Heads">Which ends of new connectors have a head.</param>
/// <param name="IsPropertiesPanelVisible">True when the properties panel is shown.</param>
/// <param name="Window">How large the window was; null when it isn't known yet.</param>
/// <param name="IsImagesPanelVisible">True when the images panel is shown.</param>
/// <param name="IsLayersPanelVisible">True when the layers panel is shown.</param>
/// <param name="Theme">The theme chosen last, which new diagrams get; null for the default one.</param>
/// <param name="RecentColors">Colors used last that no theme offers, newest first; null for none.</param>
public sealed record DiagramEditorSettingsModel(
    DiagramStyleModel Style,
    DiagramRouting Routing,
    DiagramArrowHeads Heads,
    bool IsPropertiesPanelVisible = true,
    DiagramWindowSizeModel? Window = null,
    bool IsImagesPanelVisible = true,
    bool IsLayersPanelVisible = false,
    string? Theme = null,
    IReadOnlyList<Color>? RecentColors = null);
