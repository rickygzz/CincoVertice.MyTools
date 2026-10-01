namespace CincoVertice.MyTools.Ui.Win.Main
{
    partial class MainForm
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
            MainMenu = new MenuStrip();
            FileMenu = new ToolStripMenuItem();
            CodeReviewMenu = new ToolStripMenuItem();
            MainMenu.SuspendLayout();
            SuspendLayout();
            //
            // MainMenu
            // 
            MainMenu.Items.AddRange(new ToolStripItem[] { FileMenu });
            MainMenu.Location = new Point(0, 0);
            MainMenu.Name = "MainMenu";
            MainMenu.Size = new Size(428, 24);
            MainMenu.TabIndex = 1;
            MainMenu.Text = "menuStrip1";
            // 
            // FileMenu
            // 
            FileMenu.DropDownItems.AddRange(new ToolStripItem[] { CodeReviewMenu });
            FileMenu.Name = "FileMenu";
            FileMenu.Size = new Size(37, 20);
            FileMenu.Text = "&File";
            // 
            // CodeReviewMenu
            // 
            CodeReviewMenu.Name = "CodeReviewMenu";
            CodeReviewMenu.Size = new Size(139, 22);
            CodeReviewMenu.Text = "Code review";
            CodeReviewMenu.Click += CodeReviewMenu_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(428, 300);
            Controls.Add(MainMenu);
            Icon = Resources.Resources.RGIcon;
            MainMenuStrip = MainMenu;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "My Tools";
            Load += MainForm_Load;
            MainMenu.ResumeLayout(false);
            MainMenu.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip MainMenu;
        private ToolStripMenuItem FileMenu;
        private ToolStripMenuItem CodeReviewMenu;
    }
}
