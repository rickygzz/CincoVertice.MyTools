using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Zones.Interfaces;

/// <summary>The zones the user set, which the zone editor also saves.</summary>
public interface IZoneLayoutStore : IZoneLayoutSource
{
    /// <summary>Saves the layouts, so the next drag uses them.</summary>
    /// <returns>Why it couldn't, for the user; null when it saved.</returns>
    string? Save(ZoneLayoutsModel layouts);
}
