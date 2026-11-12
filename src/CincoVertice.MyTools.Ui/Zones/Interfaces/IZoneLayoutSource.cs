using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Zones.Interfaces;

/// <summary>The zones the user set, read again each time, so a change applies at once.</summary>
public interface IZoneLayoutSource
{
    /// <returns>The layout; the default one when it can't be read.</returns>
    ZoneLayoutModel Load();
}
