using System.Text.RegularExpressions;
using CincoVertice.MyTools.Ui.RegexTester.Models;
using CincoVertice.MyTools.Ui.RegexTester.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.RegexTester;

public class RegexPatternBuilderTests
{
    private static RegexPieceModel Piece(string title) => RegexPieces.All.Single(piece => piece.Title == title);

    private static RegexEditModel Insert(string title, string text, int start, int length, string sample = "") =>
        RegexPatternBuilder.Insert(new RegexPieceRequestModel(Piece(title), new RegexEditModel(text, start, length), sample));

    [Fact]
    public void NothingSelected_WritesItAtTheCaret_WithThePlaceholderSelected()
    {
        // Act: the caret after "id="
        RegexEditModel result = Insert("Starts with…", "id=", 3, 0);

        // Assert: "text" selected, to type over
        Assert.Equal(new RegexEditModel("id=^text", 4, 4), result);
    }

    [Fact]
    public void APartSelected_GoesInsideIt_AndTheCaretAfter()
    {
        // Act: "abc" selected
        RegexEditModel result = Insert("Whole word…", "x abc y", 2, 3);

        // Assert
        Assert.Equal(new RegexEditModel(@"x \babc\b y", 9, 0), result);
    }

    [Theory]
    [InlineData("Named group", "(?<name>ab)", 3, 4)]
    [InlineData("Either…or", "(?:ab|other)", 6, 5)]
    public void APartSelected_ThenTheNextPartToWriteIsSelected(string title, string text, int start, int length)
    {
        // Act
        RegexEditModel result = Insert(title, "ab", 0, 2);

        // Assert
        Assert.Equal(new RegexEditModel(text, start, length), result);
    }

    [Theory]
    [InlineData(@"\d", @"\d+")]
    [InlineData(@"\p{L}", @"\p{L}+")]
    [InlineData("[a-z]", "[a-z]+")]
    [InlineData("(ab|c)", "(ab|c)+")]
    [InlineData("ab", "(?:ab)+")]
    [InlineData(@"\d-", @"(?:\d-)+")]
    [InlineData("(a)(b)", "(?:(a)(b))+")]
    public void ACount_OnASelection_GroupsItWhenItIsntOnePart(string selected, string expected)
    {
        // Act
        RegexEditModel result = Insert("One or more", selected, 0, selected.Length);

        // Assert
        Assert.Equal(new RegexEditModel(expected, expected.Length, 0), result);
    }

    [Fact]
    public void ACountWithANumber_SelectsTheNumber()
    {
        // Act: the caret after \d
        RegexEditModel result = Insert("Exactly n times", @"\d", 2, 0);

        // Assert
        Assert.Equal(new RegexEditModel(@"\d{3}", 3, 1), result);
    }

    [Fact]
    public void TextSelectedInTheSample_IsWrittenEscaped_OverTheSelection()
    {
        // Act
        RegexEditModel result = Insert("Text selected in the sample", "^x$", 1, 1, "a.b (1)\t$5\n");

        // Assert
        Assert.Equal(@"^a\.b \(1\)\t\$5\n$", result.Text);
        Assert.Matches(new Regex(result.Text), "a.b (1)\t$5\n");
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("a", true)]
    [InlineData(".", true)]
    [InlineData("+", false)]
    [InlineData(@"\.", true)]
    [InlineData("[]a]", true)]
    [InlineData("[^]a]", true)]
    [InlineData(@"[a\]b]", true)]
    [InlineData("[a][b]", false)]
    [InlineData("(a[)]b)", true)]
    [InlineData("(a)b", false)]
    public void IsOneToken(string part, bool expected) => Assert.Equal(expected, RegexPatternBuilder.IsOneToken(part));

    [Fact]
    public void EveryPiece_WritesAValidPattern_WithItsPlaceholder_OrAroundASelection()
    {
        foreach (RegexPieceModel piece in RegexPieces.All)
        {
            // Act: after an "a", then around "ab" selected
            var request = new RegexPieceRequestModel(piece, new RegexEditModel("a", 1, 0), "x.y");
            string alone = RegexPatternBuilder.Insert(request).Text;
            string around = RegexPatternBuilder.Insert(request with { Pattern = new RegexEditModel("ab", 0, 2) }).Text;

            // Assert
            Assert.Null(Record.Exception(() => new Regex(alone)));
            Assert.Null(Record.Exception(() => new Regex(around)));
        }
    }

    [Fact]
    public void Pieces_HaveTitlesOfTheirOwn_AndADescription()
    {
        Assert.Equal(RegexPieces.All.Count, RegexPieces.All.Select(piece => piece.Title).Distinct().Count());
        Assert.All(RegexPieces.All, piece => Assert.False(string.IsNullOrWhiteSpace(piece.Description)));
    }
}
