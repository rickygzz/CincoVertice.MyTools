using System.Text;
using CincoVertice.MyTools.Ui.Hotstrings.Models;
using CincoVertice.MyTools.Ui.Hotstrings.Services;

namespace CincoVertice.MyTools.Ui.Win.Hotstrings.Services;

/// <summary>
///     Owns hotstrings.cfg: creates it with only comments the first time, so nothing is replaced until the user asks,
///     and reads it.
/// </summary>
public sealed class HotstringsConfigService
{
    public HotstringsConfigService(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>config\hotstrings.cfg next to the executable.</summary>
    public static string DefaultFilePath { get; } = Path.Combine(AppContext.BaseDirectory, "config", "hotstrings.cfg");

    public string FilePath { get; }

    /// <summary>
    ///     Reads the hotstrings, creating the file when it doesn't exist. When the file can't be read or written, there
    ///     are none, and the reason is the problem.
    /// </summary>
    public HotstringsResultModel Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, CreateDefaultText());
            }

            return HotstringReader.Read(File.ReadAllText(FilePath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new HotstringsResultModel([], [new HotstringProblemModel(0, $"Could not read {FilePath}: {ex.Message}")]);
        }
    }

    /// <summary>The content of a new hotstrings.cfg: how to use it, and examples left as comments.</summary>
    public static string CreateDefaultText()
    {
        var sb = new StringBuilder();

        sb.AppendLine("# MyTools hotstrings: type a name, then Space, Enter or Tab, and it's replaced by its text,");
        sb.AppendLine("# in any program. A name is letters and digits, matched ignoring case, as a word of its own.");
        sb.AppendLine("# Changes apply when you save. One per line:");
        sb.AppendLine("#   name = text");
        sb.AppendLine("# A text of several lines goes between [\" and \"]:");
        sb.AppendLine("#   sig = [\"Best regards,");
        sb.AppendLine("#   Ricky\"]");
        sb.AppendLine("# See docs/user-guide/19-hotstrings.md.");
        sb.AppendLine();
        sb.AppendLine("# btw = by the way");

        return sb.ToString();
    }
}
