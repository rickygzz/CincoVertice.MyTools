namespace CincoVertice.MyTools.Ui.Win.Snipping;

partial class SnipOverlayForm
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
            _dimmed.Dispose();
            _borderPen.Dispose();
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
        // SnipOverlayForm
        //
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        ClientSize = new Size(800, 450);
        Cursor = Cursors.Cross;
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None;
        KeyPreview = true;
        Name = "SnipOverlayForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "Snip";
        TopMost = true;
        Deactivate += SnipOverlayForm_Deactivate;
        FormClosed += SnipOverlayForm_FormClosed;
        Paint += SnipOverlayForm_Paint;
        KeyDown += SnipOverlayForm_KeyDown;
        MouseDown += SnipOverlayForm_MouseDown;
        MouseMove += SnipOverlayForm_MouseMove;
        MouseUp += SnipOverlayForm_MouseUp;
        ResumeLayout(false);
    }

    #endregion
}
