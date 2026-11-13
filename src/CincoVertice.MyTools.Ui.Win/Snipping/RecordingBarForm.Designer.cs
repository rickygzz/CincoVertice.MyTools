namespace CincoVertice.MyTools.Ui.Win.Snipping;

partial class RecordingBarForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        TimeLabel = new Label();
        StopButton = new Button();
        TickTimer = new System.Windows.Forms.Timer(components);
        SuspendLayout();
        //
        // TimeLabel
        //
        TimeLabel.Cursor = Cursors.SizeAll;
        TimeLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        TimeLabel.ForeColor = Color.FromArgb(230, 30, 30);
        TimeLabel.Location = new Point(8, 6);
        TimeLabel.Name = "TimeLabel";
        TimeLabel.Size = new Size(90, 24);
        TimeLabel.Text = "● 0:00";
        TimeLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // StopButton
        //
        StopButton.Location = new Point(102, 5);
        StopButton.Name = "StopButton";
        StopButton.Size = new Size(70, 26);
        StopButton.Text = "■ Stop";
        StopButton.UseVisualStyleBackColor = true;
        StopButton.Click += StopButton_Click;
        //
        // TickTimer
        //
        TickTimer.Interval = 250;
        TickTimer.Tick += TickTimer_Tick;
        //
        // RecordingBarForm
        //
        AutoScaleMode = AutoScaleMode.None;
        BackColor = SystemColors.Window;
        ClientSize = new Size(180, 36);
        Controls.Add(TimeLabel);
        Controls.Add(StopButton);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Name = "RecordingBarForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "Recording (Ctrl+Alt+R stops)";
        FormClosed += RecordingBarForm_FormClosed;
        ResumeLayout(false);
    }

    #endregion

    private Label TimeLabel;
    private Button StopButton;
    private System.Windows.Forms.Timer TickTimer;
}
