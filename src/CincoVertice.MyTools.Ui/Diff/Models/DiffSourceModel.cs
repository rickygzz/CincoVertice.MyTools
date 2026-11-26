namespace CincoVertice.MyTools.Ui.Diff.Models;

/// <summary>What one side shows: a file as read, or pasted or typed text, with the user's changes.</summary>
/// <param name="Path">The file, or null for text that isn't one.</param>
/// <param name="WrittenUtc">When the file was written, as last read or saved.</param>
/// <param name="IsModified">Whether the lines were changed since.</param>
public sealed record DiffSourceModel(
    IReadOnlyList<string> Lines,
    string? Path,
    DateTime? WrittenUtc,
    DiffTextFormatModel Format,
    bool IsModified = false,
    DateTime? DeclinedReloadUtc = null,
    string? DisplayName = null)
{
    public bool IsFile => Path is not null;

    /// <summary>A file changed and not saved; text that isn't a file is never asked to be saved.</summary>
    public bool NeedsSaving => IsModified && IsFile;
}
