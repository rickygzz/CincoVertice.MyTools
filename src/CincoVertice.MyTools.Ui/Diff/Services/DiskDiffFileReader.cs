using System.Text;
using CincoVertice.MyTools.Ui.Diff.Interfaces;
using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.Diff.Services;

/// <summary>
///     Reads files from disk: as their byte order mark says, else as UTF-8, else, when that isn't valid UTF-8, as
///     Latin-1, so an older file's accents still show and compare.
/// </summary>
public sealed class DiskDiffFileReader : IDiffFileReader
{
    /// <summary>Larger files aren't read: comparing them would take too long.</summary>
    public const long MaxBytes = 64L * 1024 * 1024;

    private static readonly UTF8Encoding _strictUtf8 = new(false, true);

    public DiffFileModel Read(string path)
    {
        if (Directory.Exists(path))
        {
            throw new IOException("It's a folder: choose a file.");
        }

        var info = new FileInfo(path);

        if (info.Exists && info.Length > MaxBytes)
        {
            throw new IOException($"It's larger than {MaxBytes / 1024 / 1024} MB.");
        }

        byte[] bytes = File.ReadAllBytes(path);

        return new DiffFileModel(Decode(bytes), File.GetLastWriteTimeUtc(path));
    }

    public DateTime? WrittenUtc(string path) => File.Exists(path) ? File.GetLastWriteTimeUtc(path) : null;

    public static string Decode(byte[] bytes)
    {
        try
        {
            using var reader = new StreamReader(
                new MemoryStream(bytes),
                _strictUtf8,
                detectEncodingFromByteOrderMarks: true);

            return reader.ReadToEnd();
        }
        catch (DecoderFallbackException)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }
}
