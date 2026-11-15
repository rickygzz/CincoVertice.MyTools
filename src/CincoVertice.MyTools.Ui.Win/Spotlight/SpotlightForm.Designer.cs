namespace CincoVertice.MyTools.Ui.Win.Spotlight;

partial class SpotlightForm
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
        TickTimer = new System.Windows.Forms.Timer(components);
        SuspendLayout();
        //
        // TickTimer
        //
        TickTimer.Interval = 15;
        TickTimer.Tick += TickTimer_Tick;
        //
        // SpotlightForm
        //
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        ClientSize = new Size(800, 600);
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None;
        Name = "SpotlightForm";
        Opacity = 0.6D;
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "Mouse spotlight";
        Paint += SpotlightForm_Paint;
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Timer TickTimer;
}
