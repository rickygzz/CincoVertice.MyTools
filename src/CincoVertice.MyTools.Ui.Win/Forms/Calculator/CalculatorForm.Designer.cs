namespace CincoVertice.MyTools.Ui.Win.Forms.Calculator;

partial class CalculatorForm
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
        if (disposing && (components != null))
        {
            components.Dispose();
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
        CalculatorTextBox = new TextBox();
        ResultOutputControl = new CincoVertice.Common.Ui.Win.Controls.Rtf.ConsoleOutputControl();
        SuspendLayout();
        // 
        // CalculatorTextBox
        // 
        CalculatorTextBox.Font = new Font("Segoe UI", 14F);
        CalculatorTextBox.Location = new Point(5, 5);
        CalculatorTextBox.Name = "CalculatorTextBox";
        CalculatorTextBox.PlaceholderText = "Enter expression (e.g. 2+2*3)";
        CalculatorTextBox.Size = new Size(790, 32);
        CalculatorTextBox.TabIndex = 0;
        CalculatorTextBox.MouseDown += Calculator_MouseDown;
        CalculatorTextBox.MouseMove += Calculator_MouseMove;
        CalculatorTextBox.MouseUp += Calculator_MouseUp;
        // 
        // ResultOutputControl
        // 
        ResultOutputControl.BackColor = SystemColors.ControlDarkDark;
        ResultOutputControl.DetectUrls = false;
        ResultOutputControl.Location = new Point(5, 47);
        ResultOutputControl.Name = "ResultOutputControl";
        ResultOutputControl.ReadOnly = true;
        ResultOutputControl.Size = new Size(790, 38);
        ResultOutputControl.TabIndex = 1;
        ResultOutputControl.Text = "";
        ResultOutputControl.MouseDown += Calculator_MouseDown;
        ResultOutputControl.MouseMove += Calculator_MouseMove;
        ResultOutputControl.MouseUp += Calculator_MouseUp;
        // 
        // CalculatorForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 90);
        ControlBox = false;
        Controls.Add(ResultOutputControl);
        Controls.Add(CalculatorTextBox);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        KeyPreview = true;
        Name = "CalculatorForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Deactivate += CalculatorForm_Deactivate;
        FormClosing += CalculatorForm_FormClosing;
        VisibleChanged += CalculatorForm_VisibleChanged;
        KeyDown += CalculatorForm_KeyDown;
        MouseDown += Calculator_MouseDown;
        MouseMove += Calculator_MouseMove;
        MouseUp += Calculator_MouseUp;
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TextBox CalculatorTextBox;
    private Common.Ui.Win.Controls.Rtf.ConsoleOutputControl ResultOutputControl;
}
