namespace CincoVertice.MyTools.Ui.Help.Models;

/// <summary>A place in the user guide that back and forward go to.</summary>
/// <param name="Page">The page's file name.</param>
/// <param name="Anchor">The heading it was opened at; null for the top.</param>
/// <param name="Top">
///     Where the text at the top of the window was when it was left, so going back shows what was shown; null
///     before it's left.
/// </param>
public sealed record HelpPlaceModel(string Page, string? Anchor, int? Top = null);
