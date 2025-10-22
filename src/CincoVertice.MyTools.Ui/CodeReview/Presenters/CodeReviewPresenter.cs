using CincoVertice.MyTools.Ui.CodeReview.Views;

namespace CincoVertice.MyTools.Ui.CodeReview.Presenters;

public class CodeReviewPresenter
{
    private readonly ICodeReviewView _view;

    public CodeReviewPresenter(ICodeReviewView view)
    {
        _view = view;

        _view.LoadCodeReview += OnLoadCodeReview;
    }

    private void OnLoadCodeReview(object? sender, EventArgs e)
    {
        // _view.DisplayMessage("Code Review Loaded.");
    }
}
