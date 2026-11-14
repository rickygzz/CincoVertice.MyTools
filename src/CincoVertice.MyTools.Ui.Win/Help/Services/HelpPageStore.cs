using CincoVertice.MyTools.Ui.Help.Interfaces;

namespace CincoVertice.MyTools.Ui.Win.Help.Services;

/// <summary>The user guide's pages: the Markdown files in docs\user-guide next to the exe, as in the zip.</summary>
public sealed class HelpPageStore : IHelpPageStore
{
    private readonly string _folder;

    public HelpPageStore(string folder)
    {
        _folder = folder;
    }

    public static string DefaultFolder { get; } = Path.Combine(AppContext.BaseDirectory, "docs", "user-guide");

    public IReadOnlyList<string> GetPageNames()
    {
        try
        {
            return Directory.Exists(_folder)
                ? [.. Directory.GetFiles(_folder, "*.md").Select(path => Path.GetFileName(path))]
                : [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    public string? Read(string name)
    {
        // Only the guide's own pages: a link can't read a file elsewhere
        if (name.Length == 0 || Path.GetFileName(name) != name)
        {
            return null;
        }

        try
        {
            return File.ReadAllText(Path.Combine(_folder, name));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
