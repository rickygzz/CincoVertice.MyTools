using CincoVertice.MyTools.Ui.Snipping.Interfaces;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>Records to a file of its own in the temporary folder, then moves it where the user chose.</summary>
public sealed class RecordingFileStore : IRecordingFileStore
{
    public string CreateTemporaryPath() =>
        Path.Combine(Path.GetTempPath(), $"MyTools recording {Guid.NewGuid():N}.mp4");

    public void Keep(string temporaryPath, string path) => File.Move(temporaryPath, path, overwrite: true);

    public void Discard(string temporaryPath)
    {
        try
        {
            File.Delete(temporaryPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Left in the temporary folder, which Windows cleans up
        }
    }
}
