using CincoVertice.MyTools.Ui.Win.CodeReview.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.CodeReview.Actions;

/// <summary>
///     Asks for a folder and checks it for the code standard in a new Code Review window.
/// </summary>
public sealed class CheckFolderAction : IAppAction
{
    private readonly CodeReviewWindowService _codeReviewWindowService;

    // Folder picker starts here on the next check
    private string _lastCheckedFolder = string.Empty;

    public CheckFolderAction(CodeReviewWindowService codeReviewWindowService)
    {
        _codeReviewWindowService = codeReviewWindowService;
    }

    public string Name => "CheckFolder";

    public string? DefaultShortcut => null;

    /// <summary>
    ///     Shows the folder picker; when a folder is chosen, opens a Code Review window and checks it there.
    /// </summary>
    public async Task ExecuteAsync()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the folder to check for the code standard",
            UseDescriptionForTitle = true,
            InitialDirectory = _lastCheckedFolder,
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        _lastCheckedFolder = dialog.SelectedPath;

        var presenter = _codeReviewWindowService.Open();

        await presenter.CheckFolderAsync(dialog.SelectedPath);
    }
}
