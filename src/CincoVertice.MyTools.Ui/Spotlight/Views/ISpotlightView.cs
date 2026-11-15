using System.Drawing;

namespace CincoVertice.MyTools.Ui.Spotlight.Views;

/// <summary>
///     Dims every monitor except a circle around the mouse pointer. It never takes the mouse or the focus.
/// </summary>
public interface ISpotlightView
{
    /// <summary>Raised about 60 times a second while the spotlight shows.</summary>
    event EventHandler? Tick;

    /// <summary>Where the mouse pointer is, in physical screen pixels.</summary>
    Point CursorPosition { get; }

    /// <summary>Shows, or moves, the circle.</summary>
    /// <param name="center">In physical screen pixels.</param>
    /// <param name="radius">In pixels at 100% scale; the view scales it for the screen.</param>
    void ShowSpotlight(Point center, int radius);

    void HideSpotlight();
}
