namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>What the JSON editor keeps between runs.</summary>
/// <param name="IsDark">Dark or light, as chosen with its button; null to follow Windows' mode.</param>
/// <param name="IsTreeShown">Whether the tree panel is shown.</param>
public sealed record JsonEditorSettingsModel(bool? IsDark, bool IsTreeShown = false);
