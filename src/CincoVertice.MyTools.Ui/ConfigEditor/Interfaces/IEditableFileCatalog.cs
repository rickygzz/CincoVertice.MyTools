using CincoVertice.MyTools.Ui.ConfigEditor.Models;

namespace CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;

/// <summary>The files that can be edited.</summary>
public interface IEditableFileCatalog
{
    /// <summary>The files as they are now, creating the ones that should exist.</summary>
    IReadOnlyList<EditableFileModel> GetFiles();

    /// <summary>Creates a new script.</summary>
    /// <param name="name">Its name, with or without the extension.</param>
    /// <param name="file">The new file, or null when it fails.</param>
    /// <param name="error">Why it failed: a bad name, or one that's taken.</param>
    bool TryCreateScript(string name, out EditableFileModel? file, out string error);
}
