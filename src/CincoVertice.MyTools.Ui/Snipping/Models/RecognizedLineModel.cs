namespace CincoVertice.MyTools.Ui.Snipping.Models;

/// <summary>A line of text read from an image: its words, left to right.</summary>
public sealed record RecognizedLineModel(IReadOnlyList<RecognizedWordModel> Words);
