namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     An image of the catalog, the folder of SVG images the diagram editor offers to insert.
/// </summary>
/// <param name="Path">Where it is in the catalog, with '/' between folders, e.g. "Azure/Compute/vm.svg".</param>
/// <param name="Name">What the panel shows: the file's name, made easier to read and to search.</param>
/// <param name="Group">The folder it's in, e.g. "Azure/Compute"; inserted images are in "Inserted".</param>
/// <param name="Markup">The image, as <see cref="Services.DiagramSvgImage.Prepare"/> gives it.</param>
public sealed record DiagramCatalogImageModel(string Path, string Name, string Group, string Markup);
