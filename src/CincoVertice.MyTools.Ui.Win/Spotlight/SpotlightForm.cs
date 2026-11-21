using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Spotlight.Views;

namespace CincoVertice.MyTools.Ui.Win.Spotlight;

/// <summary>
///     Spotlight view: covers every monitor, dimmed, but for a see-through circle with a ring around the mouse pointer
///     (<see cref="ISpotlightView"/>), or the active window with a frame around it (<see cref="IAppSpotlightView"/>);
///     one instance for each. It never takes the mouse or the focus, so the user goes on working under it.
///     <see cref="Ui.Spotlight.Presenters.SpotlightPresenter"/> and
///     <see cref="Ui.Spotlight.Presenters.AppSpotlightPresenter"/> decide where the hole is and how large.
///     <para>Its window is created per-monitor DPI aware, so its pixels are the screen's physical pixels.</para>
/// </summary>
public partial class SpotlightForm : Form, ISpotlightView, IAppSpotlightView
{
    // Drawn nowhere else, so the hole is see-through
    private static readonly Color _transparent = Color.FromArgb(255, 0, 255);

    private const int RingWidth = 4;

    // The hole drawn last, in client pixels, to repaint only what changes: a circle, or a window's frame
    private Rectangle _hole;
    private bool _isRound;

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

    public Rectangle? ActiveWindowBounds
    {
        get
        {
            nint window = WinUser.GetForegroundWindow();

            if (!WindowHelper.IsMovableWindow(window) || WinUser.IsIconic(window))
            {
                return null;
            }

            using (DpiAwarenessScope.PerMonitorV2())
            {
                return WindowHelper.GetVisibleBounds(window);
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

    /// <summary>The hole drawn last, in client pixels, and whether it's a circle.</summary>
    internal (Rectangle Hole, bool IsRound) Drawn => (_hole, _isRound);

    public void ShowSpotlight(Point center, int radius)
    {
        ShowWindow();
        int scaled = radius * DeviceDpi / 96;
        ShowHole(new Rectangle(center.X - Left - scaled, center.Y - Top - scaled, scaled * 2, scaled * 2), true);
    }

    public void ShowSpotlight(Rectangle window, int margin)
    {
        ShowWindow();
        int scaled = margin * DeviceDpi / 96;
        var hole = new Rectangle(window.Left - Left, window.Top - Top, window.Width, window.Height);
        hole.Inflate(scaled, scaled);
        ShowHole(hole, false);
    }

    public void HideSpotlight()
    {
        TickTimer.Stop();
        Hide();
    }

    // The window is made, sized and shown per-monitor aware; the monitors may have changed since last time
    private void ShowWindow()
    {
        if (Visible)
        {
            return;
        }

        using (DpiAwarenessScope.PerMonitorV2())
        {
            if (!IsHandleCreated)
            {
                CreateHandle();
            }

            Bounds = SystemInformation.VirtualScreen;
            _hole = Rectangle.Empty;
            Show();
        }

        TickTimer.Start();
    }

    private void ShowHole(Rectangle hole, bool isRound)
    {
        if (hole == _hole && isRound == _isRound)
        {
            return;
        }

        Rectangle changed = _hole.IsEmpty ? ClientRectangle : Rectangle.Union(_hole, hole);
        changed.Inflate(RingWidth * 2, RingWidth * 2);
        _hole = hole;
        _isRound = isRound;
        Invalidate(changed);
    }

    private void TickTimer_Tick(object? sender, EventArgs e)
    {
        Tick?.Invoke(this, EventArgs.Empty);
    }

    private void SpotlightForm_Paint(object? sender, PaintEventArgs e)
    {
        if (_hole.IsEmpty)
        {
            return;
        }

        // No smoothing: an edge blended with the see-through color would show pink
        using var hole = new SolidBrush(_transparent);
        using var ring = new Pen(Color.White, RingWidth);

        if (_isRound)
        {
            e.Graphics.FillEllipse(hole, _hole);
            e.Graphics.DrawEllipse(ring, _hole);
        }
        else
        {
            e.Graphics.FillRectangle(hole, _hole);
            e.Graphics.DrawRectangle(ring, _hole);
        }
    }
}
