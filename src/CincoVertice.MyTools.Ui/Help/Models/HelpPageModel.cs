namespace CincoVertice.MyTools.Ui.Help.Models;

/// <summary>A page of the user guide.</summary>
/// <param name="Name">Its file name, as links give it: "02-hotkeys.md".</param>
/// <param name="Title">Its first heading, or its name when it has none.</param>
public sealed record HelpPageModel(string Name, string Title);
