using CincoVertice.Common.Application.Calculator;
using CincoVertice.MyTools.Ui.Calculator.Presenters;
using CincoVertice.MyTools.Ui.Calculator.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Calculator;

public class CalculatorPresenterTests
{
    private readonly FakeCalculatorView _view = new();
    private readonly CalculatorPresenter _presenter;

    public CalculatorPresenterTests()
    {
        _presenter = new CalculatorPresenter(_view, new ExpressionCalculator());
    }

    [Fact]
    public void Show_ShowsAndActivatesTheView()
    {
        // Act
        _presenter.Show();

        // Assert
        Assert.Equal(1, _view.ShowCount);
    }

    [Theory]
    [InlineData("2+2*3", "8")]
    [InlineData("10000", "10,000")]
    [InlineData("1/4", "0.25")]
    public void EvaluateRequested_ShowsFormattedResult(string expression, string expected)
    {
        // Arrange
        _view.Expression = expression;

        // Act
        _view.RequestEvaluate();

        // Assert
        Assert.Equal(expected, _view.Result);
        Assert.Null(_view.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EvaluateRequested_WhenExpressionIsEmpty_ShowsNothing(string expression)
    {
        // Arrange
        _view.Expression = expression;

        // Act
        _view.RequestEvaluate();

        // Assert
        Assert.Null(_view.Result);
        Assert.Null(_view.Error);
    }

    [Fact]
    public void EvaluateRequested_WhenErrorIsInsideExpression_SelectsTheCharacterAtFault()
    {
        // Arrange
        _view.Expression = "2+)";

        // Act
        _view.RequestEvaluate();

        // Assert
        Assert.NotNull(_view.Error);
        Assert.Null(_view.Result);
        Assert.Equal((2, 1), _view.Selection);
    }

    [Fact]
    public void EvaluateRequested_WhenErrorIsAtTheEnd_PutsTheCaretAtTheEnd()
    {
        // Arrange
        _view.Expression = "2+";

        // Act
        _view.RequestEvaluate();

        // Assert
        Assert.NotNull(_view.Error);
        Assert.Equal((2, 0), _view.Selection);
    }

    private sealed class FakeCalculatorView : ICalculatorView
    {
        public event EventHandler? EvaluateRequested;

        public string Expression { get; set; } = string.Empty;

        public int ShowCount { get; private set; }

        public string? Result { get; private set; }

        public string? Error { get; private set; }

        public (int Start, int Length) Selection { get; private set; }

        public void RequestEvaluate() => EvaluateRequested?.Invoke(this, EventArgs.Empty);

        public void ShowAndActivate() => ShowCount++;

        public void ShowResult(string result) => Result = result;

        public void ShowError(string message, int selectionStart, int selectionLength)
        {
            Error = message;
            Selection = (selectionStart, selectionLength);
        }
    }
}
