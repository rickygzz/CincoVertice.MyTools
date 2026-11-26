using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Interfaces;

/// <summary>Where the JSON editor's settings are kept: config\jsoneditor.json in the app.</summary>
public interface IJsonEditorSettingsStore
{
    /// <returns>The settings; null when there are none yet, or they can't be read.</returns>
    JsonEditorSettingsModel? Load();

    void Save(JsonEditorSettingsModel settings);
}
