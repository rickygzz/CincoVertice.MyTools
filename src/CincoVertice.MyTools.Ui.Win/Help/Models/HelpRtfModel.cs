namespace CincoVertice.MyTools.Ui.Win.Help.Models;

/// <summary>A page as RTF for a RichTextBox, with a mark where each link and heading starts.</summary>
public sealed record HelpRtfModel(string Rtf, IReadOnlyList<HelpRtfMarkModel> Marks);
