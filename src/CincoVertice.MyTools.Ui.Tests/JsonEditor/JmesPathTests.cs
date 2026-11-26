using CincoVertice.MyTools.Ui.JsonEditor.JmesPath;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.JsonEditor;

/// <summary>Mostly the examples of JMESPath's tutorial and specification.</summary>
public class JmesPathTests
{
    private const string People =
        """{"people": [{"name": "Ann", "age": 30}, {"name": "Bob", "age": 20}, """
        + """{"name": "Cy", "age": 40, "tags": ["a", "b"]}]}""";

    private const string Locations =
        """{"locations": [{"name": "Seattle", "state": "WA"}, {"name": "New York", "state": "NY"}, """
        + """{"name": "Bellevue", "state": "WA"}, {"name": "Olympia", "state": "WA"}]}""";

    private const string Reservations =
        """{"reservations": [{"instances": [{"state": "running"}, {"state": "stopped"}]}, """
        + """{"instances": [{"state": "terminated"}]}]}""";

    private static string Search(string json, string expression) =>
        JsonTextWriter.Write(JmesEvaluator.Search(expression, JsonDocumentReader.Read([json]).Root!), indented: false);

    [Theory]
    [InlineData("""{"a": {"b": {"c": 1}}}""", "a.b.c", "1")]
    [InlineData("""{"a": {"b": {"c": 1}}}""", "a.x.c", "null")]
    [InlineData("""{"first name": 1}""", "\"first name\"", "1")]
    [InlineData("[0, 1, 2, 3, 4, 5]", "[1]", "1")]
    [InlineData("[0, 1, 2, 3, 4, 5]", "[-1]", "5")]
    [InlineData("[0, 1, 2, 3, 4, 5]", "[0:3]", "[0,1,2]")]
    [InlineData("[0, 1, 2, 3, 4, 5]", "[::2]", "[0,2,4]")]
    [InlineData("[0, 1, 2, 3, 4, 5]", "[::-1]", "[5,4,3,2,1,0]")]
    [InlineData("[0, 1, 2, 3, 4, 5]", "[-2:]", "[4,5]")]
    [InlineData(People, "people[*].name", """["Ann","Bob","Cy"]""")]
    [InlineData(People, "people[*].tags", """[["a","b"]]""")]
    [InlineData(People, "people[?age > `25`].name", """["Ann","Cy"]""")]
    [InlineData(People, "people[?age == `20` || name == 'Cy'].name", """["Bob","Cy"]""")]
    [InlineData(People, "people[?!tags].name", """["Ann","Bob"]""")]
    [InlineData(People, "people[0].{n: name, a: age}", """{"n":"Ann","a":30}""")]
    [InlineData(People, "people[*].[name, age]", """[["Ann",30],["Bob",20],["Cy",40]]""")]
    [InlineData(People, "length(people)", "3")]
    [InlineData(People, "max_by(people, &age).name", "\"Cy\"")]
    [InlineData(People, "sort_by(people, &age)[*].name", """["Bob","Ann","Cy"]""")]
    [InlineData(People, "people[*].age | sum(@)", "90")]
    [InlineData(People, "people[*].age | avg(@)", "30")]
    [InlineData(People, "people[0].name | starts_with(@, 'A')", "true")]
    [InlineData(People, "people[*].name | join(', ', @)", "\"Ann, Bob, Cy\"")]
    [InlineData(People, "map(&name, people)", """["Ann","Bob","Cy"]""")]
    [InlineData(People, "keys(people[0])", """["name","age"]""")]
    [InlineData(People, "people[2].tags | contains(@, 'b')", "true")]
    [InlineData(People, "type(people)", "\"array\"")]
    [InlineData(People, "people[0].name || 'x'", "\"Ann\"")]
    [InlineData(People, "people[0].missing || 'x'", "\"x\"")]
    [InlineData(People, "people[0].name && people[1].name", "\"Bob\"")]
    [InlineData(People, "`[1, 2]`", "[1,2]")]
    [InlineData(People, "to_string(people[0].age)", "\"30\"")]
    [InlineData(People, "to_number('42')", "42")]
    [InlineData(Reservations, "reservations[*].instances[*].state", """[["running","stopped"],["terminated"]]""")]
    [InlineData(Reservations, "reservations[].instances[].state", """["running","stopped","terminated"]""")]
    [InlineData("""{"ops": {"a": {"n": 1}, "b": {"n": 2}}}""", "ops.*.n", "[1,2]")]
    [InlineData(
        Locations,
        "locations[?state == 'WA'].name | sort(@) | {WashingtonCities: join(', ', @)}",
        """{"WashingtonCities":"Bellevue, Olympia, Seattle"}""")]
    public void Search_GivesWhatTheSpecificationSays(string json, string expression, string expected)
    {
        // Act / Assert
        Assert.Equal(expected, Search(json, expression));
    }

    [Theory]
    [InlineData("people[", "The expression ends too soon", 7)]
    [InlineData("people.[", "The expression ends too soon", 8)]
    [InlineData("a = b", "Write == to compare", 2)]
    [InlineData("people.", "Expected a name after '.', not the end", 7)]
    [InlineData("nothing(people)", "There's no function nothing()", 0)]
    [InlineData("length(`1`)", "length(string, array or object) isn't given number there", 7)]
    [InlineData("sort_by(people, age)", "sort_by(array, &expression): write & before the expression", 16)]
    [InlineData("'not closed", "The ' isn't closed", 0)]
    [InlineData("", "Write an expression", 0)]
    public void Search_OfWhatCantRun_SaysWhyAndWhere(string expression, string message, int position)
    {
        // Act
        var error = Assert.Throws<JmesPathException>(() => Search(People, expression));

        // Assert
        Assert.StartsWith(message, error.Message);
        Assert.Equal(position, error.Position);
    }

    [Fact]
    public void Search_ValuesOfTheDocument_KeepTheirPlaces_AndLiteralsHaveNone()
    {
        // Arrange
        JsonNodeModel root = JsonDocumentReader.Read([People]).Root!;

        // Act
        JsonNodeModel names = JmesEvaluator.Search("people[*].name", root);
        JsonNodeModel literal = JmesEvaluator.Search("`{\"a\": 1}`", root);

        // Assert
        Assert.All(names.Items, name => Assert.True(name.IsInText));
        Assert.False(literal.IsInText);
        Assert.False(literal.Member("a")!.IsInText);
    }
}
