namespace CincoVertice.MyTools.Ui.Win.ColorPicker;

partial class ColorPickerOverlayForm
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
            _hintFont.Dispose();
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
        // ColorPickerOverlayForm
        //
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        ClientSize = new Size(800, 450);
        Cursor = Cursors.Cross;
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.None;
        KeyPreview = true;
        Name = "ColorPickerOverlayForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "Pick a color";
        TopMost = true;
        Deactivate += ColorPickerOverlayForm_Deactivate;
        FormClosed += ColorPickerOverlayForm_FormClosed;
        Shown += ColorPickerOverlayForm_Shown;
        Paint += ColorPickerOverlayForm_Paint;
        KeyDown += ColorPickerOverlayForm_KeyDown;
        MouseDown += ColorPickerOverlayForm_MouseDown;
        MouseMove += ColorPickerOverlayForm_MouseMove;
        MouseWheel += ColorPickerOverlayForm_MouseWheel;
        ResumeLayout(false);
    }

    #endregion
}
