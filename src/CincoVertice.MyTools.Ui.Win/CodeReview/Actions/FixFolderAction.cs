using CincoVertice.Common.Application.CodeStandard.Models;
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
        FixOptionsModel options = _fixers.Options;

        if (!options.DoesAnything)
        {
            MessageBox.Show(
                "Every fixer is off, and line endings and encodings are kept, so a fix would change nothing.\n\n"
                + "Choose what to do in the tray menu, under Fix folder for standard.",
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

        // Files are rewritten in place, so give a chance to commit first; No is the default button
        var answer = MessageBox.Show(
            $"This rewrites the C# and JavaScript files in\n{dialog.SelectedPath}\nin place.\n\n"
            + WhatItDoes(options)
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

        await presenter.FixFolderAsync(dialog.SelectedPath, options);
    }

    // What the fix does besides the usual, so nothing it does or leaves is a surprise
    private string WhatItDoes(FixOptionsModel options)
    {
        List<string> lines = [];
        IReadOnlyList<string> off = _fixers.OffTitles;

        if (options.Fixers?.Count == 0)
        {
            lines.Add("Every fixer is off.");
        }
        else if (off.Count > 0)
        {
            lines.Add($"These fixers are off: {string.Join(", ", off)}.");
        }

        if (options.LineEnding != LineEndingEnum.None)
        {
            lines.Add($"Every line ends in {FixerSelection.TitleOf(options.LineEnding)}.");
        }

        if (options.Encoding != FileEncodingEnum.Keep)
        {
            lines.Add($"Every file is saved as {FixerSelection.TitleOf(options.Encoding)}.");
        }

        return lines.Count == 0 ? string.Empty : string.Join("\n", lines) + "\n\n";
    }
}
