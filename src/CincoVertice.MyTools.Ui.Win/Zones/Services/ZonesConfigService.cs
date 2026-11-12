using System.Text;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Zones.Models;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Win.Zones.Services;

/// <summary>
///     Owns zones.cfg: creates it with the default layout the first time, and reads it each time the zones are needed,
///     so a change applies at once. Problems are shown in a notification, once until they change.
/// </summary>
public sealed class ZonesConfigService : IZoneLayoutSource
{
    public const string ProblemsTitle = "My Tools: some window zones are off";

    private readonly TrayNotificationService _notifications;

    // The problems reported last, so the same ones aren't reported on every drag
    private string _reported = string.Empty;

    public ZonesConfigService(string filePath, TrayNotificationService notifications)
    {
        FilePath = filePath;
        _notifications = notifications;
    }

    /// <summary>config\zones.cfg next to the executable.</summary>
    public static string DefaultFilePath { get; } = Path.Combine(AppContext.BaseDirectory, "config", "zones.cfg");

    public string FilePath { get; }

    public ZoneLayoutModel Load()
    {
        ZonesConfigResultModel result = LoadResult();
        string problems = string.Join(Environment.NewLine, result.Problems);

        if (problems.Length > 0 && problems != _reported)
        {
            _notifications.Show(ProblemsTitle, problems, ToolTipIcon.Warning);
        }

        _reported = problems;

        return result.Layout;
    }

    /// <summary>
    ///     Reads the layout, creating the file when it doesn't exist. When the file can't be read or written, the
    ///     default layout is used and the reason is reported as a problem.
    /// </summary>
    public ZonesConfigResultModel LoadResult()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, CreateDefaultText());
            }

            return ZonesConfigReader.Read(File.ReadAllText(FilePath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var result = new ZonesConfigResultModel();
            result.Problems.Add($"Could not use {FilePath}, so the default zones are used: {ex.Message}");

            return result;
        }
    }

    /// <summary>The content of a new zones.cfg: how to use it, and three columns.</summary>
    public static string CreateDefaultText()
    {
        var sb = new StringBuilder();

        sb.AppendLine("# MyTools window zones: hold Shift while you drag a window, and drop it on a zone.");
        sb.AppendLine("# The same zones on every monitor, numbered from 1, for MoveToZone in hotkeys.cfg.");
        sb.AppendLine("# Changes apply at the next drag. One layout:");
        sb.AppendLine("#   Layout = Columns 3           3 columns as wide as each other");
        sb.AppendLine("#   Layout = Columns 25 50 25    columns this wide, in percent of the screen");
        sb.AppendLine("#   Layout = Rows 2              or Rows 30 70");
        sb.AppendLine("#   Layout = Grid 3 x 2          3 columns, 2 rows");
        sb.AppendLine("# Or your own zones instead, one line each, in percent: left, top, width, height");
        sb.AppendLine("#   Zone = 0, 0, 60, 100");
        sb.AppendLine("#   Zone = 60, 0, 40, 50");
        sb.AppendLine("#   Zone = 60, 50, 40, 50");
        sb.AppendLine("# Gap: pixels between zones and around them, 0 for none.");
        sb.AppendLine();
        sb.AppendLine("Layout = Columns 3");
        sb.AppendLine($"Gap = {ZoneLayoutModel.DefaultGap}");

        return sb.ToString();
    }
}
