namespace CincoVertice.MyTools.Ui.Zones.Models;

/// <summary>Where a zone is, in percent of a monitor's work area: (0, 0, 100, 100) is all of it.</summary>
public sealed record ZoneModel(double Left, double Top, double Width, double Height);
