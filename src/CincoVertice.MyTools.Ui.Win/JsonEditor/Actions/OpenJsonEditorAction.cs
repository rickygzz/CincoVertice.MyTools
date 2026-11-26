using CincoVertice.MyTools.Ui.Diff.Services;
using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.JsonEditor.Presenters;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.JsonEditor.Actions;

/// <summary>
///     Opens a new JSON editor window, on the screen with the pointer, empty, for a file to open or JSON to paste. Each
///     run opens another; each disposes itself when closed.
/// </summary>
public sealed class OpenJsonEditorAction : IAppAction
{
    private readonly AppIconService _appIcon;
    private readonly HelpWindowService _help;

    public OpenJsonEditorAction(AppIconService appIcon, HelpWindowService help)
    {
        _appIcon = appIcon;
        _help = help;
    }

    public string Name => "OpenJsonEditor";

    public string? DefaultShortcut => "Ctrl+Alt+J";

    public Task ExecuteAsync()
    {
        Open(null);

        return Task.CompletedTask;
    }

    /// <summary>Opens a window with this file; null for an empty one.</summary>
    public void Open(string? path)
    {
        var form = new JsonEditorForm { Icon = _appIcon.Current };
        var presenter = new JsonEditorPresenter(form, new DiskDiffFileReader(), new DiskDiffFileWriter());
        _help.OpenOnF1(form, () => HelpTopics.JsonEditor);
        presenter.Start(path);
        form.CenterOnCursorScreen();
        form.ActivateWhenShown();
        form.Show();
    }
}
