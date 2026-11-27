using CincoVertice.MyTools.Ui.RegexTester.Models;
using CincoVertice.MyTools.Ui.RegexTester.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.RegexTester;

public class RegexRunnerTests
{
    [Fact]
    public void Run_GivesEachMatch_WithItsGroups_NumberedAndNamed()
    {
        // Act
        RegexResultModel result = RegexRunner.Run(
            new RegexInputModel(@"(?<user>\w+)@(\w+)\.com", "ann@example.com, bob@test.com"));

        // Assert
        Assert.Null(result.Error);
        Assert.False(result.HasMore);
        Assert.Equal(2, result.Matches.Count);
        RegexMatchModel first = result.Matches[0];
        Assert.Equal((1, 0, 15, "ann@example.com"), (first.Number, first.Index, first.Length, first.Value));
        Assert.Equal(
            [
                new RegexGroupModel(1, null, 4, 7, "example", true),
                new RegexGroupModel(2, "user", 0, 3, "ann", true),
            ],
            first.Groups);
        Assert.Equal((2, 17, "bob@test.com"), (result.Matches[1].Number, result.Matches[1].Index, result.Matches[1].Value));
    }

    [Fact]
    public void Run_AGroupThatTookNoPart_SaysSo()
    {
        // Act
        RegexResultModel result = RegexRunner.Run(new RegexInputModel("a(b)?c", "ac"));

        // Assert
        Assert.False(Assert.Single(Assert.Single(result.Matches).Groups).Success);
    }

    [Theory]
    [InlineData("ABC", "abc", false, false, false, 0)]
    [InlineData("ABC", "abc", true, false, false, 1)]
    [InlineData("^b$", "a\nb\nc", false, false, false, 0)]
    [InlineData("^b$", "a\nb\nc", false, true, false, 1)]
    [InlineData("a.b", "a\nb", false, false, false, 0)]
    [InlineData("a.b", "a\nb", false, false, true, 1)]
    public void Run_FollowsTheOptions(
        string pattern, string text, bool ignoreCase, bool multiline, bool dotAll, int matches)
    {
        // Act
        RegexResultModel result = RegexRunner.Run(new RegexInputModel(pattern, text, "", ignoreCase, multiline, dotAll));

        // Assert
        Assert.Equal(matches, result.Matches.Count);
    }

    [Fact]
    public void Run_WithAReplacement_GivesTheTextReplaced()
    {
        // Act: as .NET numbers them, the named group comes after the unnamed one, so $1 is the second word
        RegexResultModel result = RegexRunner.Run(
            new RegexInputModel(@"(?<first>\w+) (\w+)", "John Smith", "$1, ${first}"));

        // Assert
        Assert.Equal("Smith, John", result.Replaced);
        Assert.Null(RegexRunner.Run(new RegexInputModel("x", "x")).Replaced);
    }

    [Fact]
    public void Run_APatternThatIsntValid_SaysWhy()
    {
        // Act
        RegexResultModel result = RegexRunner.Run(new RegexInputModel("(a", "a", "x"));

        // Assert
        Assert.Contains("Not enough )'s", result.Error);
        Assert.Empty(result.Matches);
        Assert.Null(result.Replaced);
    }

    [Fact]
    public void Run_APatternThatTakesTooLong_IsStopped()
    {
        // Act: each a can go to either +, and the b that would end it never comes
        RegexResultModel result = RegexRunner.Run(new RegexInputModel("(a+)+b", new string('a', 40) + "c"));

        // Assert
        Assert.Contains("took more than 1 second", result.Error);
    }

    [Fact]
    public void Run_ManyMatches_GivesTheFirst_AndSaysThereAreMore()
    {
        // Act
        RegexResultModel result = RegexRunner.Run(new RegexInputModel("a", new string('a', RegexRunner.MaxMatches + 5)));

        // Assert
        Assert.Equal(RegexRunner.MaxMatches, result.Matches.Count);
        Assert.True(result.HasMore);
    }

    [Fact]
    public void Run_WithoutAPattern_GivesNothing()
    {
        // Act
        RegexResultModel result = RegexRunner.Run(new RegexInputModel("", "abc", "x"));

        // Assert
        Assert.Empty(result.Matches);
        Assert.Null(result.Error);
        Assert.Null(result.Replaced);
    }
}
