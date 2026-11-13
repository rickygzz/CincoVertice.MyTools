using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Zones.Interfaces;

/// <summary>Tells when the user drags a window of any program, and where the mouse goes meanwhile.</summary>
public interface IWindowDragSource
{
    /// <summary>Raised with the window when the user starts moving or resizing it.</summary>
    event EventHandler<nint>? DragStarted;

    /// <summary>Raised now and then while the window is dragged.</summary>
    event EventHandler<ZoneDragPointerModel>? DragMoved;

    /// <summary>Raised when the user lets go of the window.</summary>
    event EventHandler? DragEnded;
}
