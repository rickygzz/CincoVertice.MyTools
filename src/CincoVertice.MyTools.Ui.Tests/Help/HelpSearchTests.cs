using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Help;

public class HelpSearchTests
{
    [Fact]
    public void ThePageWithTheMost_IsFirst_CountingTablesAndCode()
    {
        // Arrange
        (HelpPageModel, MarkdownDocumentModel)[] pages =
        [
            Page("a.md", "# A\n\nOne snip here."),
            Page("b.md", "# B\n\n| Key | What |\n|---|---|\n| S | Snip |\n\n```\nsnip; snip\n```"),
            Page("c.md", "# C\n\nNothing."),
        ];

        // Act
        IReadOnlyList<HelpSearchResultModel> results = HelpSearch.Find(pages, "snip");

        // Assert
        Assert.Equal([("b.md", 3), ("a.md", 1)], results.Select(result => (result.Page, result.Count)));
    }

    [Fact]
    public void TheSnippet_IsWholeWordsAroundTheText_OnOneLine_CutWithEllipses()
    {
        // Arrange
        string before = string.Join(' ', Enumerable.Repeat("word", 20));
        string after = string.Join(' ', Enumerable.Repeat("more", 30));
        (HelpPageModel, MarkdownDocumentModel)[] pages = [Page("a.md", $"{before} the  needle\nis {after}")];

        // Act
        string snippet = Assert.Single(HelpSearch.Find(pages, "needle")).Snippet;

        // Assert
        Assert.StartsWith("…word ", snippet);
        Assert.Contains("word the needle is more", snippet);
        Assert.EndsWith(" more…", snippet);
        Assert.True(snippet.Length < 120, snippet);
    }

    [Fact]
    public void NothingToFind_FindsNothing()
    {
        Assert.Empty(HelpSearch.Find([Page("a.md", "# A")], "   "));
    }

    private static (HelpPageModel, MarkdownDocumentModel) Page(string name, string markdown) =>
        (new HelpPageModel(name, name), MarkdownParser.Parse(markdown));
}
