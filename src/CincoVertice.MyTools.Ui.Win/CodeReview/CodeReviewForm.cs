using CincoVertice.MyTools.Ui.CodeReview.Views;
using CincoVertice.MyTools.Ui.Win.CodeReview.Views;

namespace CincoVertice.MyTools.Ui.Win.CodeReview;

public partial class CodeReviewForm : Form, ICodeReviewView
{
    public event EventHandler? LoadCodeReview;

    public readonly ConsoleOutputView _consoleOutput;

    public CodeReviewForm()
    {
        InitializeComponent();

        _consoleOutput = new ConsoleOutputView(ConsoleOutput);
    }

    private void CodeReviewForm_Load(object sender, EventArgs e)
    {
        LoadCodeReview?.Invoke(this, EventArgs.Empty);
        _consoleOutput.OnLoadConsoleOuput(this, EventArgs.Empty);
    }

    public void InitializeConsoleOutput()
    {
        throw new NotImplementedException();
    }
}
