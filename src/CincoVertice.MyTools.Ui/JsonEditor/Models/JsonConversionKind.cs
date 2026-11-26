namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>The Convert menu's choices.</summary>
public enum JsonConversionKind
{
    CopyYaml,
    CopyCsv,
    SaveYaml,
    SaveCsv,

    /// <summary>A CSV file read as a new JSON document.</summary>
    OpenCsv,

    /// <summary>CSV on the clipboard read as a new JSON document.</summary>
    PasteCsv,
}
