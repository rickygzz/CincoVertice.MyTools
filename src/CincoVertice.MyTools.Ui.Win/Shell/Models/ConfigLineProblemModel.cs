namespace CincoVertice.MyTools.Ui.Win.Shell.Models;

/// <summary>A line of a config file that can't be used, and why. The line starts at 1.</summary>
public sealed record ConfigLineProblemModel(int Line, string Message);
