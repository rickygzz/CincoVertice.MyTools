namespace CincoVertice.MyTools.Ui.ConfigEditor.Models;

/// <summary>A name a config file can set (<c>Name = Value</c>), for the editor's suggestions.</summary>
/// <param name="Name">As written at the start of a line.</param>
/// <param name="Detail">What it means, its default and what it allows.</param>
/// <param name="Values">What to offer after its "=".</param>
/// <param name="IsRepeatable">Whether it can be on several lines, as Zone; others are offered once a section.</param>
/// <param name="StartsSection">Whether it starts a section of its own, as Monitor in zones.cfg.</param>
/// <param name="ArgumentsOf">What to offer after the comma after a value, given that value; null for nothing.</param>
public sealed record ConfigSettingModel(
    string Name,
    string Detail,
    IReadOnlyList<CodeSuggestionModel> Values,
    bool IsRepeatable = false,
    bool StartsSection = false,
    Func<string, IReadOnlyList<CodeSuggestionModel>>? ArgumentsOf = null);
