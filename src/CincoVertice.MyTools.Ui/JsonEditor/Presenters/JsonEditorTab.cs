using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Schema;
using CincoVertice.MyTools.Ui.JsonEditor.Services;

namespace CincoVertice.MyTools.Ui.JsonEditor.Presenters;

/// <summary>
///     What <see cref="JsonEditorPresenter"/> keeps of one tab's document: its file, the text as saved, the document
///     read from the text, the caret, and its schema. The text itself is the view's, in the tab's own editor.
/// </summary>
internal sealed class JsonEditorTab
{
    public string? Path { get; set; }

    public DiffTextFormatModel Format { get; set; } = DiffTextFormatModel.Default;

    public IReadOnlyList<string> SavedLines { get; set; } = [string.Empty];

    public JsonDocumentModel Document { get; set; } = JsonDocumentReader.Read([string.Empty]);

    public DiffPosition Caret { get; set; }

    // The schema: one chosen, or else the one the document names in $schema, unless turned off
    public string? ChosenSchema { get; set; }

    public bool IsSchemaOff { get; set; }

    public string? SchemaPath { get; set; }

    public JsonSchemaValidator? Validator { get; set; }

    public string? SchemaProblem { get; set; }

    public IReadOnlyList<JsonSchemaProblemModel> Problems { get; set; } = [];

    // A large text: read on another thread, once more if it changed meanwhile; Document is behind it until then
    public bool IsStale { get; set; }

    public bool IsReading { get; set; }

    public bool ReadAgain { get; set; }

    public Task Reading { get; set; } = Task.CompletedTask;

    /// <summary>The name shown in the tab and the title.</summary>
    public string Name => Path is null ? "Untitled" : System.IO.Path.GetFileName(Path);
}
