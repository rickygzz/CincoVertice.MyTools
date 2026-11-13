using CincoVertice.MyTools.Ui.Snipping.Models;

namespace CincoVertice.MyTools.Ui.Snipping.Interfaces;

/// <summary>Where a recording is written while it's made, and where it goes once the user names it.</summary>
public interface IRecordingFileStore
{
    /// <returns>A new MP4 file to record to, until it's kept or discarded.</returns>
    string CreateTemporaryPath();

    /// <summary>
    ///     Puts the recording where the user chose, replacing a file there: moved as it is, or made a GIF when the path
    ///     ends with .gif, which takes a moment. The recording is gone afterwards either way.
    /// </summary>
    /// <param name="settings">For a GIF: how many frames a second, and how wide at most.</param>
    /// <exception cref="IOException">When it can't be put there.</exception>
    /// <exception cref="InvalidOperationException">When it can't be made a GIF; the message says why.</exception>
    Task KeepAsync(string temporaryPath, string path, RecordingSettingsModel settings);

    /// <summary>Deletes the recording, if it's there.</summary>
    void Discard(string temporaryPath);
}
