using CincoVertice.MyTools.UI.CodeReview;
using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.UI.Win.Main;

public partial class MainForm : Form
{
    private readonly IServiceProvider _serviceProvider;

    public MainForm(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider;
    }

    private void ExitNotifyContextMenu_Click(object sender, EventArgs e)
    {
        Dispose();
        Application.Exit();
    }

    private void CodeReviewMenu_Click(object sender, EventArgs e)
    {
        var form = _serviceProvider.GetRequiredService<CodeReviewForm>();

        form.Show();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        CodeReviewMenu_Click(sender, e);
    }
}
