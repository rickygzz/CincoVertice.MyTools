using System.Globalization;
using CincoVertice.MyTools.Ui.TextTransform.Models;
using CincoVertice.MyTools.Ui.TextTransform.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.TextTransform;

public sealed class TextTransformerTests : IDisposable
{
    // Case changes follow the user's culture; these expect English's
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;

    public TextTransformerTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
    }

    [Theory]
    [InlineData(TextTransformKind.Upper, "Hello wörld", "HELLO WÖRLD")]
    [InlineData(TextTransformKind.Lower, "Hello WÖRLD", "hello wörld")]
    [InlineData(TextTransformKind.Title, "the QUICK brown fox", "The Quick Brown Fox")]
    [InlineData(TextTransformKind.Base64Encode, "hello ñ", "aGVsbG8gw7E=")]
    [InlineData(TextTransformKind.Base64Decode, "aGVsbG8gw7E=", "hello ñ")]
    [InlineData(TextTransformKind.UrlEncode, "a b&c=d/é", "a%20b%26c%3Dd%2F%C3%A9")]
    [InlineData(TextTransformKind.UrlDecode, "a%20b+c%26d", "a b c&d")]
    public void Apply_ChangesTheText(TextTransformKind kind, string text, string expected)
    {
        // Act / Assert
        Assert.Equal(expected, TextTransformer.Apply(kind, text));
    }

    [Theory]
    [InlineData(TextTransformKind.SortLines, "pear\r\nApple\r\nbanana\r\n", "Apple\r\nbanana\r\npear\r\n")]
    [InlineData(TextTransformKind.SortLines, "b\na\n", "a\nb\n")]
    [InlineData(TextTransformKind.SortLines, "b\r\na", "a\r\nb")]
    [InlineData(TextTransformKind.RemoveDuplicateLines, "a\nb\na\nB\nb", "a\nb\nB")]
    [InlineData(TextTransformKind.TrimLines, "\n  a  \n\tb\n\n  \n", "a\nb\n")]
    [InlineData(TextTransformKind.TrimLines, "  one line  ", "one line")]
    public void Apply_ToLines_KeepsTheLineEndings_AndTheOneAtTheEnd(TextTransformKind kind, string text, string expected)
    {
        // Act / Assert
        Assert.Equal(expected, TextTransformer.Apply(kind, text));
    }

    [Fact]
    public void Apply_JsonPrettyPrint_IndentsByTwo_AndKeepsLettersAsThemselves()
    {
        // Act
        string json = TextTransformer.Apply(TextTransformKind.JsonPrettyPrint, "{\"name\":\"José\",\"tags\":[1,2]}");

        // Assert
        Assert.Equal("{\r\n  \"name\": \"José\",\r\n  \"tags\": [\r\n    1,\r\n    2\r\n  ]\r\n}", json);
    }

    [Fact]
    public void Apply_JsonMinify_TakesCommentsAndTrailingCommas()
    {
        // Act
        string json = TextTransformer.Apply(
            TextTransformKind.JsonMinify,
            "{\n  // the name\n  \"name\": \"José\",\n  \"tags\": [1, 2,],\n}\n");

        // Assert: the line break at the end stays
        Assert.Equal("{\"name\":\"José\",\"tags\":[1,2]}\n", json);
    }

    [Theory]
    [InlineData(TextTransformKind.JsonPrettyPrint, "{\"a\": }", "That isn't JSON at line 1")]
    [InlineData(TextTransformKind.JsonMinify, "hello", "That isn't JSON")]
    [InlineData(TextTransformKind.Base64Decode, "#!", "That isn't base64.")]
    [InlineData(TextTransformKind.Base64Decode, "//79", "That base64 isn't text")]
    public void Apply_ToWhatItCantRead_SaysWhy(TextTransformKind kind, string text, string message)
    {
        // Act
        var error = Assert.Throws<FormatException>(() => TextTransformer.Apply(kind, text));

        // Assert
        Assert.StartsWith(message, error.Message);
    }

    [Fact]
    public void All_HasEveryTransform_EachWithItsOwnLetter()
    {
        // Act
        IEnumerable<char> letters = TextTransformer.All.Select(t => char.ToUpperInvariant(t.Title[t.Title.IndexOf('&') + 1]));

        // Assert
        Assert.Equal(Enum.GetValues<TextTransformKind>(), TextTransformer.All.Select(t => t.Kind));
        Assert.Equal(TextTransformer.All.Count, letters.Distinct().Count());
    }
}
