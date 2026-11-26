using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.JsonEditor;

public class JsonDocumentReaderTests
{
    private static readonly string[] _sample =
    [
        "{",
        "  \"name\": \"Ann\",",
        "  \"tags\": [",
        "    \"a\",",
        "    \"b\", // the second",
        "  ],",
        "  \"address\": { \"zip code\": 12345 }",
        "}",
    ];

    [Fact]
    public void Read_ColorsEachPiece_NamesApartFromStrings()
    {
        // Act
        JsonDocumentModel document = JsonDocumentReader.Read(["  \"a\": [1, true, \"x\"], // note"]);

        // Assert
        Assert.Equal(
            [
                JsonTokenKind.Key, JsonTokenKind.Punctuation, JsonTokenKind.Punctuation, JsonTokenKind.Number,
                JsonTokenKind.Punctuation, JsonTokenKind.Literal, JsonTokenKind.Punctuation, JsonTokenKind.String,
                JsonTokenKind.Punctuation, JsonTokenKind.Punctuation, JsonTokenKind.Comment,
            ],
            document.Tokens[0].Select(token => token.Kind));
        Assert.Equal(new JsonTokenModel(JsonTokenKind.Key, 2, 3), document.Tokens[0][0]);
    }

    [Fact]
    public void Read_FindsTheSectionsOverSeveralLines_WithHowMuchTheyHold()
    {
        // Act
        JsonDocumentModel document = JsonDocumentReader.Read(_sample);

        // Assert: the address's object is on one line, so it doesn't collapse
        Assert.Equal(
            [new JsonFoldModel(0, 7, "{ 3 keys }"), new JsonFoldModel(2, 5, "[ 2 items ]")],
            document.Folds);
    }

    [Fact]
    public void Read_TheValues_WithTheirPlaces_CommentsAndACommaBeforeABracketAllowed()
    {
        // Act
        JsonDocumentModel document = JsonDocumentReader.Read(_sample);
        JsonNodeModel zip = document.Root!.Member("address")!.Member("zip code")!;

        // Assert
        Assert.Null(document.Error);
        Assert.Equal(12345, zip.Number);
        Assert.Equal((new DiffPosition(6, 27), new DiffPosition(6, 32)), (zip.Start, zip.End));
        Assert.Equal(new DiffPosition(6, 15), zip.KeyStart);
        Assert.Equal(["a", "b"], document.Root.Member("tags")!.Items.Select(item => item.Text));
    }

    [Theory]
    [InlineData("{\"a\": 1 \"b\": 2}", "Expected , or }, not '\"b\"'", 0, 8)]
    [InlineData("{\"a\": 1", "Expected } to close the object of line 1", 0, 7)]
    [InlineData("[1, 2", "Expected ] to close the array of line 1", 0, 5)]
    [InlineData("{a: 1}", "Expected a name in quotes, not 'a'", 0, 1)]
    [InlineData("{\"a\": tru}", "Expected a value, not 'tru'", 0, 6)]
    [InlineData("{\"a\": \"open}", "The string isn't closed", 0, 6)]
    [InlineData("1 2", "Expected the end of the text after the value", 0, 2)]
    [InlineData("{\"a\": 01}", "Expected a value, not '01'", 0, 6)]
    public void Read_InvalidJson_SaysTheFirstProblem_AndWhere(string json, string error, int line, int column)
    {
        // Act
        JsonDocumentModel document = JsonDocumentReader.Read([json]);

        // Assert
        Assert.Equal(error, document.Error);
        Assert.Equal(new DiffPosition(line, column), document.ErrorAt);
    }

    [Fact]
    public void Read_ABrokenDocument_StillCollapses()
    {
        // Act: the comma is missing after the first member
        JsonDocumentModel document = JsonDocumentReader.Read(["{", "  \"a\": 1", "  \"b\": [", "    2", "  ]", "}"]);

        // Assert
        Assert.NotNull(document.Error);
        Assert.Contains(new JsonFoldModel(2, 4, "[ 1 item ]"), document.Folds);
    }

    [Fact]
    public void Read_NoText_IsNoValue_AndNoProblem()
    {
        // Act
        JsonDocumentModel document = JsonDocumentReader.Read([string.Empty]);

        // Assert
        Assert.Null(document.Root);
        Assert.Null(document.Error);
    }

    [Fact]
    public void Read_AStringWithEscapes_IsItsText()
    {
        // Act
        JsonNodeModel value = JsonDocumentReader.Read(["\"a\\\"b\\n\\u00e9\""]).Root!;

        // Assert
        Assert.Equal("a\"b\né", value.Text);
    }
}
