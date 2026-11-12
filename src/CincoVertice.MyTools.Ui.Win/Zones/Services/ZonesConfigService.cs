using System.Drawing;
using System.Globalization;
using System.Text;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Zones.Models;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Models;

namespace CincoVertice.MyTools.Ui.Win.Zones.Services;

/// <summary>
///     Owns zones.cfg: creates it with the default layout the first time, reads it each time the zones are needed, so a
///     change applies at once, and saves what the zone editor makes. Problems are shown in a notification, once until
///     they change.
/// </summary>
public sealed class ZonesConfigService : IZoneLayoutStore
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

    public ZoneLayoutsModel Load()
    {
        ZonesConfigResultModel result = LoadResult();
        string problems = string.Join(Environment.NewLine, result.Problems);

        if (problems.Length > 0 && problems != _reported)
        {
            _notifications.Show(ProblemsTitle, problems, ToolTipIcon.Warning);
        }

        _reported = problems;

        return result.Layouts;
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

    /// <summary>Writes zones.cfg with the layouts, as Zone lines, for the next drag.</summary>
    public string? Save(ZoneLayoutsModel layouts)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            AtomicFile.WriteAllText(FilePath, CreateText(layouts));

            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"Could not save {FilePath}: {ex.Message}";
        }
    }

    /// <summary>
    ///     The content of zones.cfg for the layouts: how to use the file, then the default layout, then each monitor's
    ///     own after its Monitor line. A Zone line per zone, in percent rounded to hundredths: each zone's right and
    ///     bottom are rounded, not its size, so zones side by side still meet.
    /// </summary>
    public static string CreateText(ZoneLayoutsModel layouts)
    {
        var sb = new StringBuilder();

        AppendHelp(sb);
        sb.AppendLine("# Saved by the window zones editor (tray menu, Edit window zones...).");
        sb.AppendLine();
        AppendLayout(sb, layouts.DefaultLayout);

        foreach (MonitorZoneLayoutModel monitor in layouts.Monitors)
        {
            Rectangle bounds = monitor.Bounds;

            sb.AppendLine();
            sb.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{ZonesConfigReader.MonitorName} = {bounds.X}, {bounds.Y}, {bounds.Width}, {bounds.Height}"));
            AppendLayout(sb, monitor.Layout);
        }

        return sb.ToString();
    }

    /// <summary>The content of a new zones.cfg: how to use it, and three columns.</summary>
    public static string CreateDefaultText()
    {
        var sb = new StringBuilder();

        AppendHelp(sb);
        sb.AppendLine();
        sb.AppendLine("Layout = Columns 3");
        sb.AppendLine($"Gap = {ZoneLayoutModel.DefaultGap}");

        return sb.ToString();
    }

    private static void AppendLayout(StringBuilder sb, ZoneLayoutModel layout)
    {
        foreach (ZoneModel zone in layout.Zones)
        {
            double left = Math.Round(zone.Left, 2);
            double top = Math.Round(zone.Top, 2);
            double width = Math.Round(Math.Round(zone.Right, 2) - left, 2);
            double height = Math.Round(Math.Round(zone.Bottom, 2) - top, 2);

            sb.AppendLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{ZonesConfigReader.ZoneName} = {left}, {top}, {width}, {height}"));
        }

        sb.AppendLine($"{ZonesConfigReader.GapName} = {layout.Gap}");
    }

    private static void AppendHelp(StringBuilder sb)
    {
        sb.AppendLine("# MyTools window zones: hold Shift while you drag a window, and drop it on a zone.");
        sb.AppendLine("# Zones are numbered from 1, for MoveToZone in hotkeys.cfg. Changes apply at the next drag.");
        sb.AppendLine("# The default layout, for every monitor without its own:");
        sb.AppendLine("#   Layout = Columns 3           3 columns as wide as each other");
        sb.AppendLine("#   Layout = Columns 25 50 25    columns this wide, in percent of the screen");
        sb.AppendLine("#   Layout = Rows 2              or Rows 30 70");
        sb.AppendLine("#   Layout = Grid 3 x 2          3 columns, 2 rows");
        sb.AppendLine("# Or your own zones instead, one line each, in percent: left, top, width, height");
        sb.AppendLine("#   Zone = 0, 0, 60, 100");
        sb.AppendLine("#   Zone = 60, 0, 40, 50");
        sb.AppendLine("#   Zone = 60, 50, 40, 50");
        sb.AppendLine("# Gap: pixels between zones and around them, 0 for none.");
        sb.AppendLine("# A monitor's own layout: the same lines after a Monitor line, with where Windows has the");
        sb.AppendLine("# monitor, in pixels: left, top, width, height. Without a Gap, it takes the default one's.");
        sb.AppendLine("#   Monitor = 1920, 0, 2560, 1440");
        sb.AppendLine("#   Layout = Rows 2");
    }
}
