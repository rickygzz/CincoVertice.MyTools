using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Spotlight.Views;

namespace CincoVertice.MyTools.Ui.Win.Spotlight;

/// <summary>
///     Mouse spotlight view: covers every monitor, dimmed, but for a see-through circle with a ring around the mouse
///     pointer. It never takes the mouse or the focus, so the user goes on working under it.
///     <see cref="Ui.Spotlight.Presenters.SpotlightPresenter"/> decides where the circle is and how large.
///     <para>Its window is created per-monitor DPI aware, so its pixels are the screen's physical pixels.</para>
/// </summary>
public partial class SpotlightForm : Form, ISpotlightView
{
    // Drawn nowhere else, so the circle is see-through
    private static readonly Color _transparent = Color.FromArgb(255, 0, 255);

    private const int RingWidth = 4;

    // The circle drawn last, in client pixels, to repaint only what changes
    private Rectangle _circle;

    public SpotlightForm()
    {
        InitializeComponent();
        TransparencyKey = _transparent;
    }

    public event EventHandler? Tick;

    public Point CursorPosition
    {
        get
        {
            // Per-monitor aware, Windows gives physical pixels on every monitor
            using (DpiAwarenessScope.PerMonitorV2())
            {
                return Cursor.Position;
            }
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;

            // The mouse goes through it, and it never becomes the active window or shows in Alt+Tab. On top from the
            // start: the TopMost property would activate it
            parameters.ExStyle |=
                (int)(WS.WS_EX_TRANSPARENT | WS.WS_EX_NOACTIVATE | WS.WS_EX_TOOLWINDOW | WS.WS_EX_TOPMOST);

            return parameters;
        }
    }

    public void ShowSpotlight(Point center, int radius)
    {
        // The window is made, sized and shown per-monitor aware; the monitors may have changed since last time
        if (!Visible)
        {
            using (DpiAwarenessScope.PerMonitorV2())
            {
                if (!IsHandleCreated)
                {
                    CreateHandle();
                }

                Bounds = SystemInformation.VirtualScreen;
                _circle = Rectangle.Empty;
                Show();
            }

            TickTimer.Start();
        }

        int scaled = radius * DeviceDpi / 96;
        var circle = new Rectangle(center.X - Left - scaled, center.Y - Top - scaled, scaled * 2, scaled * 2);

        if (circle == _circle)
        {
            return;
        }

        Rectangle changed = _circle.IsEmpty ? ClientRectangle : Rectangle.Union(_circle, circle);
        changed.Inflate(RingWidth * 2, RingWidth * 2);
        _circle = circle;
        Invalidate(changed);
    }

    public void HideSpotlight()
    {
        TickTimer.Stop();
        Hide();
    }

    private void TickTimer_Tick(object? sender, EventArgs e)
    {
        Tick?.Invoke(this, EventArgs.Empty);
    }

    private void SpotlightForm_Paint(object? sender, PaintEventArgs e)
    {
        if (_circle.IsEmpty)
        {
            return;
        }

        // No smoothing: an edge blended with the see-through color would show pink
        using var hole = new SolidBrush(_transparent);
        using var ring = new Pen(Color.White, RingWidth);
        e.Graphics.FillEllipse(hole, _circle);
        e.Graphics.DrawEllipse(ring, _circle);
    }
}
