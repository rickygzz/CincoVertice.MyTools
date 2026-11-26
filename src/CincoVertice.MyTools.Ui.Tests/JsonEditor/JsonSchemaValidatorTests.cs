using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Schema;
using CincoVertice.MyTools.Ui.JsonEditor.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.JsonEditor;

public class JsonSchemaValidatorTests
{
    private const string Schema = """
        {
          "type": "object",
          "required": ["name", "age"],
          "properties": {
            "name": { "type": "string", "minLength": 2 },
            "age": { "type": "integer", "minimum": 0 },
            "role": { "enum": ["admin", "user"] },
            "tags": { "type": "array", "items": { "type": "string" }, "uniqueItems": true },
            "address": { "$ref": "#/$defs/address" }
          },
          "additionalProperties": false,
          "$defs": {
            "address": { "type": "object", "properties": { "zip": { "type": "string", "pattern": "^[0-9]{5}$" } } }
          }
        }
        """;

    private static JsonNodeModel Read(string json) => JsonDocumentReader.Read(json.Split('\n')).Root!;

    private static IReadOnlyList<(string Path, string Message)> Problems(string schema, string json) =>
    [
        .. new JsonSchemaValidator(Read(schema)).Validate(Read(json))
            .Select(problem => (problem.Path, problem.Message)),
    ];

    [Fact]
    public void Validate_SaysWhatIsWrong_WhereItIs_InTheOrderOfTheText()
    {
        // Act
        IReadOnlyList<(string, string)> problems = Problems(
            Schema,
            "{\"name\":\"A\",\"age\":1.5,\"role\":\"boss\",\"tags\":[\"x\",\"x\",3],"
            + "\"address\":{\"zip\":\"12\"},\"extra\":true}");

        // Assert
        Assert.Equal(
            [
                ("name", "At least 2 characters"),
                ("age", "Expected an integer, not a number"),
                ("role", "Should be one of: \"admin\", \"user\""),
                ("tags[1]", "The same as an item before it, where items should differ"),
                ("tags[2]", "Expected a string, not a number"),
                ("address.zip", "Should match ^[0-9]{5}$"),
                ("extra", "\"extra\" isn't one of the names allowed here"),
            ],
            problems);
    }

    [Fact]
    public void Validate_OfAValidDocument_FindsNothing()
    {
        // Act / Assert
        Assert.Empty(Problems(Schema, "{\"name\":\"Ann\",\"age\":3,\"tags\":[\"a\"],\"address\":{\"zip\":\"01234\"}}"));
    }

    [Fact]
    public void Validate_SaysAMissingMember_AtTheObjectsFirstCharacter()
    {
        // Act
        JsonSchemaProblemModel problem =
            Assert.Single(new JsonSchemaValidator(Read(Schema)).Validate(Read("{\"name\":\"Ann\"}")));

        // Assert
        Assert.Equal(("@", "Missing \"age\""), (problem.Path, problem.Message));
        Assert.Equal((new DiffPosition(0, 0), new DiffPosition(0, 1)), (problem.Start, problem.End));
    }

    [Theory]
    [InlineData(
        "{\"oneOf\":[{\"type\":\"string\"},{\"type\":\"number\"}]}",
        "true",
        "Expected a string, not a boolean")]
    [InlineData("{\"anyOf\":[{\"minimum\":10},{\"maximum\":0}]}", "5", "At least 10")]
    [InlineData("{\"not\":{\"type\":\"null\"}}", "null", "Matches what the schema says it shouldn't")]
    [InlineData("{\"exclusiveMaximum\":5}", "5", "Less than 5")]
    [InlineData("{\"maximum\":5,\"exclusiveMaximum\":true}", "5", "Less than 5")]
    [InlineData("{\"multipleOf\":0.5}", "1.25", "Should be a multiple of 0.5")]
    [InlineData("{\"const\":\"x\"}", "\"y\"", "Should be \"x\"")]
    [InlineData("{\"type\":[\"string\",\"null\"]}", "1", "Expected a string or null, not a number")]
    [InlineData("{\"minItems\":2}", "[1]", "At least 2 items")]
    [InlineData("false", "1", "Not allowed here")]
    [InlineData(
        "{\"if\":{\"properties\":{\"kind\":{\"const\":\"a\"}}},\"then\":{\"required\":[\"x\"]}}",
        "{\"kind\":\"a\"}",
        "Missing \"x\"")]
    public void Validate_KnowsTheKeywords(string schema, string json, string message)
    {
        // Act / Assert
        Assert.Equal(message, Assert.Single(Problems(schema, json)).Message);
    }

    [Fact]
    public void Validate_WithAPatternThatIsntOne_DoesntBlameTheDocument()
    {
        // Act / Assert
        Assert.Empty(Problems("{\"pattern\":\"([\"}", "\"x\""));
    }

    [Fact]
    public void NamesFor_GivesTheSchemasNames_ThroughRefs()
    {
        // Arrange
        JsonNodeModel document = Read("{\"address\":{}}");
        var validator = new JsonSchemaValidator(Read(Schema));

        // Act / Assert
        Assert.Equal(["name", "age", "role", "tags", "address"], validator.NamesFor(document));
        Assert.Equal(["zip"], validator.NamesFor(document.Member("address")!));
    }

    [Fact]
    public void KeySuggester_OffersTheSchemasNamesFirst()
    {
        // Arrange
        JsonNodeModel document = Read("{\"name\":\"Ann\"}");

        // Act
        IReadOnlyList<string> names =
            JsonKeySuggester.NamesFor(document, document, new JsonSchemaValidator(Read(Schema)));

        // Assert
        Assert.Equal(["age", "role", "tags", "address"], names);
    }
}
