namespace CincoVertice.MyTools.Ui.Win.Snipping;

partial class RecordingFrameForm
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
            Region?.Dispose();
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
        SuspendLayout();
        //
        // RecordingFrameForm
        //
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(230, 30, 30);
        ClientSize = new Size(200, 100);
        FormBorderStyle = FormBorderStyle.None;
        Name = "RecordingFrameForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "Recording";
        ResumeLayout(false);
    }

    #endregion
}
