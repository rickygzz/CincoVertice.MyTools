using CincoVertice.MyTools.Ui.Hotstrings.Models;
using CincoVertice.MyTools.Ui.Hotstrings.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Hotstrings;

public class HotstringReaderTests
{
    [Fact]
    public void Read_NamesAndTexts_WithCommentsAndTextsOfSeveralLines()
    {
        // Act
        HotstringsResultModel result = HotstringReader.Read(
            "# Hotstrings\r\n"
            + "btw = by the way\r\n"
            + "addr1 = 221B Baker Street, London   \r\n"
            + "sig = [\"Best regards,\r\n"
            + "  Ricky\"]\r\n"
            + "tag = #1 # not a comment: the whole text\r\n");

        // Assert
        Assert.Empty(result.Problems);
        Assert.Equal(
            [
                new HotstringModel("btw", "by the way", 2),
                new HotstringModel("addr1", "221B Baker Street, London", 3),
                new HotstringModel("sig", "Best regards,\n  Ricky", 4),
                new HotstringModel("tag", "#1 # not a comment: the whole text", 6),
            ],
            result.Hotstrings);
    }

    [Theory]
    [InlineData("my_sig = x", 1, "my_sig: only letters and digits can be typed as a hotstring.")]
    [InlineData("btw =", 1, "btw: what does it become? btw = text.")]
    [InlineData("btw = x\nBTW = y", 2, "BTW: already on line 1; that one is used.")]
    [InlineData("just text", 1, "'just text' is not 'abbreviation = text'.")]
    [InlineData("sig = [\"Best\nx = y", 1, "sig: the [\" is never closed with \"], so the rest of the file is ignored.")]
    public void Read_ALineThatCantBeUsed_IsAProblem_AndSkipped(string text, int line, string message)
    {
        // Act
        HotstringsResultModel result = HotstringReader.Read(text);

        // Assert
        Assert.Equal(new HotstringProblemModel(line, message), Assert.Single(result.Problems));
        Assert.True(result.Hotstrings.Count <= 1);
    }

    [Fact]
    public void Read_ANameTooLong_IsAProblem()
    {
        // Act
        HotstringsResultModel result = HotstringReader.Read(new string('a', HotstringTracker.MaxLength + 1) + " = x");

        // Assert
        Assert.Contains($"at most {HotstringTracker.MaxLength} letters", Assert.Single(result.Problems).Message);
        Assert.Empty(result.Hotstrings);
    }
}
