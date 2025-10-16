using CincoVertice.MyTools.UI.Win.CodeReview.Adapters;
using CincoVertice.MyTools.UI.Win.CodeReview.Presenters;

namespace CincoVertice.MyTools.UI.Win.CodeReview;

public partial class CodeReviewForm : Form
{
    public CodeReviewForm()
    {
        InitializeComponent();

        var _presenter = new ConsoleOutputPresenter(new ConsoleOutputAdapter(ConsoleOutput));
        _presenter.Initialize();
    }
}
