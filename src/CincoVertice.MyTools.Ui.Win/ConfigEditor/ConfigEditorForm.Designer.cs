namespace CincoVertice.MyTools.Ui.Win.ConfigEditor;

partial class ConfigEditorForm
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        ToolStrip = new ToolStrip();
        NewScriptButton = new ToolStripButton();
        SaveButton = new ToolStripButton();
        ZoomButton = new ToolStripButton();
        MainSplit = new SplitContainer();
        FilesList = new ListBox();
        CodeEditor = new CincoVertice.Common.Ui.Win.Controls.Rtf.CodeTextControl();
        ProblemsList = new ListBox();
        EditedTimer = new System.Windows.Forms.Timer(components);
        ToolStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)MainSplit).BeginInit();
        MainSplit.Panel1.SuspendLayout();
        MainSplit.Panel2.SuspendLayout();
        MainSplit.SuspendLayout();
        SuspendLayout();
        //
        // ToolStrip
        //
        ToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        ToolStrip.Items.AddRange(new ToolStripItem[] { NewScriptButton, SaveButton, ZoomButton });
        ToolStrip.Location = new Point(0, 0);
        ToolStrip.Name = "ToolStrip";
        ToolStrip.Size = new Size(884, 25);
        ToolStrip.TabIndex = 1;
        //
        // NewScriptButton
        //
        NewScriptButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        NewScriptButton.Name = "NewScriptButton";
        NewScriptButton.Size = new Size(70, 22);
        NewScriptButton.Text = "New script...";
        NewScriptButton.Click += NewScriptButton_Click;
        //
        // SaveButton
        //
        SaveButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        SaveButton.Name = "SaveButton";
        SaveButton.Size = new Size(35, 22);
        SaveButton.Text = "Save";
        SaveButton.ToolTipText = "Save (Ctrl+S)";
        SaveButton.Click += SaveButton_Click;
        //
        // ZoomButton
        //
        ZoomButton.Alignment = ToolStripItemAlignment.Right;
        ZoomButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        ZoomButton.Name = "ZoomButton";
        ZoomButton.Overflow = ToolStripItemOverflow.Never;
        ZoomButton.Text = "100%";
        ZoomButton.ToolTipText = "Ctrl+wheel to zoom. Click, or Ctrl+0, for 100%";
        ZoomButton.Click += ZoomButton_Click;
        //
        // MainSplit
        //
        MainSplit.Dock = DockStyle.Fill;
        MainSplit.FixedPanel = FixedPanel.Panel1;
        MainSplit.Location = new Point(0, 25);
        MainSplit.Name = "MainSplit";
        MainSplit.Size = new Size(884, 536);
        MainSplit.SplitterDistance = 190;
        MainSplit.TabIndex = 0;
        //
        // MainSplit.Panel1
        //
        MainSplit.Panel1.Controls.Add(FilesList);
        //
        // MainSplit.Panel2
        //
        MainSplit.Panel2.Controls.Add(CodeEditor);
        MainSplit.Panel2.Controls.Add(ProblemsList);
        //
        // FilesList
        //
        FilesList.BorderStyle = BorderStyle.None;
        FilesList.DisplayMember = "Name";
        FilesList.Dock = DockStyle.Fill;
        FilesList.IntegralHeight = false;
        FilesList.Name = "FilesList";
        FilesList.TabIndex = 0;
        FilesList.SelectedIndexChanged += FilesList_SelectedIndexChanged;
        //
        // CodeEditor
        //
        CodeEditor.BorderStyle = BorderStyle.None;
        CodeEditor.Dock = DockStyle.Fill;
        CodeEditor.Name = "CodeEditor";
        CodeEditor.TabIndex = 0;
        CodeEditor.Text = "";
        CodeEditor.TextChanged += CodeEditor_TextChanged;
        CodeEditor.ZoomChanged += CodeEditor_ZoomChanged;
        //
        // ProblemsList
        //
        ProblemsList.Dock = DockStyle.Bottom;
        ProblemsList.IntegralHeight = false;
        ProblemsList.Name = "ProblemsList";
        ProblemsList.Size = new Size(690, 90);
        ProblemsList.TabIndex = 1;
        ProblemsList.Visible = false;
        ProblemsList.SelectedIndexChanged += ProblemsList_SelectedIndexChanged;
        //
        // EditedTimer
        //
        EditedTimer.Interval = 200;
        EditedTimer.Tick += EditedTimer_Tick;
        //
        // ConfigEditorForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(884, 561);
        Controls.Add(MainSplit);
        Controls.Add(ToolStrip);
        MinimumSize = new Size(500, 300);
        Name = "ConfigEditorForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Config editor";
        Activated += ConfigEditorForm_Activated;
        FormClosing += ConfigEditorForm_FormClosing;
        ToolStrip.ResumeLayout(false);
        ToolStrip.PerformLayout();
        MainSplit.Panel1.ResumeLayout(false);
        MainSplit.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)MainSplit).EndInit();
        MainSplit.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private ToolStrip ToolStrip;
    private ToolStripButton NewScriptButton;
    private ToolStripButton SaveButton;
    private ToolStripButton ZoomButton;
    private SplitContainer MainSplit;
    private ListBox FilesList;
    private CincoVertice.Common.Ui.Win.Controls.Rtf.CodeTextControl CodeEditor;
    private ListBox ProblemsList;
    private System.Windows.Forms.Timer EditedTimer;
}
