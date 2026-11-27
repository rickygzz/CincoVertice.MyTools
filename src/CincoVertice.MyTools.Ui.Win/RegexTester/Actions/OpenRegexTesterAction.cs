using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.RegexTester.Presenters;
using CincoVertice.MyTools.Ui.RegexTester.Services;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.RegexTester.Actions;

/// <summary>
///     Opens a new regex tester window, on the screen with the pointer, with what the last one had. Each run opens
///     another; each disposes itself when closed. No default shortcut: Ctrl+Alt and a letter is AltGr on many keyboard
///     layouts, and the letters left free type characters there, as € with E.
/// </summary>
public sealed class OpenRegexTesterAction : IAppAction
{
    private readonly AppIconService _appIcon;
    private readonly HelpWindowService _help;
    private readonly JsonFileRegexTesterSettingsStore _settings =
        new(Path.Combine(AppContext.BaseDirectory, "config", "regextester.json"));

    public OpenRegexTesterAction(AppIconService appIcon, HelpWindowService help)
    {
        _appIcon = appIcon;
        _help = help;
    }

    public string Name => "OpenRegexTester";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        var form = new RegexTesterForm { Icon = _appIcon.Current };
        var presenter = new RegexTesterPresenter(form, _settings);

        _help.OpenOnF1(form, () => HelpTopics.RegexTester);
        presenter.Start();
        form.CenterOnCursorScreen();
        form.ActivateWhenShown();
        form.Show();

        return Task.CompletedTask;
    }
}
