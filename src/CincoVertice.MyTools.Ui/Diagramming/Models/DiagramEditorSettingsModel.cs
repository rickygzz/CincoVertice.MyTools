namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     What the diagram editor remembers between windows and runs. Nothing about any one diagram.
/// </summary>
/// <param name="Style">How new shapes and connectors look: what was used last.</param>
/// <param name="Routing">How new connectors go between shapes.</param>
/// <param name="Heads">Which ends of new connectors have a head.</param>
/// <param name="IsPropertiesPanelVisible">True when the properties panel is shown.</param>
/// <param name="Window">How large the window was; null when it isn't known yet.</param>
public sealed record DiagramEditorSettingsModel(
    DiagramStyleModel Style,
    DiagramRouting Routing,
    DiagramArrowHeads Heads,
    bool IsPropertiesPanelVisible = true,
    DiagramWindowSizeModel? Window = null);
