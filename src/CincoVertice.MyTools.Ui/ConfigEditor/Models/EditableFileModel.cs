namespace CincoVertice.MyTools.Ui.ConfigEditor.Models;

/// <summary>A file the config editor can open.</summary>
public sealed record EditableFileModel(string Path, string Name, EditableFileKind Kind);
