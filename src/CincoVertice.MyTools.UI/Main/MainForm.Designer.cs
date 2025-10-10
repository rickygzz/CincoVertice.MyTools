namespace CincoVertice.MyTools.UI.Main
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
            NotifyIconControl = new NotifyIcon(components);
            NotifyContextMenu = new ContextMenuStrip(components);
            ExitNotifyContextMenu = new ToolStripMenuItem();
            NotifyContextMenu.SuspendLayout();
            SuspendLayout();
            // 
            // NotifyIconControl
            // 
            NotifyIconControl.BalloonTipText = "Info";
            NotifyIconControl.BalloonTipTitle = "My Tools";
            NotifyIconControl.ContextMenuStrip = NotifyContextMenu;
            NotifyIconControl.Icon = UI.Resources.Resources.RGIcon;
            NotifyIconControl.Text = "My Tools";
            NotifyIconControl.Visible = true;
            // 
            // NotifyContextMenu
            // 
            NotifyContextMenu.ImageScalingSize = new Size(24, 24);
            NotifyContextMenu.Items.AddRange(new ToolStripItem[] { ExitNotifyContextMenu });
            NotifyContextMenu.Name = "NotifyContextMenu";
            NotifyContextMenu.Size = new Size(112, 36);
            // 
            // ExitNotifyContextMenu
            // 
            ExitNotifyContextMenu.Name = "ExitNotifyContextMenu";
            ExitNotifyContextMenu.Size = new Size(111, 32);
            ExitNotifyContextMenu.Text = "&Exit";
            ExitNotifyContextMenu.Click += ExitNotifyContextMenu_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1329, 750);
            Icon = UI.Resources.Resources.RGIcon;
            Margin = new Padding(4, 5, 4, 5);
            Name = "MainForm";
            Text = "My Tools";
            NotifyContextMenu.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private NotifyIcon NotifyIconControl;
        private ContextMenuStrip NotifyContextMenu;
        private ToolStripMenuItem ExitNotifyContextMenu;
    }
}
