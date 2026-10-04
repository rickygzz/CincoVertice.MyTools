using CincoVertice.MyTools.Ui.Calculator.Presenters;

namespace CincoVertice.MyTools.Ui.Win.Calculator.Actions;

/// <summary>
///     Shows the calculator, e.g. from the global hotkey.
/// </summary>
public sealed class ShowCalculatorAction
{
    private readonly CalculatorPresenter _presenter;

    public ShowCalculatorAction(CalculatorPresenter presenter)
    {
        _presenter = presenter;
    }

    public Task ExecuteAsync()
    {
        _presenter.Show();

        return Task.CompletedTask;
    }
}
