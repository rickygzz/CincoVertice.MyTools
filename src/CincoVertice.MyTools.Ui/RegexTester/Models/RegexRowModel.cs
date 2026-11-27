namespace CincoVertice.MyTools.Ui.RegexTester.Models;

/// <summary>A row of the matches list: a match, or one of its groups under it.</summary>
/// <param name="Name">"Match 1", or the group's number or name.</param>
/// <param name="Value">What it matched; for a group that took no part, a note saying so.</param>
/// <param name="Place">Where, as "5, 3 long"; empty for a group that took no part.</param>
/// <param name="Start">Where it is in the text, to select it; -1 for none.</param>
public sealed record RegexRowModel(string Name, string Value, string Place, int Start, int Length, bool IsGroup);
