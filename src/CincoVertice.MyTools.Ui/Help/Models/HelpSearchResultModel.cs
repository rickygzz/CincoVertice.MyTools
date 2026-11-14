namespace CincoVertice.MyTools.Ui.Help.Models;

/// <summary>A page of the user guide that has the text searched for.</summary>
/// <param name="Page">The page's file name.</param>
/// <param name="Title">The page's title.</param>
/// <param name="Snippet">Some of the page's text around the first time it has it, on one line.</param>
/// <param name="Count">How many times the page has it.</param>
public sealed record HelpSearchResultModel(string Page, string Title, string Snippet, int Count);
