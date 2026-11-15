using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Interfaces;

/// <summary>
///     Where each editor window keeps a copy of its diagram while it has changes that weren't saved, so they can be
///     got back after the app or Windows ends. One for all the windows.
/// </summary>
public interface IDiagramRecoveryStore
{
    /// <summary>A new id for a window's copy, which <see cref="Left"/> never gives while the app runs.</summary>
    string NewId();

    /// <summary>Keeps the copy, instead of the one with the same id.</summary>
    /// <exception cref="IOException">It can't be written.</exception>
    /// <exception cref="UnauthorizedAccessException">It can't be written.</exception>
    void Save(DiagramRecoveryModel recovery);

    /// <summary>Forgets the copy, when the diagram was saved or its changes thrown away. Never fails.</summary>
    void Delete(string id);

    /// <summary>
    ///     The copies no window of this run of the app keeps: left when it or Windows ended. Copies that can't be read
    ///     are left out.
    /// </summary>
    IReadOnlyList<DiagramRecoveryModel> Left();

    /// <summary>Takes a copy from <see cref="Left"/> as a window's own, so it isn't offered again.</summary>
    void Adopt(string id);
}
