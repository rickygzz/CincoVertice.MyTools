using System.Drawing;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diagramming;

public class DiagramRichTextTests
{
    private const DiagramTextFormat Bold = DiagramTextFormat.Bold;
    private const DiagramTextFormat Italic = DiagramTextFormat.Italic;

    private static DiagramTextSpanModel Span(int start, int length, DiagramTextFormat format) =>
        new(start, length, format);

    [Fact]
    public void SpansOf_MergesTheSameFormat_AndLeavesOutPlainText()
    {
        // Arrange: "ab" plain, "cd" bold, "e" bold and italic
        DiagramTextFormat[] formats = [default, default, Bold, Bold, Bold | Italic];

        // Act / Assert
        Assert.Equal([Span(2, 2, Bold), Span(4, 1, Bold | Italic)], DiagramRichText.SpansOf(formats));
    }

    [Fact]
    public void Valid_CutsSpansToTheText_AndMergesThem()
    {
        // Arrange: from a file written by hand
        DiagramTextSpanModel[] spans =
            [Span(-5, 7, Bold), Span(2, 1, Bold), Span(4, int.MaxValue, Italic), Span(1, -3, Bold), Span(9, 1, Bold)];

        // Act / Assert
        Assert.Equal([Span(0, 3, Bold), Span(4, 2, Italic)], DiagramRichText.Valid("abcdef", spans));
    }

    [Fact]
    public void Normalized_MovesTheSpansWithTheCharacters_WhenLineBreaksAndTheEndChange()
    {
        // Act: "one" plain, "\r\n", "two" bold, then spaces in bold
        DiagramRichTextModel text = DiagramRichText.Normalized("one\r\ntwo  \r\n", [Span(5, 7, Bold)]);

        // Assert
        Assert.Equal("one\ntwo", text.Text);
        Assert.Equal([Span(4, 3, Bold)], text.Spans);
    }

    [Theory]
    [InlineData("big cat", "big fat cat", 8, 3)]
    [InlineData("big cat", "big caat", 4, 4)]
    [InlineData("big cat", "big catalog", 4, 7)]
    [InlineData("big cat", "big c", 4, 1)]
    [InlineData("big cat", "a big cat", 6, 3)]
    public void AfterEdit_KeepsTheFormatOfWhatsKept_AndWhatsTypedTakesTheOneBeforeIt(
        string oldText,
        string newText,
        int start,
        int length)
    {
        // Arrange: "cat" in bold
        DiagramTextSpanModel[] spans = [Span(4, 3, Bold)];

        // Act / Assert
        Assert.Equal([Span(start, length, Bold)], DiagramRichText.AfterEdit(oldText, spans, newText));
    }

    [Fact]
    public void AfterEdit_TypedBetweenBoldAndPlain_IsBold_AndAPlainTextStaysPlain()
    {
        // Act / Assert: "big" bold, typed after it
        Assert.Equal([Span(0, 6, Bold)], DiagramRichText.AfterEdit("big cat", [Span(0, 3, Bold)], "bigger cat"));
        Assert.Empty(DiagramRichText.AfterEdit("big cat", null, "big fat cat"));
    }

    [Fact]
    public void HasFormat_CountsOnlyLetters_AndWithFormat_KeepsTheOtherFormat()
    {
        // Arrange: "go" bold, "home" bold and italic; the space between plain
        DiagramTextSpanModel[] spans = [Span(0, 2, Bold), Span(3, 4, Bold | Italic)];

        // Act / Assert
        Assert.True(DiagramRichText.HasFormat("go home", spans, Bold));
        Assert.False(DiagramRichText.HasFormat("go home", spans, Italic));
        Assert.False(DiagramRichText.HasFormat("   ", [], Bold));
        Assert.Equal([Span(3, 4, Italic)], DiagramRichText.WithFormat("go home", spans, Bold, false));
        Assert.Equal(
            [Span(0, 3, Bold), Span(3, 4, Bold | Italic)],
            DiagramRichText.WithFormat("go home", spans, Bold, true));
    }

    [Fact]
    public void Layout_PlainText_HasNoRuns_AndTheSameLinesAsBefore()
    {
        // Act
        DiagramTextBlockModel block =
            DiagramTextLayout.Layout("one two three four", new RectangleF(0, 0, 60, 100), 14);

        // Assert
        Assert.Null(block.Runs);
        Assert.Equal(DiagramTextLayout.Wrap("one two three four", 60, 14), block.Lines);
    }

    [Fact]
    public void Layout_ATextWithBoldParts_GivesEachWrappedLineItsRuns()
    {
        // Arrange: "two three" in bold, wrapped after "two" (seven characters a line at 14 pixels in 60)
        const string text = "one two three four";

        // Act
        DiagramTextBlockModel block =
            DiagramTextLayout.Layout(text, new RectangleF(0, 0, 60, 100), 14, spans: [Span(4, 9, Bold)]);

        // Assert: the spaces at the breaks are left out, as in a plain text
        Assert.Equal(["one two", "three", "four"], block.Lines);
        Assert.Equal(
            [
                [new DiagramTextRunModel("one ", default), new DiagramTextRunModel("two", Bold)],
                [new DiagramTextRunModel("three", Bold)],
                [new DiagramTextRunModel("four", default)],
            ],
            block.Runs!.Select(line => line.ToList()));
    }

    [Fact]
    public void OfLabel_HasTheRunsOfEachLine()
    {
        // Arrange: "yes" italic
        DiagramConnectorModel connector = new(
            1, 2, 3, DiagramRouting.Straight, DiagramArrowHeads.End, "ok\nyes", new DiagramStyleModel(
                Color.White, Color.Black, 1, false, Color.Black, 14),
            LabelSpans: [Span(3, 3, Italic)]);

        // Act
        DiagramTextBlockModel block = DiagramTextLayout.OfLabel(connector, new PointF(100, 100));

        // Assert
        Assert.Equal(
            [[new DiagramTextRunModel("ok", default)], [new DiagramTextRunModel("yes", Italic)]],
            block.Runs!.Select(line => line.ToList()));
    }
}
