namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>A file as read, and when it was last written, to tell when it changes.</summary>
public sealed record DiffFileModel(string Text, DateTime WrittenUtc);
