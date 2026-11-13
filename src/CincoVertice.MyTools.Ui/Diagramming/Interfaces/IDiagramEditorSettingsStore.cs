using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Interfaces;

/// <summary>
///     Where the diagram editor keeps its settings.
/// </summary>
public interface IDiagramEditorSettingsStore
{
    /// <returns>The settings saved last; null when there are none that can be used.</returns>
    DiagramEditorSettingsModel? Load();

    void Save(DiagramEditorSettingsModel settings);
}
