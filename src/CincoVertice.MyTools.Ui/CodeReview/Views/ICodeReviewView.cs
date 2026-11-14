using CincoVertice.Common.Application.CodeStandard.Models;

namespace CincoVertice.MyTools.Ui.CodeReview.Views;

public interface ICodeReviewView : IConsoleOutputView
{
    event EventHandler? LoadCodeReview;

    /// <summary>Shows that a folder check has started.</summary>
    void ShowCheckingFolder(string folderPath);

    /// <summary>Shows the code standard errors found in a folder.</summary>
    /// <param name="folderPath">The checked folder.</param>
    /// <param name="results">One result per checked file, including files without errors.</param>
    void ShowCheckResults(string folderPath, IReadOnlyList<FileCheckResultModel> results);

    /// <summary>Shows that a folder could not be checked.</summary>
    void ShowCheckError(string folderPath, string message);

    /// <summary>Shows that a folder fix has started.</summary>
    void ShowFixingFolder(string folderPath);

    /// <summary>Shows which files were fixed, and the issues left that the fixers can't fix.</summary>
    /// <param name="folderPath">The fixed folder.</param>
    /// <param name="fixes">One result per file, including unchanged and skipped files.</param>
    /// <param name="remaining">The folder checked again after the fix.</param>
    void ShowFixResults(
        string folderPath,
        IReadOnlyList<FileFixResultModel> fixes,
        IReadOnlyList<FileCheckResultModel> remaining);

    /// <summary>Shows that a folder could not be fixed.</summary>
    void ShowFixError(string folderPath, string message);
}
