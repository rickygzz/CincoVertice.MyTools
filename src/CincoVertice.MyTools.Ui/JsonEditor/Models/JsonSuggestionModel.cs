namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>A suggestion for the search box.</summary>
/// <param name="Text">What the list shows: a name, a function, a value.</param>
/// <param name="Detail">What it is: string "Ann", array of 3, length(string, array or object).</param>
/// <param name="Insert">What choosing it writes, in place of the part typed.</param>
/// <param name="Start">Where the part typed starts in the search text.</param>
public sealed record JsonSuggestionModel(string Text, string Detail, string Insert, int Start);
