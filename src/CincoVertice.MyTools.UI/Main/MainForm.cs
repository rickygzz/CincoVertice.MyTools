namespace CincoVertice.MyTools
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void ExitNotifyContextMenu_Click(object sender, EventArgs e)
        {
            Dispose();
            Application.Exit();
        }
    }
}
