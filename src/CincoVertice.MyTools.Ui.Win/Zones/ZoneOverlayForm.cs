using System.Drawing.Text;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Zones.Services;
using CincoVertice.MyTools.Ui.Zones.Views;

namespace CincoVertice.MyTools.Ui.Win.Zones;

/// <summary>
///     Zone overlay view: covers every monitor, see-through except for the zones, numbered, with the one the window
///     would go to in the accent color. It never takes the mouse or the focus, so the drag goes on under it.
///     <see cref="Ui.Zones.Presenters.ZoneDragPresenter"/> decides what it shows.
///     <para>Its window is created per-monitor DPI aware, so its pixels are the zones' physical pixels.</para>
/// </summary>
public partial class ZoneOverlayForm : Form, IZoneOverlayView
{
    public const string NotificationTitle = "My Tools";

    // Drawn nowhere else, so everything but the zones is see-through
    private static readonly Color _transparent = Color.FromArgb(255, 0, 255);

    private static readonly Color _zoneColor = Color.FromArgb(32, 32, 32);
    private static readonly Color _highlightColor = Color.FromArgb(0, 120, 215);

    private readonly TrayNotificationService _notifications;

    private IReadOnlyList<Rectangle> _zones = [];
    private int? _highlighted;

    public ZoneOverlayForm(TrayNotificationService notifications)
    {
        InitializeComponent();

        _notifications = notifications;
        BackColor = _transparent;
        TransparencyKey = _transparent;
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

    public void ShowZones(IReadOnlyList<Rectangle> zones, int? highlighted)
    {
        _zones = zones;
        _highlighted = highlighted;

        // The window is made, sized and shown per-monitor aware; the monitors may have changed since last time
        using (DpiAwarenessScope.PerMonitorV2())
        {
            if (!IsHandleCreated)
            {
                CreateHandle();
            }

            Bounds = SystemInformation.VirtualScreen;

            if (!Visible)
            {
                Show();
            }
        }

        Invalidate();
    }

    public void HideZones()
    {
        Hide();
    }

    public void ShowProblem(string message)
    {
        _notifications.Show(NotificationTitle, message, ToolTipIcon.Warning);
    }

    private void ZoneOverlayForm_Paint(object? sender, PaintEventArgs e)
    {
        Graphics graphics = e.Graphics;

        // No smoothing: an edge blended with the see-through color would show pink
        graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

        using var border = new Pen(Color.White, 2);
        using var numberBrush = new SolidBrush(Color.White);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };

        foreach (int i in ZoneCalculator.DrawingOrder(_zones.Count, _highlighted))
        {
            Rectangle zone = _zones[i];
            zone.Offset(-Left, -Top);

            using var fill = new SolidBrush(i == _highlighted ? _highlightColor : _zoneColor);
            graphics.FillRectangle(fill, zone);
            graphics.DrawRectangle(border, zone.X + 1, zone.Y + 1, zone.Width - 2, zone.Height - 2);

            float size = Math.Clamp(Math.Min(zone.Width, zone.Height) / 4f, 8f, 96f);
            using var font = new Font(Font.FontFamily, size, FontStyle.Bold, GraphicsUnit.Pixel);
            graphics.DrawString((i + 1).ToString(), font, numberBrush, zone, format);
        }
    }
}
