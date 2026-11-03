using System.Drawing;
using System.Xml.Linq;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diagramming;

public sealed class DiagramSvgTests : IDisposable
{
    private static readonly XNamespace _svg = "http://www.w3.org/2000/svg";
    private static readonly DiagramStyleModel _style =
        new(Color.FromArgb(255, 255, 255, 0), Color.FromArgb(255, 1, 2, 3), 3, true, Color.FromArgb(255, 4, 5, 6), 18);

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "DiagramSvgTests_" + Guid.NewGuid());

    private static DiagramModel Sample() => new(
        [
            new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(10, 20, 100, 50), "Start <here> & go", _style, GroupId: 7),
            new DiagramShapeModel(2, DiagramShapeKind.Cylinder, new Rectangle(300, 200, 80, 60), "line one\nline two", _style with { Fill = Color.Empty }),
            new DiagramShapeModel(3, DiagramShapeKind.Text, new Rectangle(0, 300, 80, 30), "]]> \"quoted\"", _style),
        ],
        [
            new DiagramConnectorModel(4, 1, 2, DiagramRouting.Elbow, DiagramArrowHeads.Both, "yes", _style, ToSide: DiagramSide.Left),
        ]);

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void WriteThenRead_GivesTheSameDiagram()
    {
        // Act
        DiagramModel read = DiagramSvg.Read(DiagramSvg.Write(Sample()));

        // Assert
        Assert.Equal(Sample().Shapes, read.Shapes);
        Assert.Equal(Sample().Connectors, read.Connectors);
    }

    [Fact]
    public void WriteThenRead_KeepsTheBends()
    {
        // Arrange
        DiagramModel diagram = Sample() with
        {
            Connectors = [Sample().Connectors[0] with { Bends = [new Point(1, 2), new Point(3, 4)] }],
        };

        // Act
        DiagramModel read = DiagramSvg.Read(DiagramSvg.Write(diagram));

        // Assert
        Assert.Equal([new Point(1, 2), new Point(3, 4)], read.Connectors[0].Bends!);
    }

    [Fact]
    public void Write_TheAreaReachesABendOutsideTheShapes()
    {
        // Arrange
        DiagramModel diagram = Sample() with
        {
            Connectors = [Sample().Connectors[0] with { Label = string.Empty, Bends = [new Point(900, 900)] }],
        };

        // Act
        XDocument document = XDocument.Parse(DiagramSvg.Write(diagram));
        int[] viewBox = [.. ((string)document.Root!.Attribute("viewBox")!).Split(' ').Select(int.Parse)];

        // Assert
        Assert.Equal(920, viewBox[0] + viewBox[2]);
        Assert.Equal(920, viewBox[1] + viewBox[3]);
    }

    [Fact]
    public void Write_IsValidXml_WithSomethingToSeeForEveryPart()
    {
        // Act
        XDocument document = XDocument.Parse(DiagramSvg.Write(Sample()));

        // Assert
        Assert.Equal(_svg + "svg", document.Root!.Name);

        // The rectangle, the cylinder's two, and the connector's line, with a curve at its corners
        Assert.Equal(4, document.Descendants(_svg + "path").Count());
        Assert.Contains("C", (string?)document.Descendants(_svg + "path").Last().Attribute("d"));
        Assert.Equal(2, document.Descendants(_svg + "polygon").Count());

        // Wrapped to fit in its shape, with the characters XML treats specially kept
        Assert.Contains(document.Descendants(_svg + "tspan"), span => span.Value == "<here> &");
        Assert.Contains(document.Descendants(_svg + "tspan"), span => span.Value == "two");
        Assert.Contains(document.Descendants(_svg + "tspan"), span => span.Value == "yes");
    }

    [Fact]
    public void Write_UsesTheStyle()
    {
        // Act
        XDocument document = XDocument.Parse(DiagramSvg.Write(Sample()));
        XElement first = document.Descendants(_svg + "path").First();

        // Assert
        Assert.Equal("#FFFF00", (string?)first.Attribute("fill"));
        Assert.Equal("#010203", (string?)first.Attribute("stroke"));
        Assert.Equal("3", (string?)first.Attribute("stroke-width"));
        Assert.Equal("12 9", (string?)first.Attribute("stroke-dasharray"));
    }

    [Fact]
    public void Write_AShapeWithNoFill_HasNone()
    {
        // Act
        XDocument document = XDocument.Parse(DiagramSvg.Write(Sample()));

        // Assert: the cylinder's two paths
        Assert.All(
            document.Descendants(_svg + "path").Skip(1),
            path => Assert.Equal("none", (string?)path.Attribute("fill")));
    }

    [Fact]
    public void Write_TheAreaIsAroundEverything()
    {
        // Act
        XDocument document = XDocument.Parse(DiagramSvg.Write(Sample()));
        int[] viewBox = [.. ((string)document.Root!.Attribute("viewBox")!).Split(' ').Select(int.Parse)];

        // Assert: from 0, 20 to 380, 330, with a margin of 20
        Assert.Equal([-20, 0, 420, 350], viewBox);
    }

    [Fact]
    public void Write_AnEmptyDiagram_CanBeReadAgain()
    {
        // Act
        DiagramModel read = DiagramSvg.Read(DiagramSvg.Write(DiagramModel.Empty));

        // Assert
        Assert.Empty(read.Shapes);
        Assert.Empty(read.Connectors);
    }

    [Fact]
    public void Write_ACharacterXmlCantHold_IsLeftOutOfTheImage_ButKeptInTheDiagram()
    {
        // Arrange
        DiagramModel diagram = new(
            [new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(0, 0, 200, 50), "a\u0001b", _style)],
            []);

        // Act
        string svg = DiagramSvg.Write(diagram);

        // Assert
        Assert.Contains(XDocument.Parse(svg).Descendants(_svg + "tspan"), span => span.Value == "ab");
        Assert.Equal("a\u0001b", DiagramSvg.Read(svg).Shapes[0].Text);
    }

    [Theory]
    [InlineData("not xml at all")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect width=\"1\" height=\"1\" /></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><metadata id=\"mytools-diagram\">{ broken</metadata></svg>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"><metadata id=\"mytools-diagram\">{}</metadata></svg>")]
    public void Read_WhatThisEditorDidntWrite_IsInvalid(string text)
    {
        Assert.Throws<InvalidDataException>(() => DiagramSvg.Read(text));
    }

    [Fact]
    public void Json_AConnectorWithoutItsShapes_IsLeftOut()
    {
        // Arrange
        string json = DiagramJson.Write(Sample() with { Shapes = [Sample().Shapes[0]] });

        // Act
        DiagramModel read = DiagramJson.Read(json);

        // Assert
        Assert.Empty(read.Connectors);
    }

    [Fact]
    public void Json_TwoPartsWithTheSameId_AreInvalid()
    {
        // Arrange
        string json = DiagramJson.Write(Sample() with { Shapes = [Sample().Shapes[0], Sample().Shapes[0]] });

        // Assert
        Assert.Throws<InvalidDataException>(() => DiagramJson.Read(json));
    }

    [Fact]
    public void Json_ValuesOutOfRange_AreBroughtIntoRange()
    {
        // Arrange
        const string json = """
            { "format": "mytools-diagram", "shapes": [
                { "id": 1, "kind": "Ellipse", "x": 0, "y": 0, "width": -5, "height": 0,
                  "style": { "fill": "teal", "lineWidth": 9999, "fontSize": 1 } } ] }
            """;

        // Act
        DiagramShapeModel shape = DiagramJson.Read(json).Shapes[0];

        // Assert
        Assert.Equal(new Size(1, 1), shape.Bounds.Size);
        Assert.True(shape.Style.Fill.IsEmpty);
        Assert.Equal(100, shape.Style.LineWidth);
        Assert.Equal(6, shape.Style.FontSize);
        Assert.Equal(string.Empty, shape.Text);
    }

    [Fact]
    public void Store_SaveThenLoad_GivesTheSameDiagram_AndLeavesNoTemporaryFile()
    {
        // Arrange
        Directory.CreateDirectory(_folder);
        string path = Path.Combine(_folder, "diagram.svg");
        var store = new SvgDiagramFileStore();

        // Act
        store.Save(path, Sample());
        DiagramModel loaded = store.Load(path);

        // Assert
        Assert.Equal(Sample().Shapes, loaded.Shapes);
        Assert.False(File.Exists(path + ".tmp"));
    }
}
