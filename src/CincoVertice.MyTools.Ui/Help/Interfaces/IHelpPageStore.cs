namespace CincoVertice.MyTools.Ui.Help.Interfaces;

/// <summary>The pages of the user guide, Markdown files.</summary>
public interface IHelpPageStore
{
    /// <returns>The pages' file names, e.g. "02-hotkeys.md"; none when the guide isn't there.</returns>
    IReadOnlyList<string> GetPageNames();

    /// <returns>The page's Markdown; null when there's no such page, or it can't be read.</returns>
    string? Read(string name);
}
