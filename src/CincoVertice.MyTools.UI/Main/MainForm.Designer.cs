namespace CincoVertice.MyTools
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
            NotifyContextMenu.Items.AddRange(new ToolStripItem[] { ExitNotifyContextMenu });
            NotifyContextMenu.Name = "NotifyContextMenu";
            NotifyContextMenu.Size = new Size(181, 48);
            // 
            // ExitNotifyContextMenu
            // 
            ExitNotifyContextMenu.Name = "ExitNotifyContextMenu";
            ExitNotifyContextMenu.Size = new Size(180, 22);
            ExitNotifyContextMenu.Text = "&Exit";
            ExitNotifyContextMenu.Click += ExitNotifyContextMenu_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Icon = UI.Resources.Resources.RGIcon;
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
