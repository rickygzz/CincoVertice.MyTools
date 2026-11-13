using System.Globalization;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Snipping.Views;
using CincoVertice.MyTools.Ui.Win.Behaviors;
using CincoVertice.MyTools.Ui.Win.Shell.Services;

namespace CincoVertice.MyTools.Ui.Win.Snipping;

/// <summary>
///     Recording view: a small bar with how long it has recorded and a Stop button, below the area or above it, with a
///     red frame around the area. Neither is in the video, and the bar never takes the focus from what's recorded. The
///     view <see cref="Ui.Snipping.Presenters.ScreenRecordingPresenter"/> drives; created per use, it disposes itself
///     when closed.
/// </summary>
public partial class RecordingBarForm : Form, IRecordingView
{
    public const string NotificationTitle = "My Tools";

    // Between the area and the bar, in pixels
    private const int Spacing = 6;

    private readonly TrayNotificationService _notifications;

    private RecordingFrameForm? _frame;
    private IDisposable? _exclusion;

    public RecordingBarForm(TrayNotificationService notifications)
    {
        InitializeComponent();

        _notifications = notifications;
        DragMover.Attach(this, TimeLabel);
    }

    public event EventHandler? StopRequested;

    public event EventHandler? Ticked;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;

            // On top from the start, as the TopMost property would activate it; not in Alt+Tab
            parameters.ExStyle |= (int)(WS.WS_EX_NOACTIVATE | WS.WS_EX_TOOLWINDOW | WS.WS_EX_TOPMOST);

            return parameters;
        }
    }

    public void ShowRecording(Rectangle area)
    {
        // Both made per-monitor aware, so their bounds are physical pixels, as the area is
        using (DpiAwarenessScope.PerMonitorV2())
        {
            _frame = new RecordingFrameForm(area);
            _frame.Show();

            CreateHandle();
            Location = PlaceBar(area, Size);
            Show();
        }

        _exclusion = CaptureExclusion.Exclude([Handle, _frame.Handle]);
        TickTimer.Start();
    }

    public void ShowElapsed(TimeSpan elapsed)
    {
        string format = elapsed.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss";
        TimeLabel.Text = "● " + elapsed.ToString(format, CultureInfo.InvariantCulture);
    }

    public void ShowSaving()
    {
        TickTimer.Stop();
        TimeLabel.Text = "Saving...";
        StopButton.Enabled = false;
    }

    public string? AskSavePath(string suggestedFileName)
    {
        Hide();
        _frame?.Hide();

        string videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        using var dialog = new SaveFileDialog
        {
            FileName = suggestedFileName,
            Filter = "MP4 video (*.mp4)|*.mp4",
            InitialDirectory = videos,
            Title = "Save recording",
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    public void ShowMessage(string message, bool isProblem)
    {
        _notifications.Show(NotificationTitle, message, isProblem ? ToolTipIcon.Warning : ToolTipIcon.Info);
    }

    public void CloseView()
    {
        Close();
    }

    // Below the area, or above it when there's no room, or else inside it at the bottom, within its screen
    private static Point PlaceBar(Rectangle area, Size bar)
    {
        Rectangle screen = Screen.FromRectangle(area).WorkingArea;
        int x = Math.Clamp(area.Right - bar.Width, screen.Left, Math.Max(screen.Left, screen.Right - bar.Width));
        int below = area.Bottom + RecordingFrameForm.Thickness + Spacing;
        int above = area.Top - RecordingFrameForm.Thickness - Spacing - bar.Height;

        int y = below + bar.Height <= screen.Bottom ? below
            : above >= screen.Top ? above
            : area.Bottom - bar.Height - Spacing;

        return new Point(x, y);
    }

    private void StopButton_Click(object? sender, EventArgs e) => StopRequested?.Invoke(this, EventArgs.Empty);

    private void TickTimer_Tick(object? sender, EventArgs e) => Ticked?.Invoke(this, EventArgs.Empty);

    private void RecordingBarForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        TickTimer.Stop();
        _exclusion?.Dispose();
        _frame?.Close();
        _frame?.Dispose();
        _frame = null;
    }
}
