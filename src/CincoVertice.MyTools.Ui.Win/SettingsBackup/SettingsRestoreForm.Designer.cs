namespace CincoVertice.MyTools.Ui.Win.SettingsBackup;

partial class SettingsRestoreForm
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
        SummaryLabel = new Label();
        FilesList = new ListView();
        FileColumn = new ColumnHeader();
        ChangeColumn = new ColumnHeader();
        DeleteCheck = new CheckBox();
        NoteLabel = new Label();
        CompareFilesButton = new Button();
        RestoreButton = new Button();
        CancelRestoreButton = new Button();
        SuspendLayout();
        //
        // SummaryLabel
        //
        SummaryLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        SummaryLabel.Location = new Point(12, 12);
        SummaryLabel.Name = "SummaryLabel";
        SummaryLabel.Size = new Size(616, 40);
        SummaryLabel.TabIndex = 0;
        //
        // FilesList
        //
        FilesList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        FilesList.Columns.AddRange(new ColumnHeader[] { FileColumn, ChangeColumn });
        FilesList.FullRowSelect = true;
        FilesList.HideSelection = false;
        FilesList.Location = new Point(12, 56);
        FilesList.MultiSelect = false;
        FilesList.Name = "FilesList";
        FilesList.Size = new Size(616, 250);
        FilesList.TabIndex = 1;
        FilesList.UseCompatibleStateImageBehavior = false;
        FilesList.View = View.Details;
        FilesList.SelectedIndexChanged += FilesList_SelectedIndexChanged;
        FilesList.DoubleClick += FilesList_DoubleClick;
        //
        // FileColumn
        //
        FileColumn.Text = "File";
        FileColumn.Width = 390;
        //
        // ChangeColumn
        //
        ChangeColumn.Text = "Restore";
        ChangeColumn.Width = 200;
        //
        // DeleteCheck
        //
        DeleteCheck.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        DeleteCheck.AutoSize = true;
        DeleteCheck.Location = new Point(12, 314);
        DeleteCheck.Name = "DeleteCheck";
        DeleteCheck.Size = new Size(250, 19);
        DeleteCheck.TabIndex = 2;
        DeleteCheck.UseVisualStyleBackColor = true;
        DeleteCheck.CheckedChanged += DeleteCheck_CheckedChanged;
        //
        // NoteLabel
        //
        NoteLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        NoteLabel.Location = new Point(12, 340);
        NoteLabel.Name = "NoteLabel";
        NoteLabel.Size = new Size(616, 44);
        NoteLabel.TabIndex = 3;
        //
        // CompareFilesButton
        //
        CompareFilesButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        CompareFilesButton.Location = new Point(12, 392);
        CompareFilesButton.Name = "CompareFilesButton";
        CompareFilesButton.Size = new Size(100, 28);
        CompareFilesButton.TabIndex = 4;
        CompareFilesButton.Text = "&Compare";
        CompareFilesButton.UseVisualStyleBackColor = true;
        CompareFilesButton.Click += CompareFilesButton_Click;
        //
        // RestoreButton
        //
        RestoreButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        RestoreButton.DialogResult = DialogResult.OK;
        RestoreButton.Location = new Point(422, 392);
        RestoreButton.Name = "RestoreButton";
        RestoreButton.Size = new Size(100, 28);
        RestoreButton.TabIndex = 5;
        RestoreButton.Text = "&Restore";
        RestoreButton.UseVisualStyleBackColor = true;
        //
        // CancelRestoreButton
        //
        CancelRestoreButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        CancelRestoreButton.DialogResult = DialogResult.Cancel;
        CancelRestoreButton.Location = new Point(528, 392);
        CancelRestoreButton.Name = "CancelRestoreButton";
        CancelRestoreButton.Size = new Size(100, 28);
        CancelRestoreButton.TabIndex = 6;
        CancelRestoreButton.Text = "Cancel";
        CancelRestoreButton.UseVisualStyleBackColor = true;
        //
        // SettingsRestoreForm
        //
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        CancelButton = CancelRestoreButton;
        ClientSize = new Size(640, 432);
        Controls.Add(SummaryLabel);
        Controls.Add(FilesList);
        Controls.Add(DeleteCheck);
        Controls.Add(NoteLabel);
        Controls.Add(CompareFilesButton);
        Controls.Add(RestoreButton);
        Controls.Add(CancelRestoreButton);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(480, 360);
        Name = "SettingsRestoreForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "Restore settings";
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label SummaryLabel;
    private ListView FilesList;
    private ColumnHeader FileColumn;
    private ColumnHeader ChangeColumn;
    private CheckBox DeleteCheck;
    private Label NoteLabel;
    private Button CompareFilesButton;
    private Button RestoreButton;
    private Button CancelRestoreButton;
}
