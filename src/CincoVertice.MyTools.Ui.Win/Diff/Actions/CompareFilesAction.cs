using CincoVertice.MyTools.Ui.Diff.Presenters;
using CincoVertice.MyTools.Ui.Diff.Services;
using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Diff.Actions;

/// <summary>
///     Opens a new Compare files window, on the screen with the pointer, ready for two files or pasted texts. Each run
///     opens another, so several comparisons can be open at once; each disposes itself when closed.
/// </summary>
public sealed class CompareFilesAction : IAppAction
{
    private readonly AppIconService _appIcon;
    private readonly HelpWindowService _help;

    public CompareFilesAction(AppIconService appIcon, HelpWindowService help)
    {
        _appIcon = appIcon;
        _help = help;
    }

    public string Name => "CompareFiles";

    public string? DefaultShortcut => null;

    public Task ExecuteAsync()
    {
        Open(null, null);

        return Task.CompletedTask;
    }

    /// <summary>Opens a window comparing these files; either may be null, to choose it there.</summary>
    public void Open(string? leftPath, string? rightPath)
    {
        var form = new DiffForm { Icon = _appIcon.Current };
        var presenter = new DiffPresenter(form, new DiskDiffFileReader(), new DiskDiffFileWriter());
        _help.OpenOnF1(form, () => HelpTopics.CompareFiles);
        presenter.Open(leftPath, rightPath);
        form.CenterOnCursorScreen();
        form.ActivateWhenShown();
        form.Show();
    }

    /// <summary>Shows a modal comparison of two in-memory texts that cannot be saved over source files.</summary>
    /// <param name="owner">The window it's shown over, which waits; none for the pointer's screen.</param>
    public void CompareTexts(
        string leftName,
        string leftText,
        string rightName,
        string rightText,
        IWin32Window? owner = null)
    {
        using var form = new DiffForm { Icon = _appIcon.Current };
        var presenter = new DiffPresenter(form, new DiskDiffFileReader(), new DiskDiffFileWriter());
        _help.OpenOnF1(form, () => HelpTopics.CompareFiles);
        presenter.OpenTexts(leftName, leftText, rightName, rightText);
        form.CenterOnCursorScreen();
        form.ShowDialog(owner);
    }
}
