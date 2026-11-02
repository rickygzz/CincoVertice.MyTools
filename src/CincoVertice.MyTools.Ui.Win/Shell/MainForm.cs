using CincoVertice.MyTools.Ui.Win.CodeReview.Services;

namespace CincoVertice.MyTools.Ui.Win.Shell;

public partial class MainForm : Form
{
    private readonly CodeReviewWindowService _codeReviewWindowService;

    public MainForm(CodeReviewWindowService codeReviewWindowService)
    {
        InitializeComponent();

        _codeReviewWindowService = codeReviewWindowService;
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        // CodeReviewMenu_Click(sender, e);
    }

    private void CodeReviewMenu_Click(object sender, EventArgs e)
    {
        _codeReviewWindowService.Open();
    }
}
