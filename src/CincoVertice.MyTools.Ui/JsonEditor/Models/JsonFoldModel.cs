namespace CincoVertice.MyTools.Ui.JsonEditor.Models;

/// <summary>An object or an array over several lines, which can be collapsed to its first line.</summary>
/// <param name="StartLine">The line of its { or [, from 0.</param>
/// <param name="EndLine">The line of its } or ]: hidden too when it's collapsed.</param>
/// <param name="Summary">What a collapsed one shows after its first line: { 3 keys }, [ 12 items ].</param>
public sealed record JsonFoldModel(int StartLine, int EndLine, string Summary);
