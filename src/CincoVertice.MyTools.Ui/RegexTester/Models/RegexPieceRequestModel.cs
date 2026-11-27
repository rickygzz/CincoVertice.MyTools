namespace CincoVertice.MyTools.Ui.RegexTester.Models;

/// <summary>A part chosen in the Add menu, with the pattern as it was and the text selected in the sample.</summary>
public sealed record RegexPieceRequestModel(RegexPieceModel Piece, RegexEditModel Pattern, string SampleSelection);
