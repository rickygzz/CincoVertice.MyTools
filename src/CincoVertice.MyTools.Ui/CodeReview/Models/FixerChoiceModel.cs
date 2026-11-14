namespace CincoVertice.MyTools.Ui.CodeReview.Models;

/// <summary>A fixer of the code standard that the user can turn on or off for Fix folder.</summary>
/// <param name="Name">Its name, which the choice is kept by, e.g. "BracePlacement".</param>
/// <param name="Title">Its name in words, e.g. "Braces on their own lines".</param>
/// <param name="Description">What it does, in a sentence; empty when it's not known.</param>
/// <param name="Languages">The languages it fixes, e.g. "C# and JavaScript".</param>
/// <param name="IsOn">Whether Fix folder runs it.</param>
public sealed record FixerChoiceModel(string Name, string Title, string Description, string Languages, bool IsOn);
