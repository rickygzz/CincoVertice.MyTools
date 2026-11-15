namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>What one side shows: a file as read, or pasted text.</summary>
/// <param name="Path">The file, or null for pasted text.</param>
/// <param name="WrittenUtc">When the file was written, as it was read.</param>
public sealed record DiffSourceModel(string Text, string? Path, DateTime? WrittenUtc)
{
    public bool IsFile => Path is not null;
}
