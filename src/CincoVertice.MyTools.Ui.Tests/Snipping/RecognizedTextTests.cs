using CincoVertice.MyTools.Ui.Snipping.Models;
using CincoVertice.MyTools.Ui.Snipping.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Snipping;

public class RecognizedTextTests
{
    private const double LetterWidth = 8;

    /// <summary>A line as read: its words one space apart, the first this many letters from the left.</summary>
    private static RecognizedLineModel Line(int indent, string text)
    {
        List<RecognizedWordModel> words = [];
        int column = indent;

        foreach (string word in text.Split(' '))
        {
            words.Add(new RecognizedWordModel(word, column * LetterWidth, word.Length * LetterWidth));
            column += word.Length + 1;
        }

        return new RecognizedLineModel(words);
    }

    private static string Clean(string text) =>
        string.Join(' ', RecognizedText.Clean(Line(0, text).Words).Select(word => word.Text));

    [Theory]
    [InlineData("/// • • •Pastes •text into the • active window", "/// Pastes text into the active window")]
    [InlineData("/// ' </summary>", "/// </summary>")]
    [InlineData("'then 'Puts back what • the clipboard held", "'then Puts back what the clipboard held")]
    [InlineData("Task PressAsync(string keys, •int •times);", "Task PressAsync(string keys, int times);")]
    [InlineData("none or• it has · no title", "none or it has no title")]
    public void Clean_DropsTheDotsReadFromSpacesDrawnAsDots(string read, string expected)
    {
        Assert.Equal(expected, Clean(read));
    }

    [Theory]
    [InlineData("• First point")]
    [InlineData("He said 'quoted' and it's fine")]
    [InlineData("char c = 'a';")]
    [InlineData("' A Visual Basic comment")]
    public void Clean_KeepsBulletsQuotesAndApostrophes(string text)
    {
        Assert.Equal(text, Clean(text));
    }

    [Fact]
    public void Clean_OfALineOfDotsOnly_LeavesNothing()
    {
        Assert.Empty(Clean("• • ' •"));
    }

    [Fact]
    public void Format_IndentsEachLineAsFarAsItIs_FromTheLineFurthestLeft()
    {
        // Act
        IReadOnlyList<string> lines = RecognizedText.Format(
        [
            Line(4, "if (ready)"),
            Line(4, "{"),
            Line(8, "Run();"),
            Line(4, "}"),
        ]);

        // Assert
        Assert.Equal(["if (ready)", "{", "    Run();", "}"], lines);
    }

    [Fact]
    public void Format_RoundsToTheNearestLetter()
    {
        // Act: a little off, as read
        IReadOnlyList<string> lines = RecognizedText.Format(
        [
            Line(0, "first line"),
            new RecognizedLineModel([new RecognizedWordModel("second", 2.6 * LetterWidth, 6 * LetterWidth)]),
            new RecognizedLineModel([new RecognizedWordModel("third", 0.4 * LetterWidth, 5 * LetterWidth)]),
        ]);

        // Assert
        Assert.Equal(["first line", "   second", "third"], lines);
    }

    [Fact]
    public void Format_IndentsFromTheFirstWordLeft_AfterTheDots()
    {
        // Act: the dots of the indentation are read as words
        IReadOnlyList<string> lines = RecognizedText.Format(
        [
            Line(0, "class A"),
            Line(0, "• • • • Run();"),
        ]);

        // Assert
        Assert.Equal(["class A", "        Run();"], lines);
    }

    [Theory]
    [InlineData("the clipboard held .", "the clipboard held.")]
    [InlineData("Task PasteTextAsync(string text) ;", "Task PasteTextAsync(string text);")]
    [InlineData("one , two : three ; four ! five ? six", "one, two: three; four! five? six")]
    [InlineData("wait ...", "wait...")]
    [InlineData("a . b", "a. b")]
    public void Format_PunctuationReadAsAWord_GoesAgainstTheWordBefore(string read, string expected)
    {
        Assert.Equal([expected], RecognizedText.Format([Line(0, read)]));
    }

    [Fact]
    public void Format_LeavesOutLinesWithNothingLeft()
    {
        // Act
        IReadOnlyList<string> lines = RecognizedText.Format([Line(0, "text"), Line(0, "• •")]);

        // Assert
        Assert.Equal(["text"], lines);
    }

    [Fact]
    public void Format_OfNothing_IsNothing()
    {
        Assert.Empty(RecognizedText.Format([]));
    }
}
