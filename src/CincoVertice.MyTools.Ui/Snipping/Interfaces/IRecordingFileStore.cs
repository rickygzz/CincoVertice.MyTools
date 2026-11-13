namespace CincoVertice.MyTools.Ui.Snipping.Interfaces;

/// <summary>Where a recording is written while it's made, and where it goes once the user names it.</summary>
public interface IRecordingFileStore
{
    /// <returns>A new file to record to, until it's kept or discarded.</returns>
    string CreateTemporaryPath();

    /// <summary>Moves the recording to where the user chose, replacing a file there.</summary>
    /// <exception cref="IOException">When it can't be moved there.</exception>
    void Keep(string temporaryPath, string path);

    /// <summary>Deletes the recording, if it's there.</summary>
    void Discard(string temporaryPath);
}
