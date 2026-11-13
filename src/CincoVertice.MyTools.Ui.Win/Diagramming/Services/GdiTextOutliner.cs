using System.Drawing.Drawing2D;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Models;

namespace CincoVertice.MyTools.Ui.Win.Diagramming.Services;

/// <summary>
///     The letters' outlines from GDI+, with the fonts and the placing the editor draws text with
///     (<see cref="DiagramRenderer.TextPath"/>), so a PDF shows the text as the canvas does.
/// </summary>
public sealed class GdiTextOutliner : IDiagramTextOutliner
{
    // GraphicsPath's point types: the kind in the low bits, and a flag on a figure's last point
    private const byte PointKindMask = 0x07;
    private const byte StartPoint = 0;
    private const byte BezierPoint = 3;
    private const byte CloseFlag = 0x80;

    public IReadOnlyList<DiagramFigureModel> Outline(DiagramTextBlockModel block, DiagramStyleModel style)
    {
        using Font font = DiagramFonts.Create(style.FontFamily, style.FontSize);
        using GraphicsPath path = DiagramRenderer.TextPath(block, font);

        return path.PointCount == 0 ? [] : [new DiagramFigureModel(SegmentsOf(path.PathPoints, path.PathTypes))];
    }

    /// <summary>
    ///     All the letters in one figure: a letter with a hole, like "o", needs its inner outline in the same fill.
    /// </summary>
    internal static IReadOnlyList<DiagramPathSegmentModel> SegmentsOf(PointF[] points, byte[] types)
    {
        var segments = new List<DiagramPathSegmentModel>();

        for (int i = 0; i < points.Length; i++)
        {
            int last = i;

            switch (types[i] & PointKindMask)
            {
                case StartPoint:
                    segments.Add(new DiagramPathSegmentModel(DiagramPathOperation.Move, [points[i]]));
                    break;
                case BezierPoint when i + 2 < points.Length:
                    last = i + 2;
                    segments.Add(new DiagramPathSegmentModel(
                        DiagramPathOperation.Curve,
                        [points[i], points[i + 1], points[i + 2]]));
                    break;
                default:
                    segments.Add(new DiagramPathSegmentModel(DiagramPathOperation.Line, [points[i]]));
                    break;
            }

            if ((types[last] & CloseFlag) != 0)
            {
                segments.Add(new DiagramPathSegmentModel(DiagramPathOperation.Close, []));
            }

            i = last;
        }

        return segments;
    }
}
