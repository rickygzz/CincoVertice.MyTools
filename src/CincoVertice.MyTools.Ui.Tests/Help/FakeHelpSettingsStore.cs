using CincoVertice.MyTools.Ui.Help.Interfaces;
using CincoVertice.MyTools.Ui.Help.Models;

namespace CincoVertice.MyTools.Ui.Tests.Help;

/// <summary>Settings in memory: what's saved is what's loaded next.</summary>
public sealed class FakeHelpSettingsStore : IHelpSettingsStore
{
    public HelpSettingsModel? Saved { get; set; }

    public HelpSettingsModel? Load() => Saved;

    public void Save(HelpSettingsModel settings) => Saved = settings;
}
