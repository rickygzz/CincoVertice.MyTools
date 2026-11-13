using System.Drawing;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Diagramming;

public sealed class DiagramPdfTests : IDisposable
{
    private static readonly DiagramStyleModel _style =
        new(
            Color.FromArgb(255, 255, 255, 0),
            Color.FromArgb(255, 51, 102, 153),
            3,
            false,
            Color.FromArgb(255, 0, 0, 255),
            18);

    private readonly FakeOutliner _outliner = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "DiagramPdfTests_" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private static DiagramModel OneShape(DiagramStyleModel style, string text = "") => new(
        [new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(10, 20, 100, 50), text, style)],
        []);

    private static string Text(byte[] pdf) => Encoding.Latin1.GetString(pdf);

    /// <summary>The page's drawing, unpacked.</summary>
    private static string ContentOf(byte[] pdf)
    {
        string text = Text(pdf);
        int start = text.IndexOf("stream\n", StringComparison.Ordinal) + "stream\n".Length;
        int end = text.IndexOf("\nendstream", start, StringComparison.Ordinal);

        using var packed = new MemoryStream(pdf, start, end - start);
        using var zlib = new ZLibStream(packed, CompressionMode.Decompress);
        using var reader = new StreamReader(zlib, Encoding.ASCII);

        return reader.ReadToEnd();
    }

    [Fact]
    public void Write_IsAPdf_WhoseTableSaysWhereEachObjectStarts()
    {
        // Act
        byte[] pdf = DiagramPdf.Write(OneShape(_style, "Hi"), _outliner);

        // Assert
        string text = Text(pdf);
        Assert.StartsWith("%PDF-1.4\n", text);
        Assert.EndsWith("%%EOF\n", text);

        int table = int.Parse(Regex.Match(text, @"startxref\n(\d+)\n").Groups[1].Value, CultureInfo.InvariantCulture);
        Assert.StartsWith("xref\n0 6\n0000000000 65535 f\r\n", text[table..]);

        MatchCollection entries = Regex.Matches(text[table..], @"(\d{10}) 00000 n\r\n");
        Assert.Equal(5, entries.Count);

        for (int i = 0; i < entries.Count; i++)
        {
            int offset = int.Parse(entries[i].Groups[1].Value, CultureInfo.InvariantCulture);
            Assert.StartsWith($"{i + 1} 0 obj\n", text[offset..]);
        }

        Assert.Contains("/Root 1 0 R", text);
    }

    [Fact]
    public void Write_ThePage_IsAsLargeAsTheSvgImage_InPoints()
    {
        // Arrange
        DiagramModel diagram = OneShape(_style);
        XElement svg = XDocument.Parse(DiagramSvg.Write(diagram)).Root!;
        float width = float.Parse((string)svg.Attribute("width")!, CultureInfo.InvariantCulture) * 0.75f;
        float height = float.Parse((string)svg.Attribute("height")!, CultureInfo.InvariantCulture) * 0.75f;

        // Act
        string text = Text(DiagramPdf.Write(diagram, _outliner));

        // Assert
        Assert.Contains(
            string.Create(CultureInfo.InvariantCulture, $"/MediaBox [0 0 {width} {height}]"),
            text);
    }

    [Fact]
    public void Write_DrawsInDiagramPixels_FromTheTopLeftOfTheArea()
    {
        // Arrange
        DiagramModel diagram = OneShape(_style);
        Rectangle area = DiagramArea.Of(diagram);

        // Act
        string content = ContentOf(DiagramPdf.Write(diagram, _outliner));

        // Assert: y goes down, as in the diagram
        Assert.StartsWith(
            string.Create(
                CultureInfo.InvariantCulture,
                $"0.75 0 0 -0.75 {-area.X * 0.75f} {area.Bottom * 0.75f} cm\n"),
            content);
    }

    [Fact]
    public void Write_AShapeWithFillAndLine_FillsThenStrokesIt()
    {
        // Act
        string content = ContentOf(DiagramPdf.Write(OneShape(_style), _outliner));

        // Assert
        Assert.Contains("0.2 0.4 0.6 RG 3 w [] 0 d\n", content);
        Assert.Contains("1 1 0 rg\n", content);
        Assert.Contains("10 20 m\n", content);
        Assert.Contains("\nB\n", content);
    }

    [Fact]
    public void Write_ADashedShapeWithoutFill_OnlyStrokes_WithDashesInLineWidths()
    {
        // Arrange
        DiagramStyleModel style = _style with { Fill = Color.Empty, Dashed = true };

        // Act
        string content = ContentOf(DiagramPdf.Write(OneShape(style), _outliner));

        // Assert
        Assert.Contains("3 w [12 9] 0 d\n", content);
        Assert.Contains("\nS\n", content);
        Assert.DoesNotContain(" rg\n", content);
        Assert.DoesNotContain("\nB\n", content);
    }

    [Fact]
    public void Write_AShapeWithoutLine_OnlyFills()
    {
        // Arrange
        DiagramStyleModel style = _style with { LineWidth = 0 };

        // Act
        string content = ContentOf(DiagramPdf.Write(OneShape(style), _outliner));

        // Assert
        Assert.Contains("\nf\n", content);
        Assert.DoesNotContain("\nS\n", content);
        Assert.DoesNotContain("\nB\n", content);
    }

    [Fact]
    public void Write_Text_FillsTheOutlinesOfItsLetters_InItsColor()
    {
        // Act
        string content = ContentOf(DiagramPdf.Write(OneShape(_style, "Hello"), _outliner));

        // Assert
        (DiagramTextBlockModel block, DiagramStyleModel style) = Assert.Single(_outliner.Calls);
        Assert.Equal(["Hello"], block.Lines);
        Assert.Same(_style, style);
        Assert.Contains("q\n0 0 1 rg\n1 2 m\n3 4 l\n5 6 7 8 9 10 c\nh\nf\nQ\n", content);
    }

    [Fact]
    public void Write_TextWithAnOutline_StrokesItsEdgeUnderTheLetters()
    {
        // Arrange
        DiagramStyleModel style =
            _style with { TextOutlineWidth = 2, TextOutlineColor = Color.FromArgb(255, 255, 0, 0) };

        // Act
        string content = ContentOf(DiagramPdf.Write(OneShape(style, "Hello"), _outliner));

        // Assert: twice as wide, since the letters on top cover the inner half
        int edge = content.IndexOf("1 0 0 RG 4 w [] 0 d\n", StringComparison.Ordinal);
        int letters = content.IndexOf("0 0 1 rg\n1 2 m", StringComparison.Ordinal);
        Assert.True(edge >= 0 && letters > edge);
        Assert.Contains("h\nS\n", content);
    }

    [Fact]
    public void Write_TextWithNothingToDraw_LeavesItOut()
    {
        // Arrange
        _outliner.Letters = [];

        // Act
        string content = ContentOf(DiagramPdf.Write(OneShape(_style, "   "), _outliner));

        // Assert
        Assert.Single(_outliner.Calls);
        Assert.DoesNotContain("0 0 1 rg", content);
    }

    [Fact]
    public void Write_AConnector_StrokesItsLine_FillsItsHeads_AndClearsBehindItsLabel()
    {
        // Arrange
        DiagramModel diagram = new(
            [
                new DiagramShapeModel(1, DiagramShapeKind.Rectangle, new Rectangle(0, 0, 100, 50), "", _style),
                new DiagramShapeModel(2, DiagramShapeKind.Rectangle, new Rectangle(300, 0, 100, 50), "", _style),
            ],
            [new DiagramConnectorModel(3, 1, 2, DiagramRouting.Straight, DiagramArrowHeads.Both, "yes", _style)]);

        // Act
        string content = ContentOf(DiagramPdf.Write(diagram, _outliner));

        // Assert
        Assert.Equal(2, Regex.Matches(content, @"\nh f\n").Count);
        Assert.Matches(@"1 1 1 rg [\d.-]+ [\d.-]+ [\d.-]+ [\d.-]+ re f\n", content);
        Assert.Equal(["yes"], Assert.Single(_outliner.Calls).Block.Lines);
    }

    [Fact]
    public void Write_InAnyCulture_WritesNumbersWithAPoint()
    {
        // Arrange
        CultureInfo culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        DiagramModel diagram = new(
            [new DiagramShapeModel(1, DiagramShapeKind.Ellipse, new Rectangle(10, 20, 101, 51), "", _style)],
            []);

        try
        {
            // Act
            byte[] pdf = DiagramPdf.Write(diagram, _outliner);

            // Assert
            Assert.DoesNotContain(",", ContentOf(pdf));
            Assert.Contains(".", ContentOf(pdf));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void Write_AnEmptyDiagram_IsAnEmptyPage()
    {
        // Act
        byte[] pdf = DiagramPdf.Write(DiagramModel.Empty, _outliner);

        // Assert
        Assert.Contains("/MediaBox [0 0 75 75]", Text(pdf));
        Assert.Equal("0.75 0 0 -0.75 0 75 cm\n1 j\n", ContentOf(pdf));
    }

    [Fact]
    public void Export_WritesTheFile_WithoutLeavingATemporaryOne()
    {
        // Arrange
        Directory.CreateDirectory(_folder);
        string path = Path.Combine(_folder, "flow.pdf");
        var exporter = new PdfDiagramExporter(_outliner);

        // Act
        exporter.Export(path, OneShape(_style, "Hi"));

        // Assert
        Assert.StartsWith("%PDF-", Text(File.ReadAllBytes(path)));
        Assert.Equal([path], Directory.GetFiles(_folder));
    }

    private sealed class FakeOutliner : IDiagramTextOutliner
    {
        public List<(DiagramTextBlockModel Block, DiagramStyleModel Style)> Calls { get; } = [];

        public IReadOnlyList<DiagramFigureModel> Letters { get; set; } =
        [
            new DiagramFigureModel(
            [
                new DiagramPathSegmentModel(DiagramPathOperation.Move, [new PointF(1, 2)]),
                new DiagramPathSegmentModel(DiagramPathOperation.Line, [new PointF(3, 4)]),
                new DiagramPathSegmentModel(
                    DiagramPathOperation.Curve,
                    [new PointF(5, 6), new PointF(7, 8), new PointF(9, 10)]),
                new DiagramPathSegmentModel(DiagramPathOperation.Close, []),
            ]),
        ];

        public IReadOnlyList<DiagramFigureModel> Outline(DiagramTextBlockModel block, DiagramStyleModel style)
        {
            Calls.Add((block, style));

            return Letters;
        }
    }
}
