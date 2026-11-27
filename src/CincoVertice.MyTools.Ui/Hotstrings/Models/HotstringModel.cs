namespace CincoVertice.MyTools.Ui.Hotstrings.Models;

/// <summary>One line of hotstrings.cfg: what's typed, and the text it becomes.</summary>
/// <param name="Abbreviation">Letters and digits, matched ignoring case.</param>
/// <param name="Text">What it's replaced by; \n for each new line.</param>
/// <param name="Line">Its line in the file.</param>
public sealed record HotstringModel(string Abbreviation, string Text, int Line);
