using System.Runtime.CompilerServices;
using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Help;

public class MarkdownParserTests
{
    [Fact]
    public void Headings_HaveTheirLevel_AndAnchorsAsGitHubMakesThem()
    {
        // Act
        MarkdownDocumentModel document = MarkdownParser.Parse(
            "# My Tools Guide\n\n## Start with `Windows`\n\n### Files (and updates)\n\n## Usage\n\n## Usage ##");

        // Assert
        Assert.Equal("My Tools Guide", document.Title);
        Assert.Equal([1, 2, 3, 2, 2], document.Blocks.Select(block => block.Level));
        Assert.Equal(
            ["my-tools-guide", "start-with-windows", "files-and-updates", "usage", "usage-1"],
            document.Blocks.Select(block => block.Anchor));
    }

    [Fact]
    public void AParagraph_IsItsLinesInOne_AndBreaksWhereALineEndsInTwoSpaces()
    {
        // Act
        MarkdownBlockModel block = Assert.Single(MarkdownParser.Parse("One\ntwo  \nthree").Blocks);

        // Assert
        Assert.Equal(MarkdownBlockKind.Paragraph, block.Kind);
        Assert.Equal("One two\nthree", block.PlainText);
    }

    [Fact]
    public void AnUnderlinedLine_IsAHeading()
    {
        // Act
        IReadOnlyList<MarkdownBlockModel> blocks = MarkdownParser.Parse("Title\n=====\n\nPart\n---").Blocks;

        // Assert
        Assert.Equal([(1, "Title"), (2, "Part")], blocks.Select(block => (block.Level, block.PlainText)));
        Assert.All(blocks, block => Assert.Equal(MarkdownBlockKind.Heading, block.Kind));
    }

    [Fact]
    public void Lists_InsideLists_HaveTheirDepth_AndBulletsOrNumbers()
    {
        // Act
        IReadOnlyList<MarkdownBlockModel> blocks = MarkdownParser.Parse(
            "- One\n  - Inside\n    more of it\n- Two\n\n1. First\n2) Second").Blocks;

        // Assert
        Assert.All(blocks, block => Assert.Equal(MarkdownBlockKind.ListItem, block.Kind));
        Assert.Equal(
            [
                (0, "•", "One"), (1, "◦", "Inside more of it"), (0, "•", "Two"), (0, "1.", "First"),
                (0, "2.", "Second"),
            ],
            blocks.Select(block => (block.Level, block.Marker, block.PlainText)));
    }

    [Fact]
    public void AParagraphIndentedUnderAListItem_IsInIt_AndOneThatIsnt_EndsTheList()
    {
        // Act
        IReadOnlyList<MarkdownBlockModel> blocks = MarkdownParser.Parse(
            "1. Step\n\n   More about it.\n\n   ```\n   code\n   ```\n\nAfter.").Blocks;

        // Assert
        Assert.Equal(
            [
                (MarkdownBlockKind.ListItem, 0, "Step"),
                (MarkdownBlockKind.Paragraph, 1, "More about it."),
                (MarkdownBlockKind.CodeBlock, 1, "code"),
                (MarkdownBlockKind.Paragraph, 0, "After."),
            ],
            blocks.Select(block => (block.Kind, block.Level, block.PlainText)));
    }

    [Fact]
    public void ACodeBlock_KeepsItsLinesAsWritten_WithItsLanguage()
    {
        // Act
        MarkdownBlockModel block = Assert.Single(
            MarkdownParser.Parse("```mermaid\nflowchart TD\n    A --> B\n\n# not a heading\n```").Blocks);

        // Assert
        Assert.Equal(MarkdownBlockKind.CodeBlock, block.Kind);
        Assert.Equal("mermaid", block.Language);
        Assert.Equal("flowchart TD\n    A --> B\n\n# not a heading", block.PlainText);
        Assert.Equal(MarkdownInlineStyle.Code, Assert.Single(block.Inlines).Style);
    }

    [Fact]
    public void AnIndentedBlock_IsCode()
    {
        // Act
        IReadOnlyList<MarkdownBlockModel> blocks = MarkdownParser.Parse("Text.\n\n    x = 1\n      y\n\nMore.").Blocks;

        // Assert
        Assert.Equal(
            [
                (MarkdownBlockKind.Paragraph, "Text."),
                (MarkdownBlockKind.CodeBlock, "x = 1\n  y"),
                (MarkdownBlockKind.Paragraph, "More."),
            ],
            blocks.Select(block => (block.Kind, block.PlainText)));
    }

    [Fact]
    public void ATable_IsItsRowsOfCells_TheHeaderFirst()
    {
        // Act
        MarkdownBlockModel table = Assert.Single(MarkdownParser.Parse(
            "| Action | Shortcut |\n|---|:---:|\n| `Snip` | Ctrl+Alt+S |\n| `a \\| b` | **none** | extra |\n| short |")
            .Blocks);

        // Assert: every row as wide as the header
        Assert.Equal(MarkdownBlockKind.Table, table.Kind);
        Assert.Equal(
            [["Action", "Shortcut"], ["Snip", "Ctrl+Alt+S"], ["a | b", "none"], ["short", ""]],
            table.Rows!.Select(row => row.Select(cell => string.Concat(cell.Select(piece => piece.Text)))));
        Assert.Equal(MarkdownInlineStyle.Code, table.Rows![1][0][0].Style);
        Assert.Equal(MarkdownInlineStyle.Bold, table.Rows![2][1][0].Style);
    }

    [Fact]
    public void QuotesAndRules_AreTheirOwnBlocks()
    {
        // Act
        IReadOnlyList<MarkdownBlockModel> blocks = MarkdownParser.Parse("> Note:\n> read this\n\n---\n\n***").Blocks;

        // Assert
        Assert.Equal(
            [(MarkdownBlockKind.Quote, "Note: read this"), (MarkdownBlockKind.Rule, ""), (MarkdownBlockKind.Rule, "")],
            blocks.Select(block => (block.Kind, block.PlainText)));
    }

    [Fact]
    public void Inlines_HaveTheirStyles_AndLinks()
    {
        // Act
        IReadOnlyList<MarkdownInlineModel> inlines = MarkdownInlineParser.Parse(
            "Press **Ctrl+S** to *save*, see [the **hotkeys**](02-hotkeys.md#how) or `a*b*c`, ***both***.");

        // Assert
        Assert.Equal(
            [
                new MarkdownInlineModel("Press "),
                new MarkdownInlineModel("Ctrl+S", MarkdownInlineStyle.Bold),
                new MarkdownInlineModel(" to "),
                new MarkdownInlineModel("save", MarkdownInlineStyle.Italic),
                new MarkdownInlineModel(", see "),
                new MarkdownInlineModel("the ", Link: "02-hotkeys.md#how"),
                new MarkdownInlineModel("hotkeys", MarkdownInlineStyle.Bold, "02-hotkeys.md#how"),
                new MarkdownInlineModel(" or "),
                new MarkdownInlineModel("a*b*c", MarkdownInlineStyle.Code),
                new MarkdownInlineModel(", "),
                new MarkdownInlineModel("both", MarkdownInlineStyle.Bold | MarkdownInlineStyle.Italic),
                new MarkdownInlineModel("."),
            ],
            inlines);
    }

    [Theory]
    [InlineData("snake_case_name stays", "snake_case_name stays")]
    [InlineData("2 * 3 * 4", "2 * 3 * 4")]
    [InlineData(@"\*not italic\*", "*not italic*")]
    [InlineData("a<br>b", "a\nb")]
    [InlineData("Tom &amp; Jerry &lt;3", "Tom & Jerry <3")]
    [InlineData("<name> and [brackets]", "<name> and [brackets]")]
    [InlineData("**unclosed", "**unclosed")]
    public void Inlines_ThatArentMarkup_StayAsWritten(string markdown, string expected)
    {
        // Act
        IReadOnlyList<MarkdownInlineModel> inlines = MarkdownInlineParser.Parse(markdown);

        // Assert
        Assert.Equal(new MarkdownInlineModel(expected), Assert.Single(inlines));
    }

    [Fact]
    public void WebAddresses_AreLinks_WithoutThePunctuationAfterThem()
    {
        // Act
        IReadOnlyList<MarkdownInlineModel> inlines = MarkdownInlineParser.Parse(
            "See https://example.com/a. Or <https://example.org>.");

        // Assert
        Assert.Equal(
            [
                new MarkdownInlineModel("See "),
                new MarkdownInlineModel("https://example.com/a", Link: "https://example.com/a"),
                new MarkdownInlineModel(". Or "),
                new MarkdownInlineModel("https://example.org", Link: "https://example.org"),
                new MarkdownInlineModel("."),
            ],
            inlines);
    }

    [Theory]
    [InlineData("02-hotkeys.md", "02-hotkeys.md", null, null)]
    [InlineData("02-hotkeys.md#default-hotkeys", "02-hotkeys.md", "default-hotkeys", null)]
    [InlineData("./13-window-zones.md#", "13-window-zones.md", null, null)]
    [InlineData("#usage", null, "usage", null)]
    [InlineData("My%20Page.md", "My Page.md", null, null)]
    [InlineData("https://example.com/#x", null, null, "https://example.com/#x")]
    public void Links_GoToAPage_AHeading_OrTheWeb(string target, string? page, string? anchor, string? url)
    {
        Assert.Equal(new HelpLinkModel(page, anchor, url), HelpLinkModel.Parse(target));
    }

    [Fact]
    public void EveryLinkOfTheUserGuide_GoesToAPageAndHeadingItHas()
    {
        // Arrange: the guide in the repository
        string folder = UserGuideFolder();
        Dictionary<string, MarkdownDocumentModel> pages = Directory.GetFiles(folder, "*.md").ToDictionary(
            path => Path.GetFileName(path),
            path => MarkdownParser.Parse(File.ReadAllText(path)),
            StringComparer.OrdinalIgnoreCase);
        List<string> broken = [];

        // Act
        foreach ((string name, MarkdownDocumentModel document) in pages)
        {
            foreach (string target in LinksOf(document))
            {
                HelpLinkModel link = HelpLinkModel.Parse(target);

                if (link.Url is not null)
                {
                    continue;
                }

                if (!pages.TryGetValue(link.Page ?? name, out MarkdownDocumentModel? linked)
                    || link.Anchor is not null && linked.Blocks.All(block => block.Anchor != link.Anchor))
                {
                    broken.Add($"{name}: {target}");
                }
            }
        }

        // Assert
        Assert.NotEmpty(pages);
        Assert.Empty(broken);
    }

    private static IEnumerable<string> LinksOf(MarkdownDocumentModel document) =>
        document.Blocks
            .SelectMany(block =>
                block.Inlines.Concat(block.Rows?.SelectMany(row => row.SelectMany(cell => cell)) ?? []))
            .Select(inline => inline.Link)
            .OfType<string>();

    // Above this file, wherever the tests were built
    private static string UserGuideFolder([CallerFilePath] string source = "")
    {
        for (DirectoryInfo? folder = new FileInfo(source).Directory; folder is not null; folder = folder.Parent)
        {
            string guide = Path.Combine(folder.FullName, "docs", "user-guide");

            if (Directory.Exists(guide))
            {
                return guide;
            }
        }

        throw new DirectoryNotFoundException("docs\\user-guide isn't in any folder above the tests.");
    }
}
