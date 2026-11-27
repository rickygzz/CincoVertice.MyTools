using CincoVertice.MyTools.Ui.RegexTester.Models;
using CincoVertice.MyTools.Ui.RegexTester.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.RegexTester;

public class RegexExamplesTests
{
    public static TheoryData<string> Titles => [.. RegexExamples.All.Select(example => example.Title)];

    private static RegexResultModel Run(RegexExampleModel example) => RegexRunner.Run(new RegexInputModel(
        example.Pattern,
        example.Sample,
        example.Replacement,
        example.IgnoreCase,
        example.Multiline,
        example.DotAll));

    [Theory]
    [MemberData(nameof(Titles))]
    public void EachExample_IsValid_AndMatchesInItsSample(string title)
    {
        // Act
        RegexExampleModel example = RegexExamples.All.Single(e => e.Title == title);
        RegexResultModel result = Run(example);

        // Assert
        Assert.Null(result.Error);
        Assert.NotEmpty(result.Matches);
        Assert.False(string.IsNullOrWhiteSpace(example.Description));
        Assert.Equal(example.Replacement.Length > 0, result.Replaced is not null);
    }

    [Theory]
    [InlineData("Email address", "ann.lee@example.com", "bob+news@mail.co.uk")]
    [InlineData("Web address (URL)", "https://example.com/docs?page=2", "http://test.org")]
    [InlineData("Phone number", "(555) 123-4567", "555.987.6543", "5551112222")]
    [InlineData("Date, day first: DD/MM/YYYY", "5/3/1990", "21/09/2015")]
    [InlineData("Time: HH:MM or HH:MM:SS", "9:00", "17:30", "23:59:59")]
    [InlineData("Amount of money", "$3,332,000.00", "$ 45.10", "$7")]
    [InlineData("IP address (IPv4)", "192.168.1.10", "10.0.0.1")]
    [InlineData("Color: #RGB or #RRGGBB", "#fff", "#1E90FF")]
    [InlineData("Text in quotes", "\"hello\"", "\"a \\\"quoted\\\" word\"")]
    [InlineData("Blank lines", "\n", "   \n", "\n")]
    [InlineData("A line starting with a word", "ERROR disk full", "WARNING low memory")]
    public void Examples_MatchWhatTheySay_AndNotWhatTheirSampleSaysIsntOne(string title, params string[] matches)
    {
        // Act
        RegexResultModel result = Run(RegexExamples.All.Single(e => e.Title == title));

        // Assert
        Assert.Equal(matches, result.Matches.Select(match => match.Value));
    }

    [Fact]
    public void Examples_HaveTitlesOfTheirOwn() =>
        Assert.Equal(RegexExamples.All.Count, RegexExamples.All.Select(e => e.Title).Distinct().Count());
}
