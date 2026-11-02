using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Snipping.Interfaces;

/// <summary>
///     Keeps the snip editor's settings, so the next editor starts with them, even after a restart.
/// </summary>
public interface ISnipEditorSettingsStore
{
    /// <summary>The settings saved last; null when there are none, or they can't be read.</summary>
    SnipEditorSettingsModel? Load();

    /// <summary>Saves the settings. A failure only loses them: it never stops the editor.</summary>
    void Save(SnipEditorSettingsModel settings);
}
