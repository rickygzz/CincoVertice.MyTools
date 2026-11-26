namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>A piece of a line of JSON, colored by its kind.</summary>
/// <param name="Kind">What it is.</param>
/// <param name="Column">Where it starts in its line, from 0.</param>
/// <param name="Length">How many characters long.</param>
public readonly record struct JsonTokenModel(JsonTokenKind Kind, int Column, int Length);
