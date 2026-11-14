namespace CincoVertice.MyTools.Ui.Win.ColorPicker;

partial class ColorForm
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
        ShadesLabel = new Label();
        ShadesPanel = new Panel();
        ChosenPanel = new Panel();
        HexLabel = new Label();
        HexTextBox = new TextBox();
        HexCopyButton = new Button();
        RgbLabel = new Label();
        RgbTextBox = new TextBox();
        RgbCopyButton = new Button();
        HslLabel = new Label();
        HslTextBox = new TextBox();
        HslCopyButton = new Button();
        HsvLabel = new Label();
        HsvTextBox = new TextBox();
        HsvCopyButton = new Button();
        UseAsBaseButton = new Button();
        BackToPickedButton = new Button();
        PickAnotherButton = new Button();
        CopiedLabel = new Label();
        HarmoniesLabel = new Label();
        DarkLabel = new Label();
        LightLabel = new Label();
        HarmoniesPanel = new Panel();
        SwatchToolTip = new ToolTip(components);
        SuspendLayout();
        //
        // ShadesLabel
        //
        ShadesLabel.AutoSize = true;
        ShadesLabel.Location = new Point(12, 9);
        ShadesLabel.Name = "ShadesLabel";
        ShadesLabel.Text = "Shades";
        //
        // ShadesPanel
        //
        ShadesPanel.Location = new Point(12, 32);
        ShadesPanel.Name = "ShadesPanel";
        ShadesPanel.Size = new Size(72, 332);
        ShadesPanel.TabIndex = 0;
        //
        // ChosenPanel
        //
        ChosenPanel.BorderStyle = BorderStyle.FixedSingle;
        ChosenPanel.Location = new Point(100, 32);
        ChosenPanel.Name = "ChosenPanel";
        ChosenPanel.Size = new Size(252, 100);
        ChosenPanel.TabIndex = 1;
        //
        // HexLabel
        //
        HexLabel.AutoSize = true;
        HexLabel.Location = new Point(100, 152);
        HexLabel.Name = "HexLabel";
        HexLabel.Text = "HEX";
        //
        // HexTextBox
        //
        HexTextBox.Font = new Font("Consolas", 10F);
        HexTextBox.Location = new Point(144, 148);
        HexTextBox.Name = "HexTextBox";
        HexTextBox.ReadOnly = true;
        HexTextBox.Size = new Size(144, 23);
        HexTextBox.TabIndex = 2;
        //
        // HexCopyButton
        //
        HexCopyButton.Location = new Point(294, 147);
        HexCopyButton.Name = "HexCopyButton";
        HexCopyButton.Size = new Size(58, 25);
        HexCopyButton.TabIndex = 3;
        HexCopyButton.Tag = HexTextBox;
        HexCopyButton.Text = "Copy";
        HexCopyButton.Click += CopyButton_Click;
        //
        // RgbLabel
        //
        RgbLabel.AutoSize = true;
        RgbLabel.Location = new Point(100, 186);
        RgbLabel.Name = "RgbLabel";
        RgbLabel.Text = "RGB";
        //
        // RgbTextBox
        //
        RgbTextBox.Font = new Font("Consolas", 10F);
        RgbTextBox.Location = new Point(144, 182);
        RgbTextBox.Name = "RgbTextBox";
        RgbTextBox.ReadOnly = true;
        RgbTextBox.Size = new Size(144, 23);
        RgbTextBox.TabIndex = 4;
        //
        // RgbCopyButton
        //
        RgbCopyButton.Location = new Point(294, 181);
        RgbCopyButton.Name = "RgbCopyButton";
        RgbCopyButton.Size = new Size(58, 25);
        RgbCopyButton.TabIndex = 5;
        RgbCopyButton.Tag = RgbTextBox;
        RgbCopyButton.Text = "Copy";
        RgbCopyButton.Click += CopyButton_Click;
        //
        // HslLabel
        //
        HslLabel.AutoSize = true;
        HslLabel.Location = new Point(100, 220);
        HslLabel.Name = "HslLabel";
        HslLabel.Text = "HSL";
        //
        // HslTextBox
        //
        HslTextBox.Font = new Font("Consolas", 10F);
        HslTextBox.Location = new Point(144, 216);
        HslTextBox.Name = "HslTextBox";
        HslTextBox.ReadOnly = true;
        HslTextBox.Size = new Size(144, 23);
        HslTextBox.TabIndex = 6;
        //
        // HslCopyButton
        //
        HslCopyButton.Location = new Point(294, 215);
        HslCopyButton.Name = "HslCopyButton";
        HslCopyButton.Size = new Size(58, 25);
        HslCopyButton.TabIndex = 7;
        HslCopyButton.Tag = HslTextBox;
        HslCopyButton.Text = "Copy";
        HslCopyButton.Click += CopyButton_Click;
        //
        // HsvLabel
        //
        HsvLabel.AutoSize = true;
        HsvLabel.Location = new Point(100, 254);
        HsvLabel.Name = "HsvLabel";
        HsvLabel.Text = "HSV";
        //
        // HsvTextBox
        //
        HsvTextBox.Font = new Font("Consolas", 10F);
        HsvTextBox.Location = new Point(144, 250);
        HsvTextBox.Name = "HsvTextBox";
        HsvTextBox.ReadOnly = true;
        HsvTextBox.Size = new Size(144, 23);
        HsvTextBox.TabIndex = 8;
        //
        // HsvCopyButton
        //
        HsvCopyButton.Location = new Point(294, 249);
        HsvCopyButton.Name = "HsvCopyButton";
        HsvCopyButton.Size = new Size(58, 25);
        HsvCopyButton.TabIndex = 9;
        HsvCopyButton.Tag = HsvTextBox;
        HsvCopyButton.Text = "Copy";
        HsvCopyButton.Click += CopyButton_Click;
        //
        // UseAsBaseButton
        //
        UseAsBaseButton.Location = new Point(100, 294);
        UseAsBaseButton.Name = "UseAsBaseButton";
        UseAsBaseButton.Size = new Size(110, 27);
        UseAsBaseButton.TabIndex = 10;
        UseAsBaseButton.Text = "Use as base";
        SwatchToolTip.SetToolTip(UseAsBaseButton, "Makes the shades and the colors that go well from the color chosen");
        UseAsBaseButton.Click += UseAsBaseButton_Click;
        //
        // BackToPickedButton
        //
        BackToPickedButton.Location = new Point(216, 294);
        BackToPickedButton.Name = "BackToPickedButton";
        BackToPickedButton.Size = new Size(136, 27);
        BackToPickedButton.TabIndex = 11;
        BackToPickedButton.Text = "Back to picked";
        SwatchToolTip.SetToolTip(BackToPickedButton, "Shows the color picked again, with its palette");
        BackToPickedButton.Click += BackToPickedButton_Click;
        //
        // PickAnotherButton
        //
        PickAnotherButton.Location = new Point(100, 328);
        PickAnotherButton.Name = "PickAnotherButton";
        PickAnotherButton.Size = new Size(252, 27);
        PickAnotherButton.TabIndex = 12;
        PickAnotherButton.Text = "Pick another color from the screen";
        PickAnotherButton.Click += PickAnotherButton_Click;
        //
        // CopiedLabel
        //
        CopiedLabel.AutoEllipsis = true;
        CopiedLabel.ForeColor = SystemColors.GrayText;
        CopiedLabel.Location = new Point(100, 364);
        CopiedLabel.Name = "CopiedLabel";
        CopiedLabel.Size = new Size(252, 20);
        //
        // HarmoniesLabel
        //
        HarmoniesLabel.AutoSize = true;
        HarmoniesLabel.Location = new Point(372, 9);
        HarmoniesLabel.Name = "HarmoniesLabel";
        HarmoniesLabel.Text = "Goes well with";
        //
        // DarkLabel
        //
        DarkLabel.AutoSize = true;
        DarkLabel.Location = new Point(488, 9);
        DarkLabel.Name = "DarkLabel";
        DarkLabel.Text = "Dark";
        //
        // LightLabel
        //
        LightLabel.AutoSize = true;
        LightLabel.Location = new Point(560, 9);
        LightLabel.Name = "LightLabel";
        LightLabel.Text = "Light";
        //
        // HarmoniesPanel
        //
        HarmoniesPanel.Location = new Point(372, 32);
        HarmoniesPanel.Name = "HarmoniesPanel";
        HarmoniesPanel.Size = new Size(252, 332);
        HarmoniesPanel.TabIndex = 13;
        //
        // ColorForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(636, 392);
        Controls.Add(ShadesLabel);
        Controls.Add(ShadesPanel);
        Controls.Add(ChosenPanel);
        Controls.Add(HexLabel);
        Controls.Add(HexTextBox);
        Controls.Add(HexCopyButton);
        Controls.Add(RgbLabel);
        Controls.Add(RgbTextBox);
        Controls.Add(RgbCopyButton);
        Controls.Add(HslLabel);
        Controls.Add(HslTextBox);
        Controls.Add(HslCopyButton);
        Controls.Add(HsvLabel);
        Controls.Add(HsvTextBox);
        Controls.Add(HsvCopyButton);
        Controls.Add(UseAsBaseButton);
        Controls.Add(BackToPickedButton);
        Controls.Add(PickAnotherButton);
        Controls.Add(CopiedLabel);
        Controls.Add(HarmoniesLabel);
        Controls.Add(DarkLabel);
        Controls.Add(LightLabel);
        Controls.Add(HarmoniesPanel);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        KeyPreview = true;
        MaximizeBox = false;
        Name = "ColorForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Color";
        KeyDown += ColorForm_KeyDown;
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label ShadesLabel;
    private Panel ShadesPanel;
    private Panel ChosenPanel;
    private Label HexLabel;
    private TextBox HexTextBox;
    private Button HexCopyButton;
    private Label RgbLabel;
    private TextBox RgbTextBox;
    private Button RgbCopyButton;
    private Label HslLabel;
    private TextBox HslTextBox;
    private Button HslCopyButton;
    private Label HsvLabel;
    private TextBox HsvTextBox;
    private Button HsvCopyButton;
    private Button UseAsBaseButton;
    private Button BackToPickedButton;
    private Button PickAnotherButton;
    private Label CopiedLabel;
    private Label HarmoniesLabel;
    private Label DarkLabel;
    private Label LightLabel;
    private Panel HarmoniesPanel;
    private ToolTip SwatchToolTip;
}
