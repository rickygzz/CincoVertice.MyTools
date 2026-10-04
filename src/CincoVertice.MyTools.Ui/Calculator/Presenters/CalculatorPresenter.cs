using CincoVertice.Common.Application.Calculator;
using CincoVertice.MyTools.Ui.Calculator.Views;

namespace CincoVertice.MyTools.Ui.Calculator.Presenters;

/// <summary>
///     Shows the calculator view, evaluates the expression typed in it, and shows the result or the error.
/// </summary>
public class CalculatorPresenter
{
    private readonly ICalculatorView _view;
    private readonly ExpressionCalculator _calculator;

    public CalculatorPresenter(ICalculatorView view, ExpressionCalculator calculator)
    {
        _view = view;
        _calculator = calculator;

        _view.EvaluateRequested += OnEvaluateRequested;
    }

    public void Show()
    {
        _view.ShowAndActivate();
    }

    /// <summary>
    ///     Evaluates the view's expression. An empty expression is ignored.
    /// </summary>
    public void Evaluate()
    {
        string expression = _view.Expression;

        if (string.IsNullOrWhiteSpace(expression))
        {
            return;
        }

        try
        {
            decimal result = _calculator.Evaluate(expression);

            _view.ShowResult(ExpressionCalculator.Format(result, groupThousands: true));
        }
        catch (ExpressionException ex)
        {
            // Select the character at fault, so it can be fixed right away; past the end, put the caret there
            int start = Math.Min(ex.Position, expression.Length);
            int length = ex.Position < expression.Length ? 1 : 0;

            _view.ShowError(ex.Message, start, length);
        }
    }

    private void OnEvaluateRequested(object? sender, EventArgs e)
    {
        Evaluate();
    }
}
