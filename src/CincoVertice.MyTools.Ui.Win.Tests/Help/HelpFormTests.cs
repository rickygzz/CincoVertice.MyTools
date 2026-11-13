using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Help.Models;
using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.Help;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Help;

/// <summary>The help window with the real user guide, shown for a moment out of sight.</summary>
public sealed class HelpFormTests
{
    [Fact]
    public void EveryPageOfTheGuide_HasItsLinksWhereTheirTextIs_AndItsHeadings()
    {
        // Arrange
        var store = new HelpPageStore(UserGuideFolder());
        using HelpForm form = ShownForm(new Point(-3000, 100));
        RichTextBox page = PageOf(form);
        List<string> wrong = [];

        foreach (string name in store.GetPageNames())
        {
            MarkdownDocumentModel document = MarkdownParser.Parse(store.Read(name)!);

            // Act
            form.ShowPage(new HelpPageModel(name, document.Title ?? name), document, null);

            // Assert: no mark left, each link on its own text, each heading where its text starts
            if (page.Find(MarkdownRtfWriter.Mark.ToString(), RichTextBoxFinds.MatchCase) >= 0)
            {
                wrong.Add($"{name}: a mark is left");
            }

            List<string> expected = LinkTextsOf(document);
            List<string> shown = [.. form.Links.Select(link => TextAt(page, link.Start, link.Length))];

            if (!expected.SequenceEqual(shown))
            {
                wrong.Add($"{name}: links [{string.Join(", ", shown)}], not [{string.Join(", ", expected)}]");
            }

            foreach (MarkdownBlockModel heading in document.Blocks.Where(
                block => block.Kind == MarkdownBlockKind.Heading))
            {
                int? position = form.PositionOf(heading.Anchor!);

                if (position is null || TextAt(page, position.Value, heading.PlainText.Length) != heading.PlainText)
                {
                    wrong.Add($"{name}: heading {heading.Anchor} isn't where its text is");
                }
            }
        }

        Assert.Empty(wrong);
    }

    [Fact]
    public void ALink_IsWhereItsTextIs_AndNotPastTheEndOfItsLine()
    {
        // Arrange
        using HelpForm form = ShownForm(new Point(-3000, 500));
        MarkdownDocumentModel document = MarkdownParser.Parse(
            "# Guide\n\nSee [the hotkeys](02-hotkeys.md#usage)\n\nText.");
        form.ShowPage(new HelpPageModel("README.md", "Guide"), document, null);
        RichTextBox page = PageOf(form);
        (int start, int length, string target) = Assert.Single(form.Links);
        Point first = page.GetPositionFromCharIndex(start);
        Point last = page.GetPositionFromCharIndex(start + length - 1);

        // Act
        string? onIt = LinkAt(form, first + new Size(2, 4));
        string? onItsEnd = LinkAt(form, last + new Size(2, 4));
        string? pastIt = LinkAt(form, new Point(page.ClientSize.Width - 10, first.Y + 4));
        string? onText = LinkAt(form, page.GetPositionFromCharIndex(start - 3) + new Size(2, 4));

        // Assert
        Assert.Equal("02-hotkeys.md#usage", target);
        Assert.Equal(target, onIt);
        Assert.Equal(target, onItsEnd);
        Assert.Null(pastIt);
        Assert.Null(onText);
    }

    [Fact]
    public void TabGoesFromLinkToLink_RoundTheEnds_AndEnterOpensTheOneSelected()
    {
        // Arrange
        using HelpForm form = ShownForm(new Point(-3000, 700));
        MarkdownDocumentModel document = MarkdownParser.Parse("[One](01.md) text [Two](02.md#b) more [Three](#c)");
        form.ShowPage(new HelpPageModel("README.md", "Guide"), document, null);
        RichTextBox page = PageOf(form);

        // Act and assert: forward, then round to the first, then back round to the last
        Assert.Equal(["One", "Two", "Three", "One"], Enumerable.Range(0, 4).Select(_ => Tab(form, page, true)));
        Assert.Equal("Three", Tab(form, page, false));
        Assert.Equal("#c", form.LinkAtCaret());
    }

    [Fact]
    public void F1_InAControlOfTheForm_OpensItsTopic()
    {
        // Arrange
        using var form = new Form();
        var box = new TextBox();
        form.Controls.Add(box);
        List<string> opened = [];
        form.OpenHelpOnF1(() => "04-snipping.md#recordingcfg", opened.Add);

        // Act: what F1 in the text box does
        typeof(Control).GetMethod("OnHelpRequested", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(box, [new HelpEventArgs(Point.Empty)]);

        // Assert
        Assert.Equal(["04-snipping.md#recordingcfg"], opened);
    }

    [Fact]
    public void AFoundText_IsSelected_AndEscClearsIt_BeforeItClosesTheWindow()
    {
        // Arrange
        using HelpForm form = ShownForm(new Point(-3000, 300));
        MarkdownDocumentModel document = MarkdownParser.Parse("# Guide\n\nSome text.\n\nThe needle is here.");
        form.ShowPage(new HelpPageModel("README.md", "Guide"), document, null);

        // Act
        form.HighlightText("needle");
        string found = form.Page.SelectedText;
        bool isCleared = form.ClearFindOrSearch();

        // Assert: cleared once, then there's nothing more to clear
        Assert.Equal("needle", found);
        Assert.True(isCleared);
        Assert.Equal(0, form.Page.SelectionLength);
        Assert.False(form.ClearFindOrSearch());
    }

    [Fact]
    public void SearchResults_ReplaceThePages_AndNoneSaysSo()
    {
        // Arrange
        using HelpForm form = ShownForm(new Point(-3000, 300));
        var result = new HelpSearchResultModel("a.md", "A", "…the needle…", 2);

        // Act and assert
        form.ShowSearchResults("needle", [result]);
        Assert.Equal([result], form.ListItems);

        form.ShowSearchResults("hay", []);
        Assert.Equal(["No page has \"hay\"."], form.ListItems);

        form.ShowPages([new HelpPageModel("a.md", "A")]);
        Assert.Equal([new HelpPageModel("a.md", "A")], form.ListItems);
    }

    [Theory]
    [InlineData(false, @"\red32\green32\blue32")]
    [InlineData(true, @"\red222\green222\blue222")]
    public void ThePage_IsInTheColorsOfWindowsMode(bool isDark, string textColor)
    {
        // Act
        string rtf = MarkdownRtfWriter.Write(MarkdownParser.Parse("Text."), 9000, isDark).Rtf;

        // Assert
        Assert.Contains(@"{\colortbl ;" + textColor, rtf);
    }

    [Fact]
    public void QuotesAreQuieter_AndTableColumnsLineUpAsWritten()
    {
        // Act
        string rtf = MarkdownRtfWriter.Write(
            MarkdownParser.Parse("> Quoted.\n\n| a | b | c |\n|---|:-:|--:|\n| 1 | 2 | 3 |"),
            9000).Rtf;

        // Assert
        Assert.Contains(@"\sa120\cf4 {Quoted.}\cf1\par", rtf);
        Assert.Contains(@"\ql\b {a}", rtf);
        Assert.Contains(@"\qc\b {b}", rtf);
        Assert.Contains(@"\qr {3}", rtf);
    }

    [Fact]
    public void TheLongestPage_IsShownQuickly()
    {
        // Arrange
        var store = new HelpPageStore(UserGuideFolder());
        string name = store.GetPageNames().MaxBy(page => store.Read(page)!.Length)!;
        MarkdownDocumentModel document = MarkdownParser.Parse(store.Read(name)!);
        using HelpForm form = ShownForm(new Point(-3000, 900));

        // Act
        var watch = Stopwatch.StartNew();
        form.ShowPage(new HelpPageModel(name, name), document, null);
        watch.Stop();

        // Assert
        Assert.True(watch.ElapsedMilliseconds < 1500, $"{name} took {watch.ElapsedMilliseconds} ms");
    }

    private static RichTextBox PageOf(HelpForm form) => form.Page;

    private static string? LinkAt(HelpForm form, Point point) => form.LinkAt(point);

    // What Tab or Shift+Tab in the page does: the link it selects
    private static string Tab(HelpForm form, RichTextBox page, bool forward)
    {
        form.SelectLink(forward);

        return page.SelectedText;
    }

    // Through the box itself: its positions count a table's cells as Text doesn't
    private static string TextAt(RichTextBox page, int start, int length)
    {
        page.Select(start, length);

        return page.SelectedText;
    }

    private static HelpForm ShownForm(Point location)
    {
        var form = new HelpForm { StartPosition = FormStartPosition.Manual, Location = location };
        form.Show();

        return form;
    }

    // The text of each link, in order: its pieces next to each other with the same target are one link
    private static List<string> LinkTextsOf(MarkdownDocumentModel document)
    {
        List<string> texts = [];

        foreach (MarkdownBlockModel block in document.Blocks)
        {
            IEnumerable<IReadOnlyList<MarkdownInlineModel>> lines = block.Rows is null
                ? [block.Inlines]
                : block.Rows.SelectMany(row => row);

            foreach (IReadOnlyList<MarkdownInlineModel> inlines in lines)
            {
                for (int i = 0; i < inlines.Count; i++)
                {
                    if (inlines[i].Link is null)
                    {
                        continue;
                    }

                    if (i > 0 && inlines[i - 1].Link == inlines[i].Link)
                    {
                        texts[^1] += inlines[i].Text;
                    }
                    else
                    {
                        texts.Add(inlines[i].Text);
                    }
                }
            }
        }

        return texts;
    }

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
