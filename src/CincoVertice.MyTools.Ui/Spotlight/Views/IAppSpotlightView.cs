using System.Drawing;

namespace CincoVertice.MyTools.Ui.Spotlight.Views;

/// <summary>
///     Dims every monitor except the window the user works in, framed. It never takes the mouse or the focus.
/// </summary>
public interface IAppSpotlightView
{
    /// <summary>Raised about 60 times a second while the spotlight shows.</summary>
    event EventHandler? Tick;

    /// <summary>
    ///     The window the user works in as they see it, in physical screen pixels; null when there's none, as on the
    ///     desktop.
    /// </summary>
    Rectangle? ActiveWindowBounds { get; }

    /// <summary>Shows, or moves, the frame.</summary>
    /// <param name="window">The window, in physical screen pixels.</param>
    /// <param name="margin">How far outside it the frame is, in pixels at 100% scale; the view scales it.</param>
    void ShowSpotlight(Rectangle window, int margin);

    void HideSpotlight();
}
