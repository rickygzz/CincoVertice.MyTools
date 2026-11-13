using CincoVertice.Common.Application.CodeStandard.Interfaces;
using CincoVertice.Common.Application.CodeStandard.Models;
using CincoVertice.MyTools.Ui.CodeReview.Views;

namespace CincoVertice.MyTools.Ui.CodeReview.Presenters;

public class CodeReviewPresenter
{
    private readonly ICodeReviewView _view;
    private readonly ICodeCheckerService _codeChecker;
    private readonly ICodeFixerService _codeFixer;

    public CodeReviewPresenter(ICodeReviewView view, ICodeCheckerService codeChecker, ICodeFixerService codeFixer)
    {
        _view = view;
        _codeChecker = codeChecker;
        _codeFixer = codeFixer;

        _view.LoadCodeReview += OnLoadCodeReview;
    }

    /// <summary>
    ///     Checks every C# and JavaScript file in a folder for the code standard and shows the results in the view.
    ///     Call it from the UI thread: the check runs in the background and the view is updated on the UI thread.
    /// </summary>
    public async Task CheckFolderAsync(string folderPath)
    {
        _view.ShowCheckingFolder(folderPath);

        try
        {
            // Large folders can take a while; keep the UI responsive
            List<FileCheckResultModel> results = await Task.Run(() => _codeChecker.CheckFolder(folderPath));

            _view.ShowCheckResults(folderPath, results);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowCheckError(folderPath, ex.Message);
        }
    }

    /// <summary>
    ///     Fixes every C# and JavaScript file in a folder for the code standard, in place, then checks it again and
    ///     shows what changed and the issues left. Call it from the UI thread, like <see cref="CheckFolderAsync"/>.
    /// </summary>
    /// <param name="folderPath">The folder.</param>
    /// <param name="options">
    ///     The fixers to run, the line ending and the encoding; every fixer, keeping the rest, when null.
    /// </param>
    public async Task FixFolderAsync(string folderPath, FixOptionsModel? options = null)
    {
        _view.ShowFixingFolder(folderPath);

        try
        {
            var (fixes, remaining) = await Task.Run(
                () => (_codeFixer.FixFolder(folderPath, options), _codeChecker.CheckFolder(folderPath)));

            _view.ShowFixResults(folderPath, fixes, remaining);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowFixError(folderPath, ex.Message);
        }
    }

    private void OnLoadCodeReview(object? sender, EventArgs e)
    {
        _view.InitializeConsoleOutput();
        // This: _view.DisplayMessage("Code Review Loaded."); .
    }
}
