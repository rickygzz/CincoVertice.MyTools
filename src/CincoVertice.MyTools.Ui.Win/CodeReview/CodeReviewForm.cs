using CincoVertice.Common.Application.CodeStandard.Models;
using CincoVertice.Common.Ui.Rtf.Constants;
using CincoVertice.Common.Ui.Rtf.Models;
using CincoVertice.Common.Ui.Rtf.Themes;
using CincoVertice.MyTools.Ui.CodeReview.Views;

namespace CincoVertice.MyTools.Ui.Win.CodeReview;

public partial class CodeReviewForm : Form, ICodeReviewView
{
    private const string CheckTitle = "Code standard check";
    private const string FixTitle = "Code standard fix";

    // Same palette as the console output control's theme, used to look up color indexes
    private readonly ModernConsole _theme = new();

    public event EventHandler? LoadCodeReview;

    // public readonly ConsoleOutputView _consoleOutput;

    public CodeReviewForm()
    {
        InitializeComponent();

        // _consoleOutput = new ConsoleOutputView(ConsoleOutput);
    }

    private void CodeReviewForm_Load(object sender, EventArgs e)
    {
        LoadCodeReview?.Invoke(this, EventArgs.Empty);
        // _consoleOutput.OnLoadConsoleOuput(this, EventArgs.Empty);
    }

    public void InitializeConsoleOutput()
    {
        // throw new NotImplementedException();
    }

    public void ShowCheckingFolder(string folderPath)
    {
        if (IsDisposed)
        {
            return;
        }

        Text = $"Code standard - {folderPath}";

        ConsoleOutput.ClearContent();
        WriteHeader(CheckTitle, folderPath);
        WriteLine("Checking...", ColorFormat("Foreground"));
        ConsoleOutput.UpdateText();
    }

    public void ShowCheckResults(string folderPath, IReadOnlyList<FileCheckResultModel> results)
    {
        // The window may have been closed while the check was running
        if (IsDisposed)
        {
            return;
        }

        ConsoleOutput.ClearContent();
        WriteHeader(CheckTitle, folderPath);
        WriteCheckResults(folderPath, results);

        // Not scrolled to the bottom, so the first errors are visible
        ConsoleOutput.UpdateText();
    }

    public void ShowCheckError(string folderPath, string message)
    {
        ShowError(CheckTitle, folderPath, message);
    }

    public void ShowFixingFolder(string folderPath)
    {
        if (IsDisposed)
        {
            return;
        }

        Text = $"Code standard fix - {folderPath}";

        ConsoleOutput.ClearContent();
        WriteHeader(FixTitle, folderPath);
        WriteLine("Fixing...", ColorFormat("Foreground"));
        ConsoleOutput.UpdateText();
    }

    public void ShowFixResults(
        string folderPath,
        IReadOnlyList<FileFixResultModel> fixes,
        IReadOnlyList<FileCheckResultModel> remaining)
    {
        // The window may have been closed while the fix was running
        if (IsDisposed)
        {
            return;
        }

        ConsoleOutput.ClearContent();
        WriteHeader(FixTitle, folderPath);

        var changed = fixes.Where(f => f.IsChanged).ToList();
        var skipped = fixes.Where(f => f.SkippedReason is not null).ToList();

        foreach (var fix in changed)
        {
            WriteLine($"Fixed  {Path.GetRelativePath(folderPath, fix.FilePath)}", ColorFormat("Accent1Light"));
        }

        foreach (var fix in skipped)
        {
            WriteLine(
                $"Skipped  {Path.GetRelativePath(folderPath, fix.FilePath)}: {fix.SkippedReason}",
                RtfConstants.Highlight);
        }

        string summary = fixes.Count == 0
            ? "No C# files found."
            : $"Fixed {changed.Count} of {fixes.Count} file(s)."
                + (skipped.Count > 0 ? $" Skipped {skipped.Count}." : "");

        if (changed.Count > 0 || skipped.Count > 0)
        {
            ConsoleOutput.AddNewLine();
        }

        WriteLine(summary, ColorFormat("Foreground", bold: true));

        if (fixes.Count > 0)
        {
            ConsoleOutput.AddNewLine();
            WriteLine("Issues left (not fixable automatically)", ColorFormat("Foreground", bold: true));
            ConsoleOutput.AddNewLine();
            WriteCheckResults(folderPath, remaining);
        }

        ConsoleOutput.UpdateText();
    }

    public void ShowFixError(string folderPath, string message)
    {
        ShowError(FixTitle, folderPath, message);
    }

    /// <summary>
    ///     Writes the errors of each file, then a summary line.
    /// </summary>
    private void WriteCheckResults(string folderPath, IReadOnlyList<FileCheckResultModel> results)
    {
        int errorCount = 0;
        int filesWithErrors = 0;

        foreach (var result in results.Where(r => r.Errors.Count > 0))
        {
            errorCount += result.Errors.Count;
            filesWithErrors++;

            WriteLine(
                $"{Path.GetRelativePath(folderPath, result.FilePath)} ({result.Errors.Count})",
                ColorFormat("Accent1Light", bold: true));

            foreach (var error in result.Errors)
            {
                ConsoleOutput.AddText($"    {error.Line,5}  ", ColorFormat("Accent1Dark"));
                ConsoleOutput.AddText($"{error.Code}  ", ColorFormat("Accent1Base"));
                WriteLine(error.Message, ColorFormat("Foreground"));
            }

            ConsoleOutput.AddNewLine();
        }

        string summary = results.Count == 0
            ? "No C# files found."
            : errorCount == 0
                ? $"No issues found in {results.Count} file(s)."
                : $"{errorCount} issue(s) in {filesWithErrors} of {results.Count} file(s).";

        WriteLine(summary, ColorFormat(errorCount == 0 ? "Accent1Lighter" : "Foreground", bold: true));
    }

    private void ShowError(string title, string folderPath, string message)
    {
        if (IsDisposed)
        {
            return;
        }

        ConsoleOutput.ClearContent();
        WriteHeader(title, folderPath);
        WriteLine(message, RtfConstants.Highlight);
        ConsoleOutput.UpdateText();
    }

    private void WriteHeader(string title, string folderPath)
    {
        WriteLine(title, ColorFormat("Foreground", bold: true));
        WriteLine(folderPath, ColorFormat("Accent1Lighter"));
        ConsoleOutput.AddNewLine();
    }

    private void WriteLine(string text, RtfFormat format)
    {
        ConsoleOutput.AddText(text, format);
        ConsoleOutput.AddNewLine();
    }

    /// <summary>
    ///     Format with a theme color. Size and line height are always set, because unformatted text at the
    ///     start of the document gets the RTF default of 11 pt with double spacing.
    /// </summary>
    private RtfFormat ColorFormat(string colorName, bool bold = false)
    {
        return new RtfFormat
        {
            Bold = bold,
            Italic = false,
            ForeColorIndex = _theme.ColorIndex(colorName),
            HighlightColorIndex = RtfConstants.AutomaticColorIndex,
            Size = 10,
            LineHeight = 1,
            FontIndex = 0,
        };
    }
}
