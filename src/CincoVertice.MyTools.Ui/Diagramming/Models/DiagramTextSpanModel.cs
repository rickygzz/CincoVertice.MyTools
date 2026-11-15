namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A part of a text written in bold or italic.
/// </summary>
/// <param name="Start">Where it starts, as an index into the text.</param>
/// <param name="Length">How many characters; more than 0.</param>
/// <param name="Format">Bold, italic or both; never none.</param>
public sealed record DiagramTextSpanModel(int Start, int Length, DiagramTextFormat Format);
