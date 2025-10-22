using CincoVertice.MyTools.Ui.CodeReview.Presenters;
using CincoVertice.MyTools.Ui.CodeReview.Views;
using CincoVertice.MyTools.Ui.Win.CodeReview;
using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.Ui.Win.Main;

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
        // Create a scope per window
        var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var form = sp.GetRequiredService<CodeReviewForm>();

        // Build presenter using the same form instance as ICodeReviewView
        _ = ActivatorUtilities.CreateInstance<CodeReviewPresenter>(sp, (ICodeReviewView)form);

        form.FormClosed += (_, _) => scope.Dispose();
        form.Show();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        CodeReviewMenu_Click(sender, e);
    }
}
