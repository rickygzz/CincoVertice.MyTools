using CincoVertice.MyTools.Ui.Help.Interfaces;

namespace CincoVertice.MyTools.Ui.Tests.Help;

/// <summary>Pages in memory, by file name.</summary>
public sealed class FakeHelpPageStore(Dictionary<string, string> pages) : IHelpPageStore
{
    public IReadOnlyList<string> GetPageNames() => [.. pages.Keys];

    public string? Read(string name) => pages.GetValueOrDefault(name);
}
