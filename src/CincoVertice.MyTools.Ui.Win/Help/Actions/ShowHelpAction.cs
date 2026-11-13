using CincoVertice.MyTools.Ui.Win.Help.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.Help.Actions;

/// <summary>Opens the user guide in the help window, at its contents; brings the window to the front when open.</summary>
public sealed class ShowHelpAction : IAppAction
{
    private readonly HelpWindowService _help;

    public ShowHelpAction(HelpWindowService help)
    {
        _help = help;
    }

    public string Name => "ShowHelp";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        _help.Open();

        return Task.CompletedTask;
    }
}
