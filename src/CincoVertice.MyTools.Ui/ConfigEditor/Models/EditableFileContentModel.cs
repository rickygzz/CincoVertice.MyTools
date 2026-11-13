namespace CincoVertice.MyTools.Ui.ConfigEditor.Models;

/// <summary>What's in a file, and how to write it back the same way.</summary>
/// <param name="Text">The text, with \n for every new line, as the editor has it.</param>
/// <param name="NewLine">The new line the file uses.</param>
/// <param name="HasBom">Whether the file starts with a byte order mark.</param>
public sealed record EditableFileContentModel(string Text, string NewLine, bool HasBom);
