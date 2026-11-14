using System.Drawing;
using System.Xml.Linq;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diagramming;

public sealed class DiagramSvgImageTests
{
    private static readonly XNamespace _svg = "http://www.w3.org/2000/svg";

    private static XElement Prepared(string markup) => XElement.Parse(DiagramSvgImage.Prepare(markup));

    [Fact]
    public void Prepare_PutsTheStyleSheetsRulesInTheElements_BeforeTheirOwnStyle()
    {
        // Arrange
        const string markup = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 18 18">
              <defs><style>/* Azure */ .cls-1{fill:#e8121d;} .cls-2, #b { opacity: 0.5 } path.cls-1{stroke:#000}
                g path { fill: blue } @media print { .cls-1 { fill: gray } }</style></defs>
              <rect class="cls-1" width="9" height="9"/>
              <path id="b" class="cls-1 cls-2" style="fill:#fff" d="M0 0h9"/>
              <circle r="1"/>
            </svg>
            """;

        // Act
        XElement root = Prepared(markup);

        // Assert: a rule whose selector isn't simple, or inside an at-rule, is left out
        Assert.Empty(root.Descendants(_svg + "style"));
        Assert.Equal("fill:#e8121d", (string?)root.Descendants(_svg + "rect").Single().Attribute("style"));
        Assert.Equal(
            "fill:#e8121d;opacity: 0.5;opacity: 0.5;stroke:#000;fill:#fff",
            (string?)root.Descendants(_svg + "path").Single().Attribute("style"));
        Assert.Null(root.Descendants(_svg + "circle").Single().Attribute("style"));
    }

    [Fact]
    public void Prepare_LeavesOutScriptsAndEventHandlers()
    {
        // Arrange
        const string markup = """
            <svg xmlns="http://www.w3.org/2000/svg" onload="alert(1)"><script>alert(2)</script>
              <foreignObject><p>html</p></foreignObject><rect onclick="alert(3)" width="1" height="1"/></svg>
            """;

        // Act
        string prepared = DiagramSvgImage.Prepare(markup);

        // Assert
        Assert.DoesNotContain("alert", prepared);
        Assert.Contains("<rect", prepared);
    }

    [Fact]
    public void Prepare_ItsOwnSize_BecomesItsViewBox_SoItFillsAnyBox()
    {
        // Act
        XElement root = Prepared("""<svg xmlns="http://www.w3.org/2000/svg" width="48px" height="24"/>""");

        // Assert
        Assert.Equal("0 0 48 24", (string?)root.Attribute("viewBox"));
        Assert.Null(root.Attribute("width"));
        Assert.Null(root.Attribute("height"));
    }

    [Fact]
    public void Prepare_ADoctype_IsSkipped()
    {
        // Arrange
        const string markup = """
            <?xml version="1.0"?>
            <!DOCTYPE svg PUBLIC "-//W3C//DTD SVG 1.1//EN" "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd">
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10"/>
            """;

        // Act / Assert
        Assert.Equal(new SizeF(10, 10), DiagramSvgImage.SizeOf(DiagramSvgImage.Prepare(markup)));
    }

    [Theory]
    [InlineData("not markup")]
    [InlineData("<html><body/></html>")]
    [InlineData("<svg><rect></svg>")]
    [InlineData("<svg/>")]
    public void Prepare_SomethingElse_IsntAnImage(string text)
    {
        // Act / Assert: an svg element outside the SVG namespace isn't one either
        Assert.Throws<InvalidDataException>(() => DiagramSvgImage.Prepare(text));
    }

    [Theory]
    [InlineData("""<svg xmlns="http://www.w3.org/2000/svg"/>""",
        """<svg preserveAspectRatio="none" xmlns="http://www.w3.org/2000/svg"/>""")]
    [InlineData("""<svg preserveAspectRatio='xMinYMin slice' xmlns="http://www.w3.org/2000/svg"><g/></svg>""",
        """<svg preserveAspectRatio="none" xmlns="http://www.w3.org/2000/svg"><g/></svg>""")]
    [InlineData(
        """<svg xmlns="s"><svg preserveAspectRatio="xMidYMid"/></svg>""",
        """<svg preserveAspectRatio="none" xmlns="s"><svg preserveAspectRatio="xMidYMid"/></svg>""")]
    [InlineData("not markup", "not markup")]
    public void Filling_StretchesOnlyTheRootToItsBox(string markup, string expected)
    {
        // Act / Assert: an svg inside it keeps its own fit
        Assert.Equal(expected, DiagramSvgImage.Filling(markup));
    }

    [Theory]
    [InlineData("0 0 18 18", 64, 64)]
    [InlineData("0 0 200 100", 200, 100)]
    [InlineData("0,0,1000,500", 320, 160)]
    [InlineData("0 0 10 40", 16, 64)]
    [InlineData("none", 100, 100)]
    public void InsertSize_IsItsOwn_WithTheLongerSideBetween64And320(string viewBox, int width, int height)
    {
        // Arrange
        string markup = $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="{viewBox}"/>""";

        // Act / Assert
        Assert.Equal(new Size(width, height), DiagramSvgImage.InsertSize(markup));
    }

    [Fact]
    public void TextOfAnImage_IsUnderIt_AndCanBeWiderThanIt()
    {
        // Arrange
        DiagramShapeModel image = new(
            1,
            DiagramShapeKind.Image,
            new Rectangle(100, 100, 64, 64),
            "Virtual machines scale sets",
            new DiagramStyleModel(Color.Empty, Color.Black, 2, false, Color.Black, 14),
            Image: """<svg xmlns="http://www.w3.org/2000/svg"/>""");

        // Act
        DiagramTextBlockModel block = DiagramTextLayout.OfShape(image);

        // Assert
        Assert.Equal(132, block.CenterX);
        Assert.Equal(168, block.Top);
        Assert.True(block.Bounds.Width > 64);
        Assert.True(block.Bounds.Width <= 160);
    }
}
