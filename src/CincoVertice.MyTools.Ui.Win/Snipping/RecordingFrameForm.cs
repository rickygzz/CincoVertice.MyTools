using CincoVertice.Common.WinApi.Libs.Enums;

namespace CincoVertice.MyTools.Ui.Win.Snipping;

/// <summary>
///     A red frame just outside the area being recorded, so it's never in the video. The mouse goes through it, and it
///     never takes the focus. Create it per-monitor DPI aware, so its bounds are physical pixels, as the area is.
/// </summary>
public partial class RecordingFrameForm : Form
{
    public const int Thickness = 3;

    /// <param name="area">The area recorded, in physical screen pixels.</param>
    public RecordingFrameForm(Rectangle area)
    {
        InitializeComponent();

        Bounds = Rectangle.Inflate(area, Thickness, Thickness);

        // Only the frame: the area inside is cut out of the window
        var shape = new Region(new Rectangle(Point.Empty, Size));
        shape.Exclude(new Rectangle(Thickness, Thickness, area.Width, area.Height));
        Region = shape;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;

            // On top from the start, as the TopMost property would activate it
            parameters.ExStyle |=
                (int)(WS.WS_EX_TRANSPARENT | WS.WS_EX_NOACTIVATE | WS.WS_EX_TOOLWINDOW | WS.WS_EX_TOPMOST);

            return parameters;
        }
    }
}
