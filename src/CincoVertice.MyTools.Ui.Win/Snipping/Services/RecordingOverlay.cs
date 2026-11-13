using System.Drawing.Drawing2D;

namespace CincoVertice.MyTools.Ui.Win.Snipping.Services;

/// <summary>
///     What a recording draws over the screen: a ring that grows and fades where the mouse was clicked, and the last
///     shortcut pressed, in the bottom left corner. Clicks and keys come from the UI thread and frames are drawn on
///     another, so every access is locked.
/// </summary>
public sealed class RecordingOverlay
{
    private static readonly TimeSpan _clickTime = TimeSpan.FromMilliseconds(600);
    private static readonly TimeSpan _keysTime = TimeSpan.FromMilliseconds(1600);

    private static readonly Color _leftClick = Color.FromArgb(255, 200, 0);
    private static readonly Color _rightClick = Color.FromArgb(0, 150, 255);
    private static readonly Color _middleClick = Color.FromArgb(0, 200, 120);

    private readonly object _gate = new();
    private readonly List<(TimeSpan At, Point Point, MouseButtons Button)> _clicks = [];
    private (TimeSpan At, string Text)? _keys;

    /// <param name="at">When, in the video's time.</param>
    /// <param name="point">Where, in physical screen pixels.</param>
    /// <param name="button">Which button: the ring's color.</param>
    public void AddClick(TimeSpan at, Point point, MouseButtons button)
    {
        lock (_gate)
        {
            _clicks.Add((at, point, button));
        }
    }

    /// <param name="at">When, in the video's time.</param>
    /// <param name="text">The shortcut, e.g. "Ctrl+Shift+P"; it replaces the one shown.</param>
    public void AddKeys(TimeSpan at, string text)
    {
        lock (_gate)
        {
            _keys = (at, text);
        }
    }

    /// <param name="graphics">The frame.</param>
    /// <param name="area">Where the frame is on the screen.</param>
    /// <param name="now">The frame's time in the video.</param>
    public void Draw(Graphics graphics, Rectangle area, TimeSpan now)
    {
        List<(TimeSpan At, Point Point, MouseButtons Button)> clicks;
        (TimeSpan At, string Text)? keys;

        lock (_gate)
        {
            _clicks.RemoveAll(click => now - click.At > _clickTime);
            clicks = [.. _clicks];
            keys = _keys is { } shown && now - shown.At <= _keysTime ? shown : null;
        }

        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        foreach ((TimeSpan at, Point point, MouseButtons button) in clicks)
        {
            DrawClick(graphics, point - (Size)area.Location, (now - at) / _clickTime, button);
        }

        if (keys is (TimeSpan keysAt, string text))
        {
            DrawKeys(graphics, area.Size, text, (now - keysAt) / _keysTime);
        }
    }

    // A ring that grows and fades as the click gets older
    private static void DrawClick(Graphics graphics, Point center, double age, MouseButtons button)
    {
        age = Math.Clamp(age, 0, 1);
        float radius = (float)(10 + (22 * age));
        int alpha = (int)(230 * (1 - age));
        Color color = button switch
        {
            MouseButtons.Right => _rightClick,
            MouseButtons.Middle => _middleClick,
            _ => _leftClick,
        };

        using var pen = new Pen(Color.FromArgb(alpha, color), 4);
        graphics.DrawEllipse(pen, center.X - radius, center.Y - radius, radius * 2, radius * 2);
    }

    // White on dark, in the bottom left corner, fading out at the end
    private static void DrawKeys(Graphics graphics, Size frame, string text, double age)
    {
        double fade = age < 0.8 ? 1 : Math.Clamp((1 - age) / 0.2, 0, 1);
        float size = Math.Clamp(frame.Height / 18f, 14f, 32f);
        using var font = new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Pixel);
        SizeF measured = graphics.MeasureString(text, font);
        float padding = size / 2;
        float margin = size;
        var box = new RectangleF(
            margin,
            frame.Height - margin - measured.Height - (2 * padding),
            measured.Width + (2 * padding),
            measured.Height + (2 * padding));

        using var path = Rounded(box, size / 2);
        using var fill = new SolidBrush(Color.FromArgb((int)(200 * fade), 30, 30, 30));
        using var ink = new SolidBrush(Color.FromArgb((int)(255 * fade), Color.White));

        graphics.FillPath(fill, path);
        graphics.DrawString(text, font, ink, box.X + padding, box.Y + padding);
    }

    private static GraphicsPath Rounded(RectangleF box, float radius)
    {
        var path = new GraphicsPath();
        float diameter = radius * 2;

        path.AddArc(box.X, box.Y, diameter, diameter, 180, 90);
        path.AddArc(box.Right - diameter, box.Y, diameter, diameter, 270, 90);
        path.AddArc(box.Right - diameter, box.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(box.X, box.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}
