namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>A step of the path in the status bar's breadcrumb, which goes to its value when clicked.</summary>
/// <param name="Text">The step: a name, or [index].</param>
/// <param name="Node">The value it leads to.</param>
public sealed record JsonCrumbModel(string Text, JsonNodeModel Node);
