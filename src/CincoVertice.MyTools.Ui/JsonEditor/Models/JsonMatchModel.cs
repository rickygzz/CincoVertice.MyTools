using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>A place in the text a search found: from Start to just before End, over lines for an object.</summary>
public readonly record struct JsonMatchModel(DiffPosition Start, DiffPosition End);
