using System.Globalization;
using CincoVertice.Common.Application.Calculator;
using CincoVertice.MyTools.Ui.Calculator.Models;
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

    [Fact]
    public void ToggleHistoryRequested_BeforeAnyOperation_ShowsThatThereAreNone()
    {
        // Act
        _view.RequestToggleHistory();

        // Assert
        Assert.Equal([], _view.History!);
        Assert.Equal(CalculatorPresenter.NoHistoryText, _view.HistoryEmptyText);
    }

    [Fact]
    public void ToggleHistoryRequested_ShowsEachOperationOldestFirst()
    {
        // Arrange
        Calculate("2+2*3");
        Calculate("  10000 ");

        // Act
        _view.RequestToggleHistory();

        // Assert
        Assert.Equal(
            [
                new CalculatorHistoryEntryModel("2+2*3", "8"),
                new CalculatorHistoryEntryModel("10000", "10,000"),
            ],
            _view.History!);
    }

    [Theory]
    [InlineData("2+)")]
    [InlineData("")]
    [InlineData("   ")]
    public void History_LeavesOutWhatCouldNotBeEvaluated(string expression)
    {
        // Arrange
        Calculate("1+1");
        Calculate(expression);

        // Act
        _view.RequestToggleHistory();

        // Assert
        Assert.Equal(new CalculatorHistoryEntryModel("1+1", "2"), Assert.Single(_view.History!));
    }

    [Fact]
    public void History_KeepsTheNewestOperations()
    {
        // Arrange
        for (int i = 1; i <= CalculatorPresenter.MaxHistory + 1; i++)
        {
            Calculate(i.ToString(CultureInfo.InvariantCulture));
        }

        // Act
        _view.RequestToggleHistory();

        // Assert
        Assert.Equal(CalculatorPresenter.MaxHistory, _view.History!.Count);
        Assert.Equal(new CalculatorHistoryEntryModel("2", "2"), _view.History[0]);
    }

    [Fact]
    public void EvaluateRequested_WhileTheHistoryIsShown_AddsToItInsteadOfShowingTheResult()
    {
        // Arrange
        _view.RequestToggleHistory();

        // Act
        Calculate("1+1");

        // Assert
        Assert.Equal(new CalculatorHistoryEntryModel("1+1", "2"), Assert.Single(_view.History!));
        Assert.Null(_view.Result);
    }

    [Fact]
    public void EvaluateRequested_WhenErrorWhileTheHistoryIsShown_DrawsTheHistoryAndThenTheError()
    {
        // Arrange
        Calculate("1+1");
        _view.RequestToggleHistory();
        _view.Calls.Clear();

        // Act
        Calculate("2+)");
        Calculate("3+)");

        // Assert
        Assert.Equal(["History", "Error", "History", "Error"], _view.Calls);
    }

    [Fact]
    public void ToggleHistoryRequested_Twice_HidesItAndShowsTheCurrentResult()
    {
        // Arrange
        _view.RequestToggleHistory();
        Calculate("1+1");
        _view.Calls.Clear();

        // Act
        _view.RequestToggleHistory();

        // Assert
        Assert.Equal(["HideHistory", "Result"], _view.Calls);
        Assert.Equal("2", _view.Result);
    }

    [Fact]
    public void ToggleHistoryRequested_Twice_AfterAnError_ShowsTheErrorAgain()
    {
        // Arrange
        Calculate("2+)");
        _view.RequestToggleHistory();
        _view.Calls.Clear();

        // Act
        _view.RequestToggleHistory();

        // Assert
        Assert.Equal(["HideHistory", "Error"], _view.Calls);
        Assert.Equal((2, 1), _view.Selection);
    }

    [Fact]
    public void ToggleHistoryRequested_Twice_BeforeAnyOperation_ShowsAnEmptyResult()
    {
        // Act
        _view.RequestToggleHistory();
        _view.RequestToggleHistory();

        // Assert
        Assert.Equal(string.Empty, _view.Result);
    }

    private void Calculate(string expression)
    {
        _view.Expression = expression;
        _view.RequestEvaluate();
    }

    private sealed class FakeCalculatorView : ICalculatorView
    {
        public event EventHandler? EvaluateRequested;

        public event EventHandler? ToggleHistoryRequested;

        public List<string> Calls { get; } = [];

        public IReadOnlyList<CalculatorHistoryEntryModel>? History { get; private set; }

        public string? HistoryEmptyText { get; private set; }

        public string Expression { get; set; } = string.Empty;

        public int ShowCount { get; private set; }

        public string? Result { get; private set; }

        public string? Error { get; private set; }

        public (int Start, int Length) Selection { get; private set; }

        public void RequestEvaluate() => EvaluateRequested?.Invoke(this, EventArgs.Empty);

        public void ShowAndActivate() => ShowCount++;

        public void RequestToggleHistory() => ToggleHistoryRequested?.Invoke(this, EventArgs.Empty);

        public void ShowResult(string result)
        {
            Calls.Add("Result");
            Result = result;
        }

        public void ShowHistory(IReadOnlyList<CalculatorHistoryEntryModel> entries, string emptyText)
        {
            Calls.Add("History");
            History = [.. entries];
            HistoryEmptyText = emptyText;
        }

        public void HideHistory() => Calls.Add("HideHistory");

        public void ShowError(string message, int selectionStart, int selectionLength)
        {
            Calls.Add("Error");
            Error = message;
            Selection = (selectionStart, selectionLength);
        }
    }
}
