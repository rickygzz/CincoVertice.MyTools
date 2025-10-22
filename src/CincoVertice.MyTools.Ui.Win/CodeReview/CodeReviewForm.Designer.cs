namespace CincoVertice.MyTools.Ui.Win.CodeReview;

partial class CodeReviewForm
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
        ConsoleOutput = new CincoVertice.Common.Ui.Win.Controls.Rtf.ConsoleOutputControl();
        SuspendLayout();
        // 
        // ConsoleOutput
        // 
        ConsoleOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        ConsoleOutput.Location = new Point(4, 108);
        ConsoleOutput.Name = "ConsoleOutput";
        ConsoleOutput.Size = new Size(913, 507);
        ConsoleOutput.TabIndex = 0;
        ConsoleOutput.Text = "";
        // 
        // CodeReviewForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(921, 619);
        Controls.Add(ConsoleOutput);
        Name = "CodeReviewForm";
        Text = "CodeReviewForm";
        Load += CodeReviewForm_Load;
        ResumeLayout(false);
    }

    #endregion

    private Common.Ui.Win.Controls.Rtf.ConsoleOutputControl ConsoleOutput;
}
