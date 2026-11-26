using CincoVertice.MyTools.Ui.JsonEditor.Interfaces;
using CincoVertice.MyTools.Ui.JsonEditor.Models;

namespace CincoVertice.MyTools.Ui.Tests.JsonEditor;

internal sealed class FakeJsonSettings : IJsonEditorSettingsStore
{
    public JsonEditorSettingsModel? Settings { get; set; }

    public JsonEditorSettingsModel? Load() => Settings;

    public void Save(JsonEditorSettingsModel settings) => Settings = settings;
}
