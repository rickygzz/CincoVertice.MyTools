using CincoVertice.MyTools.Ui.Win.CodeReview.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.CodeReview.Actions;

/// <summary>
///     Asks for a folder and, after a warning, fixes it for the code standard in place, showing the result in a new
///     Code Review window.
/// </summary>
public sealed class FixFolderAction : IAppAction
{
    private readonly CodeReviewWindowService _codeReviewWindowService;

    // Folder picker starts here on the next fix
    private string _lastFixedFolder = string.Empty;

    public FixFolderAction(CodeReviewWindowService codeReviewWindowService)
    {
        _codeReviewWindowService = codeReviewWindowService;
    }

    public string Name => "FixFolder";

    public string? DefaultShortcut => null;

    public async Task ExecuteAsync()
    {
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

        // Files are rewritten in place, so give a chance to commit first; No is the default button
        var answer = MessageBox.Show(
            $"This rewrites the C# files in\n{dialog.SelectedPath}\nin place.\n\n"
            + "Commit or back up your changes first, so you can review the fixes and undo them.\n\n"
            + "Fix the folder now?",
            "Fix folder for code standard",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes)
        {
            return;
        }

        var presenter = _codeReviewWindowService.Open();

        await presenter.FixFolderAsync(dialog.SelectedPath);
    }
}
