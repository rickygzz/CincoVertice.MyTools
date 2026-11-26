using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.JsonEditor;

public class JsonPathAndSuggestionTests
{
    private static readonly string[] _lines =
    [
        "{",
        "  \"people\": [",
        "    { \"name\": \"Ann\", \"age\": 30, \"first name\": \"A\" },",
        "    { \"name\": \"Bob\", \"age\": 20 }",
        "  ],",
        "  \"count\": 2",
        "}",
    ];

    private static readonly JsonNodeModel _root = JsonDocumentReader.Read(_lines).Root!;

    [Theory]
    [InlineData(3, 16, "people[1].name")]
    [InlineData(3, 7, "people[1].name")]
    [InlineData(2, 39, "people[0].\"first name\"")]
    [InlineData(3, 4, "people[1]")]
    [InlineData(5, 11, "count")]
    [InlineData(0, 0, "@")]
    public void PathOf_TheValueAtAPlace_ItsNameIncluded(int line, int column, string path)
    {
        // Act
        JsonNodeModel node = JsonPathWriter.NodeAt(_root, new DiffPosition(line, column))!;

        // Assert
        Assert.Equal(path, JsonPathWriter.PathOf(node));
    }

    [Theory]
    [InlineData("", "people", "count")]
    [InlineData("pe", "people")]
    [InlineData("people[0].", "name", "age", "first name")]
    [InlineData("people[*].n", "name")]
    [InlineData("people[?a", "age", "abs()", "avg()")]
    [InlineData("people[?age > `25` && na", "name")]
    [InlineData("people | [0].", "name", "age", "first name")]
    [InlineData("len", "length()")]
    [InlineData("count.", new string[0])]
    public void Suggest_JmesPath_TheNamesThereAre_WhereTheExpressionEnds(string query, params string[] expected)
    {
        // Act
        IReadOnlyList<JsonSuggestionModel> suggestions = JsonSuggester.Suggest(JsonSearchMode.JmesPath, query, _root);

        // Assert
        Assert.Equal(expected, suggestions.Select(suggestion => suggestion.Text));
    }

    [Fact]
    public void Suggest_JmesPath_AQuotedName_ReplacesWhatsTyped()
    {
        // Act
        JsonSuggestionModel first = JsonSuggester.Suggest(JsonSearchMode.JmesPath, "people[0].fi", _root).Single();

        // Assert
        Assert.Equal(("\"first name\"", 10, "string \"A\""), (first.Insert, first.Start, first.Detail));
    }

    [Fact]
    public void Suggest_Text_TheNamesAndValuesHoldingIt()
    {
        // Act / Assert
        Assert.Equal(
            ["name", "first name"],
            JsonSuggester.Suggest(JsonSearchMode.Text, "NAM", _root).Select(suggestion => suggestion.Text));
    }

    [Fact]
    public void FindText_EachPlace_IgnoringCase()
    {
        // Act / Assert
        Assert.Equal(
            [
                new JsonMatchModel(new DiffPosition(2, 7), new DiffPosition(2, 11)),
                new JsonMatchModel(new DiffPosition(2, 39), new DiffPosition(2, 43)),
                new JsonMatchModel(new DiffPosition(3, 7), new DiffPosition(3, 11)),
            ],
            JsonSearch.FindText(_lines, "NAME"));
    }
}
