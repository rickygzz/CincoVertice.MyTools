using CincoVertice.MyTools.Ui.Help.Models;

namespace CincoVertice.MyTools.Ui.Help.Interfaces;

/// <summary>Where the help window keeps its settings.</summary>
public interface IHelpSettingsStore
{
    /// <returns>The settings saved last; null when there are none that can be used.</returns>
    HelpSettingsModel? Load();

    void Save(HelpSettingsModel settings);
}
