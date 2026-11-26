using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>A value that isn't what the schema says, underlined in the text.</summary>
/// <param name="Start">Where the value starts.</param>
/// <param name="End">Just past its end.</param>
/// <param name="Path">Its JMESPath.</param>
/// <param name="Message">What's wrong: Expected a number, not a string.</param>
public sealed record JsonSchemaProblemModel(DiffPosition Start, DiffPosition End, string Path, string Message);
