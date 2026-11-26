namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>What the JSON editor keeps between runs.</summary>
/// <param name="IsDark">Dark or light, as chosen with its button; null to follow Windows' mode.</param>
public sealed record JsonEditorSettingsModel(bool? IsDark);
