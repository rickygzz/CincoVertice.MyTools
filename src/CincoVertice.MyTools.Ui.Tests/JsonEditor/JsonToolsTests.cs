using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Models;
using CincoVertice.MyTools.Ui.JsonEditor.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.JsonEditor;

public class JsonToolsTests
{
    private static JsonNodeModel Read(string json) => JsonDocumentReader.Read(json.Split('\n')).Root!;

    [Fact]
    public void Tree_LabelsEachValue_ByItsNameOrIndex_AndWhatItHolds()
    {
        // Arrange
        JsonNodeModel root = Read("{\"name\":\"Ann\",\"people\":[{\"a\":1}],\"age\":31,\"ok\":null}");

        // Act
        IReadOnlyList<string> labels = [.. JsonTree.ChildrenOf(root).Select(JsonTree.LabelOf)];

        // Assert
        Assert.Equal(["name: \"Ann\"", "people [1]", "age: 31", "ok: null"], labels);
        Assert.Equal("{4}", JsonTree.LabelOf(root));
        Assert.Equal("[0] {1}", JsonTree.LabelOf(root.Member("people")!.Items[0]));
    }

    [Fact]
    public void Crumbs_AreTheStepsToTheValue_EachWithItsValue()
    {
        // Arrange
        JsonNodeModel root = Read("{\"people\":[{\"first name\":\"Ann\"}]}");
        JsonNodeModel ann = root.Member("people")!.Items[0].Member("first name")!;

        // Act
        IReadOnlyList<JsonCrumbModel> crumbs = JsonPathWriter.CrumbsOf(ann);

        // Assert
        Assert.Equal(["people", "[0]", "\"first name\""], crumbs.Select(crumb => crumb.Text));
        Assert.Same(ann, crumbs[^1].Node);
        Assert.Empty(JsonPathWriter.CrumbsOf(root));
    }

    [Theory]
    [InlineData("{\"a\":1,\"b\":2}", "object, 2 keys")]
    [InlineData("[1]", "array, 1 item")]
    [InlineData("\"Ann\"", "string, 3 characters")]
    [InlineData("4.5", "number")]
    [InlineData("false", "boolean")]
    [InlineData("null", "null")]
    public void KindOf_SaysWhatAValueIs(string json, string expected)
    {
        // Act / Assert
        Assert.Equal(expected, JsonPathWriter.KindOf(Read(json)));
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 3, 0)]
    [InlineData(3, 0, 0, 0, 3, 0)]
    [InlineData(1, 7, 1, 7, 1, 12)]
    [InlineData(1, 13, 1, 7, 1, 12)]
    [InlineData(1, 12, 1, 7, 1, 12)]
    public void Brackets_PairTheOneAtTheCaret(int line, int column, int openLine, int open, int closeLine, int close)
    {
        // Arrange
        string[] lines = ["{", "  \"a\": [1, 2],", "  \"s\": \"[\"", "}"];
        JsonDocumentModel document = JsonDocumentReader.Read(lines);

        // Act
        (DiffPosition Open, DiffPosition Close)? pair =
            JsonBrackets.PairAt(document.Tokens, lines, new DiffPosition(line, column));

        // Assert
        Assert.Equal((new DiffPosition(openLine, open), new DiffPosition(closeLine, close)), pair);
    }

    [Theory]
    [InlineData(2, 9)]
    [InlineData(1, 4)]
    public void Brackets_InAStringOrAwayFromOne_AreNone(int line, int column)
    {
        // Arrange: a [ in a string isn't a bracket
        string[] lines = ["{", "  \"a\": [1, 2],", "  \"s\": \"[\"", "}"];

        // Act / Assert
        Assert.Null(JsonBrackets.PairAt(JsonDocumentReader.Read(lines).Tokens, lines, new DiffPosition(line, column)));
    }

    [Fact]
    public void Brackets_WithNoPair_AreNone()
    {
        // Arrange
        string[] lines = ["{", "  \"a\": [1, 2"];

        // Act / Assert
        Assert.Null(JsonBrackets.PairAt(JsonDocumentReader.Read(lines).Tokens, lines, new DiffPosition(1, 7)));
    }

    [Fact]
    public void SortedKeys_SortsEveryObject_IgnoringCaseFirst()
    {
        // Arrange
        JsonNodeModel root = Read("{\"b\":1,\"a\":[{\"z\":1,\"y\":2}],\"A\":{\"d\":1,\"c\":2}}");

        // Act
        string sorted = JsonTextWriter.Write(JsonTransforms.SortedKeys(root), indented: false);

        // Assert
        Assert.Equal("{\"A\":{\"c\":2,\"d\":1},\"a\":[{\"y\":2,\"z\":1}],\"b\":1}", sorted);
    }

    [Fact]
    public void AsLines_GivesAnArraysItems_StringsWithoutQuotes()
    {
        // Act
        string lines = JsonTransforms.AsLines(Read("[\"Ann\",\"Bob\",3,{\"x\":1}]"));

        // Assert
        Assert.Equal(string.Join(Environment.NewLine, "Ann", "Bob", "3", "{\"x\":1}"), lines);
        Assert.Equal("Ann", JsonTransforms.AsLines(Read("\"Ann\"")));
    }

    [Theory]
    [InlineData("{name: 'Ann', \"age\": 31,}", "{\"name\": \"Ann\", \"age\": 31}")]
    [InlineData("{\"a\": True, \"b\": None, \"c\": undefined}", "{\"a\": true, \"b\": null, \"c\": null}")]
    [InlineData("[1 2 3]", "[1, 2, 3]")]
    [InlineData("{\"s\": 'say \"hi\"', \"t\": 'it\\'s'}", "{\"s\": \"say \\\"hi\\\"\", \"t\": \"it's\"}")]
    [InlineData("{a: -Infinity, b: .5, c: +1}", "{\"a\": null, \"b\": 0.5, \"c\": 1}")]
    [InlineData("[1,, 2]", "[1, 2]")]
    [InlineData("{\"a\": 1 /* one */}", "{\"a\": 1 }")]
    public void Repair_MakesItJson(string text, string expected)
    {
        // Act
        (string repaired, IReadOnlyList<string> fixes) = JsonRepair.Repair(text);

        // Assert
        Assert.Equal(expected, repaired);
        Assert.NotEmpty(fixes);
        Assert.Null(JsonDocumentReader.Read([repaired]).Error);
    }

    [Fact]
    public void Repair_OfLines_KeepsThem_AndAddsTheCommaAfterTheValue()
    {
        // Act
        (string repaired, IReadOnlyList<string> fixes) =
            JsonRepair.Repair("{\n  \"a\": 1 // one\n  \"b\": 2,\n}");

        // Assert
        Assert.Equal("{\n  \"a\": 1, \n  \"b\": 2\n}", repaired);
        Assert.Equal(["1 comment removed", "1 missing comma added", "1 comma before a closing bracket removed"], fixes);
    }

    [Fact]
    public void Repair_OfJson_ChangesNothing()
    {
        // Act
        (string repaired, IReadOnlyList<string> fixes) = JsonRepair.Repair("{\"a\": [1, \"x\", null]}");

        // Assert
        Assert.Equal("{\"a\": [1, \"x\", null]}", repaired);
        Assert.Empty(fixes);
    }

    [Fact]
    public void Yaml_WritesBlocks_QuotingWhatWouldReadBackAsSomethingElse()
    {
        // Arrange
        JsonNodeModel root = Read(
            "{\"name\":\"Ann\",\"age\":31,\"tags\":[\"a\",\"b\"],\"address\":{\"zip\":\"01234\",\"city\":\"Paris\"},"
            + "\"people\":[{\"id\":\"A\",\"x\":1},{\"id\":\"B\"}],\"empty\":[],\"yes\":\"yes\",\"note\":\"a: b\"}");

        // Act
        string yaml = JsonYamlWriter.Write(root);

        // Assert
        Assert.Equal(
            "name: Ann\nage: 31\ntags:\n  - a\n  - b\naddress:\n  zip: \"01234\"\n  city: Paris\n"
            + "people:\n  - id: A\n    x: 1\n  - id: B\nempty: []\n\"yes\": \"yes\"\nnote: \"a: b\"\n",
            yaml);
    }

    [Fact]
    public void Csv_WritesATable_AColumnPerName_QuotingWhatNeedsIt()
    {
        // Arrange
        JsonNodeModel root = Read(
            "[{\"name\":\"Ann\",\"age\":31},{\"name\":\"Bob, Jr.\",\"tags\":[\"a\"]},"
            + "{\"name\":\"C\\\"q\",\"age\":null}]");

        // Act
        string csv = JsonCsv.Write(root);

        // Assert
        Assert.Equal(
            "name,age,tags\r\nAnn,31,\r\n\"Bob, Jr.\",,\"[\"\"a\"\"]\"\r\n\"C\"\"q\",,\r\n",
            csv);
    }

    [Fact]
    public void Csv_OfValues_IsOneColumn_AndOfAnObject_IsRefused()
    {
        // Act / Assert
        Assert.Equal("value\r\nAnn\r\n3\r\n", JsonCsv.Write(Read("[\"Ann\",3]")));
        Assert.Throws<FormatException>(() => JsonCsv.Write(Read("{\"a\":1}")));
    }

    [Fact]
    public void Csv_ReadsATable_BySemicolons_ValuesAsTheyRead()
    {
        // Act
        JsonNodeModel table = JsonCsv.Read("name;age;active\r\nAnn;31;true\r\n\"Bob; \"\"Jr\"\"\";;x\r\n\r\n");

        // Assert
        Assert.Equal(
            "[{\"name\":\"Ann\",\"age\":31,\"active\":true},"
            + "{\"name\":\"Bob; \\\"Jr\\\"\",\"age\":\"\",\"active\":\"x\"}]",
            JsonTextWriter.Write(table, indented: false));
    }

    [Fact]
    public void Csv_WithAQuoteThatNeverCloses_IsRefused()
    {
        // Act / Assert
        Assert.Throws<FormatException>(() => JsonCsv.Read("a,b\n\"x,1"));
    }

    [Fact]
    public void KeySuggester_OffersTheNamesTheOtherItemsHave_ButNotThoseItHas()
    {
        // Arrange
        string[] lines = ["{\"people\": [", "  {\"name\": \"A\", \"age\": 1},", "  {\"name\": \"B\" }", "]}"];
        JsonNodeModel root = JsonDocumentReader.Read(lines).Root!;

        // Act: the caret before Bob's }
        JsonNodeModel? target = JsonKeySuggester.ObjectAt(root, new DiffPosition(2, 15));

        // Assert
        Assert.Same(root.Member("people")!.Items[1], target);
        Assert.Equal(["age"], JsonKeySuggester.NamesFor(target!, root, schema: null));
    }
}
