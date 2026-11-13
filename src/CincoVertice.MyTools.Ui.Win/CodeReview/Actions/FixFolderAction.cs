using CincoVertice.MyTools.Ui.CodeReview.Services;
using CincoVertice.MyTools.Ui.Win.CodeReview.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.CodeReview.Actions;

/// <summary>
///     Asks for a folder and, after a warning, fixes it for the code standard in place, with the fixers turned on in
///     the tray menu, showing the result in a new Code Review window.
/// </summary>
public sealed class FixFolderAction : IAppAction
{
    private const string Title = "Fix folder for code standard";

    private readonly CodeReviewWindowService _codeReviewWindowService;
    private readonly FixerSelection _fixers;

    // Folder picker starts here on the next fix
    private string _lastFixedFolder = string.Empty;

    public FixFolderAction(CodeReviewWindowService codeReviewWindowService, FixerSelection fixers)
    {
        _codeReviewWindowService = codeReviewWindowService;
        _fixers = fixers;
    }

    public string Name => "FixFolder";

    public string? DefaultShortcut => null;

    public async Task ExecuteAsync()
    {
        IReadOnlyCollection<string> fixers = _fixers.On;

        if (fixers.Count == 0)
        {
            MessageBox.Show(
                "Every fixer is off, so there's nothing to fix with.\n\n"
                + "Turn some on in the tray menu, under Fix folder for standard.",
                Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the folder to fix for the code standard",
            UseDescriptionForTitle = true,
            InitialDirectory = _lastFixedFolder,
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        _lastFixedFolder = dialog.SelectedPath;

        // Which fixers won't run, so a fix that leaves something isn't a surprise
        IReadOnlyList<string> off = _fixers.OffTitles;
        string offText = off.Count == 0 ? string.Empty : $"These fixers are off: {string.Join(", ", off)}.\n\n";

        // Files are rewritten in place, so give a chance to commit first; No is the default button
        var answer = MessageBox.Show(
            $"This rewrites the C# and JavaScript files in\n{dialog.SelectedPath}\nin place.\n\n"
            + offText
            + "Commit or back up your changes first, so you can review the fixes and undo them.\n\n"
            + "Fix the folder now?",
            Title,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes)
        {
            return;
        }

        var presenter = _codeReviewWindowService.Open();

        await presenter.FixFolderAsync(dialog.SelectedPath, fixers);
    }
}
