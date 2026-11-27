using CincoVertice.MyTools.Ui.Hotstrings.Models;
using CincoVertice.MyTools.Ui.Hotstrings.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Hotstrings;

public class HotstringTrackerTests
{
    private static readonly HotstringModel _btw = new("btw", "by the way", 1);
    private static readonly HotstringModel _addr1 = new("addr1", "221B Baker Street", 2);

    private readonly HotstringTracker _tracker = new();

    public HotstringTrackerTests()
    {
        _tracker.Use([_btw, _addr1]);
    }

    private HotstringModel? TypeThenEnd(string typed)
    {
        foreach (char character in typed)
        {
            if (character == '\b')
            {
                _tracker.Erase();
            }
            else if (character == '.')
            {
                _tracker.Reset();
            }
            else
            {
                _tracker.Type(character);
            }
        }

        return _tracker.End();
    }

    [Theory]
    [InlineData("btw")]
    [InlineData("BTW")]
    [InlineData("bxw\b\btw")]
    [InlineData("hello.btw")]
    public void AHotstringTypedAsAWord_IsFound_IgnoringCase(string typed)
    {
        Assert.Equal(_btw, TypeThenEnd(typed));
    }

    [Theory]
    [InlineData("abtw")]
    [InlineData("btwx")]
    [InlineData("bt")]
    [InlineData("btw.")]
    [InlineData("")]
    public void APartOfAWord_OrSomethingElse_IsNot(string typed)
    {
        Assert.Null(TypeThenEnd(typed));
    }

    [Fact]
    public void Digits_ArePartOfIt()
    {
        Assert.Equal(_addr1, TypeThenEnd("addr1"));
    }

    [Fact]
    public void AfterAnEnd_ANewWordStarts()
    {
        // Act
        HotstringModel? first = TypeThenEnd("so");
        HotstringModel? second = TypeThenEnd("btw");

        // Assert
        Assert.Null(first);
        Assert.Equal(_btw, second);
    }

    [Fact]
    public void AWordLongerThanAnyHotstring_EvenErasedBack_IsNot()
    {
        // Act: past the longest, the letters erased can't be told apart from those kept
        string typed = new string('x', HotstringTracker.MaxLength) + "btw" + new string('\b', HotstringTracker.MaxLength);

        // Assert
        Assert.Null(TypeThenEnd(typed));
    }

    [Fact]
    public void Use_OthersFromNowOn_ForgetsWhatWasTyped()
    {
        // Arrange
        _tracker.Type('b');
        _tracker.Type('t');

        // Act
        _tracker.Use([new HotstringModel("w", "with", 1)]);
        _tracker.Type('w');

        // Assert
        Assert.Equal("with", _tracker.End()?.Text);
        Assert.False(_tracker.IsEmpty);
    }

    [Fact]
    public void WithoutHotstrings_ItsEmpty()
    {
        // Act
        _tracker.Use([]);

        // Assert
        Assert.True(_tracker.IsEmpty);
        Assert.Null(TypeThenEnd("btw"));
    }
}
