using CincoVertice.MyTools.Ui.Diff.Models;

namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>JSON text as read: its colors, its sections, its values, and the first thing wrong with it.</summary>
/// <param name="Tokens">Each line's pieces, colored.</param>
/// <param name="Folds">The objects and arrays over several lines, by their first line.</param>
/// <param name="Root">The value the text holds, as far as it could be read; null for no text.</param>
/// <param name="Error">What's wrong first, and where; null for JSON that is valid.</param>
/// <param name="ErrorAt">Where that is.</param>
public sealed record JsonDocumentModel(
    IReadOnlyList<IReadOnlyList<JsonTokenModel>> Tokens,
    IReadOnlyList<JsonFoldModel> Folds,
    JsonNodeModel? Root,
    string? Error,
    DiffPosition ErrorAt);
