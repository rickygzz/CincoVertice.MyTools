using System.Drawing;

namespace CincoVertice.MyTools.Ui.Zones.Views;

/// <summary>
///     Shows the zones of every monitor over everything while a window is dragged, without taking the mouse.
/// </summary>
public interface IZoneOverlayView
{
    /// <summary>Shows the zones, numbered from 1, or shows another one under the mouse.</summary>
    /// <param name="zones">In screen pixels.</param>
    /// <param name="highlighted">The index of the zone the window would go to; null for none.</param>
    void ShowZones(IReadOnlyList<Rectangle> zones, int? highlighted);

    void HideZones();

    /// <summary>Tells the user why a window wasn't moved, without waiting for them.</summary>
    void ShowProblem(string message);
}
