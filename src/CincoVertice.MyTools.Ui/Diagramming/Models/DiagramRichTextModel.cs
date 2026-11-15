namespace CincoVertice.MyTools.Ui.Diagramming.Models;

/// <summary>
///     A text with the parts of it in bold or italic, as it's typed.
/// </summary>
/// <param name="Text">The characters, lines ending in "\n".</param>
/// <param name="Spans">The parts in bold or italic, in order, none overlapping; none for plain text.</param>
public sealed record DiagramRichTextModel(string Text, IReadOnlyList<DiagramTextSpanModel> Spans)
{
    public static DiagramRichTextModel Plain(string text) => new(text, []);
}
