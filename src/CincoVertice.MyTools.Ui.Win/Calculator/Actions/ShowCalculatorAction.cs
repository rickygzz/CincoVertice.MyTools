using CincoVertice.MyTools.Ui.Calculator.Presenters;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;

namespace CincoVertice.MyTools.Ui.Win.Calculator.Actions;

/// <summary>
///     Shows the calculator, e.g. from the global hotkey.
/// </summary>
public sealed class ShowCalculatorAction : IAppAction
{
    private readonly CalculatorPresenter _presenter;

    public ShowCalculatorAction(CalculatorPresenter presenter)
    {
        _presenter = presenter;
    }

    public string Name => "ShowCalculator";

    public string? DefaultShortcut => "Ctrl+Alt+C";

    public Task ExecuteAsync()
    {
        _presenter.Show();

        return Task.CompletedTask;
    }
}
