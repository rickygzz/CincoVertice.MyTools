namespace CincoVertice.MyTools.Ui.Hotstrings.Models;

/// <summary>A line of hotstrings.cfg that was skipped, and why.</summary>
public sealed record HotstringProblemModel(int Line, string Message);
