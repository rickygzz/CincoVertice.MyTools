using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Models;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Runs the actions in startup.cfg, when Windows started the app at sign-in. In order, each after the one before
///     ends, so a script that waits until the computer is quiet holds back the ones after it too. The tray and the
///     hotkeys work meanwhile, since the actions wait without blocking the UI thread.
/// </summary>
public sealed class StartupActionsService
{
    public const string ProblemsTitle = "My Tools: some startup actions are off";

    private const int MaxProblemsInNotification = 3;

    private readonly StartupConfigService _config;
    private readonly Func<IReadOnlyCollection<IAppAction>> _actions;
    private readonly TrayNotificationService _notifications;

    /// <param name="config">startup.cfg.</param>
    /// <param name="actions">The actions there are now, scripts included.</param>
    /// <param name="notifications">Where problems are reported.</param>
    public StartupActionsService(
        StartupConfigService config,
        Func<IReadOnlyCollection<IAppAction>> actions,
        TrayNotificationService notifications)
    {
        _config = config;
        _actions = actions;
        _notifications = notifications;
    }

    /// <summary>
    ///     Runs them. Lines that can't be used are reported in one notification and skipped. Never throws: an action
    ///     that fails is reported, and the next one runs.
    /// </summary>
    public async Task RunAsync()
    {
        IReadOnlyCollection<IAppAction> actions = _actions();
        StartupConfigResultModel config = _config.Load(actions);

        if (config.Problems.Count > 0)
        {
            _notifications.Show(ProblemsTitle, SummarizeProblems(config.Problems), ToolTipIcon.Warning);
        }

        foreach (StartupActionModel startup in config.Actions)
        {
            IAppAction action = actions.First(a => a.Name == startup.ActionName);

            try
            {
                await RunAsync(action, startup.Argument);
            }
            catch (Exception ex)
            {
                _notifications.Show(ProblemsTitle, $"{action.Name} failed: {ex.Message}", ToolTipIcon.Warning);
            }
        }
    }

    /// <summary>The first problems, how many more, and where to fix them; a notification shows about 255 characters.</summary>
    public static string SummarizeProblems(IReadOnlyList<string> problems)
    {
        List<string> lines = [.. problems.Take(MaxProblemsInNotification)];

        if (problems.Count > MaxProblemsInNotification)
        {
            lines.Add($"...and {problems.Count - MaxProblemsInNotification} more.");
        }

        lines.Add("Fix them in startup.cfg: tray menu, Edit hotkeys.");

        return string.Join(Environment.NewLine, lines);
    }

    private Task RunAsync(IAppAction action, string? argument)
    {
        if (argument is null)
        {
            return action.ExecuteAsync();
        }

        if (action is IArgumentAppAction argumentAction)
        {
            return argumentAction.ExecuteAsync(argument);
        }

        _notifications.Show(ProblemsTitle, $"{action.Name}: takes no value ({argument}).", ToolTipIcon.Warning);

        return Task.CompletedTask;
    }
}
