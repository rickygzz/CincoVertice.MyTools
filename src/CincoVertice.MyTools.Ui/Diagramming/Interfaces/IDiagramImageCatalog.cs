using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Diagramming.Interfaces;

/// <summary>
///     A folder of SVG images to insert again: every image inserted goes in, and so do the ones the user copies there,
///     in folders of their own. It also knows which were used last. A diagram never needs it: it keeps its own copy of
///     each image, so the folder can be emptied at any time.
/// </summary>
public interface IDiagramImageCatalog
{
    /// <summary>The folder, for the user to open and copy images into.</summary>
    string Folder { get; }

    /// <summary>Makes the folder if it isn't there, so it can be opened.</summary>
    /// <exception cref="IOException">It can't be made.</exception>
    /// <exception cref="UnauthorizedAccessException">It can't be made.</exception>
    void CreateFolder();

    /// <summary>Every image it has that can be drawn, by group, then by name.</summary>
    IReadOnlyList<DiagramCatalogImageModel> Images();

    /// <returns>The paths of the images used last, the last one first; some may be gone.</returns>
    IReadOnlyList<string> Recent();

    /// <summary>
    ///     Keeps a copy of an image inserted from elsewhere, as used last. An image it has already is only used again,
    ///     wherever it is in the folder. Nothing happens when the folder can't be written.
    /// </summary>
    /// <param name="markup">The image, as it was read.</param>
    /// <param name="fileName">The name it had, e.g. "vm.svg"; a number is added when another image has it.</param>
    void Add(string markup, string fileName);

    /// <summary>Makes the image the one used last.</summary>
    void MarkUsed(string path);
}
