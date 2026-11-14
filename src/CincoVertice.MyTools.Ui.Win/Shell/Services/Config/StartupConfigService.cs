using System.Text;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Models;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

/// <summary>
///     Owns startup.cfg: creates it with only comments the first time, so nothing runs until the user asks, and reads
///     it.
/// </summary>
public sealed class StartupConfigService
{
    public StartupConfigService(string filePath)
    {
        FilePath = filePath;
    }

    /// <summary>config\startup.cfg next to the executable.</summary>
    public static string DefaultFilePath { get; } = Path.Combine(
        AppContext.BaseDirectory,
        "config",
        "startup.cfg");

    public string FilePath { get; }

    /// <summary>
    ///     Reads the actions to run, creating the file when it doesn't exist. When the file can't be read or written,
    ///     nothing runs and the reason is reported as a problem.
    /// </summary>
    public StartupConfigResultModel Load(IReadOnlyCollection<IAppAction> actions)
    {
        List<string> actionNames = [.. actions.Select(a => a.Name)];

        try
        {
            if (!File.Exists(FilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, CreateDefaultText());
            }

            return StartupConfigReader.Read(File.ReadAllText(FilePath), actionNames);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var result = new StartupConfigResultModel();
            result.Problems.Add($"Could not use {FilePath}, so nothing was run: {ex.Message}");

            return result;
        }
    }

    /// <summary>The content of a new startup.cfg: how to use it, and nothing to run.</summary>
    public static string CreateDefaultText()
    {
        var sb = new StringBuilder();

        sb.AppendLine("# MyTools startup actions: run when Windows starts MyTools at sign-in (Start with Windows),");
        sb.AppendLine("# not when you start it yourself. One line per action, run in order, each after the one");
        sb.AppendLine("# before ends. Changes apply at the next sign-in.");
        sb.AppendLine("#   Run = Action");
        sb.AppendLine("#   Run = Action, value      (a value for the script's script.argument())");
        sb.AppendLine("# A script can wait until the computer is quiet before it starts a program:");
        sb.AppendLine("#   if system.waitidle(15, 20, 60, 20)");
        sb.AppendLine("#       shell.run(\"C:\\Program Files\\App\\App.exe\")");
        sb.AppendLine("#   end");
        sb.AppendLine("# See docs/user-guide/08-scripts.md.");
        sb.AppendLine();
        sb.AppendLine("# Run = StartLater");

        return sb.ToString();
    }
}
