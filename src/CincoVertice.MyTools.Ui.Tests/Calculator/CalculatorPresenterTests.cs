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
    [InlineData("3 km to m", "3,000 m")]
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

    [Fact]
    public void Suggestions_ATimeThenPlaces_WriteTheConversion_WhichCalculates()
    {
        // Arrange: in winter, so Mexico City is UTC-6
        var view = new FakeCalculatorView();
        var winter = new DateTime(2026, 1, 15, 12, 0, 0);
        var presenter = new CalculatorPresenter(view, new ExpressionCalculator(), () => winter);

        // Act / Assert: the first place writes "to", so the second follows
        view.Type("10:30 ");
        Assert.Equal("Mexico City (UTC-6)", view.Suggestions![0]);

        view.Choose(0);
        Assert.Equal(("10:30 Mexico City to ", 21), (view.Expression, view.Caret));
        Assert.Contains("India (UTC+5:30)", view.Suggestions!);

        view.Type("10:30 Mexico City to ind");
        Assert.Equal(["India (UTC+5:30)"], view.Suggestions);

        view.Choose(0);
        Assert.Equal("10:30 Mexico City to India", view.Expression);

        presenter.Evaluate();
        Assert.Null(view.Suggestions);
        Assert.Null(view.Error);
    }

    [Fact]
    public void Suggestions_ANumberThenUnits_WriteTheConversion_WithUnitsOfTheSameKind()
    {
        // Act / Assert
        _view.Type("10 ");
        Assert.Contains("km (kilometer, length)", _view.Suggestions!);
        Assert.Contains("kg (kilogram, mass)", _view.Suggestions!);

        _view.Choose(_view.Suggestions!.ToList().IndexOf("km (kilometer, length)"));
        Assert.Equal("10 km to ", _view.Expression);
        Assert.All(_view.Suggestions!, item => Assert.EndsWith("length)", item));
        Assert.DoesNotContain("km (kilometer, length)", _view.Suggestions!);

        _view.Type("10 km to mi");
        _view.Choose(0);
        Assert.Equal("10 km to mi", _view.Expression);
    }

    [Fact]
    public void Suggestions_AChoiceWhenNoneAreShown_DoesNothing()
    {
        // Arrange
        _view.Type("2+2");

        // Act
        _view.Choose(0);

        // Assert
        Assert.Null(_view.Suggestions);
        Assert.Equal("2+2", _view.Expression);
    }

    private void Enter(string expression)
    {
        _view.Type(expression);
        _view.RequestEvaluate();
    }

    [Fact]
    public void Recall_UpGoesBackThroughTheLinesEntered_AndDownPastTheNewestBringsBackWhatWasTyped()
    {
        // Arrange: a line that failed comes back too, to be fixed; the same line twice is there once
        Enter("1+1");
        Enter("2+");
        Enter("2+");
        _view.Type("3");

        // Act / Assert
        _view.Recall(-1);
        Assert.Equal(("2+", 2), (_view.Expression, _view.Caret));
        _view.Recall(-1);
        Assert.Equal("1+1", _view.Expression);
        _view.Recall(-1);
        Assert.Equal("1+1", _view.Expression);
        _view.Recall(1);
        Assert.Equal("2+", _view.Expression);
        _view.Recall(1);
        Assert.Equal("3", _view.Expression);
        _view.Recall(1);
        Assert.Equal("3", _view.Expression);
    }

    [Fact]
    public void Recall_ALineBroughtBack_OffersNoSuggestions_AndTypingStartsOver()
    {
        // Arrange
        Enter("10 km to mi");
        Enter("2+2");

        // Act / Assert: else Up would go through the suggestions
        _view.Recall(-1);
        _view.Recall(-1);
        Assert.Equal("10 km to mi", _view.Expression);
        Assert.Null(_view.Suggestions);

        _view.Type("5");
        _view.Recall(-1);
        Assert.Equal("2+2", _view.Expression);
    }

    [Fact]
    public void Recall_WithNothingEntered_DoesNothing()
    {
        // Arrange
        _view.Type("7");

        // Act
        _view.Recall(-1);
        _view.Recall(1);

        // Assert
        Assert.Equal("7", _view.Expression);
    }

    [Fact]
    public void Names_TheLastAnswerAndNamesGiven_AreKeptForTheNextLines()
    {
        // Act / Assert
        Enter("2+3");
        Enter("ans * 2");
        Assert.Equal("10", _view.Result);

        Enter("x = 4");
        Assert.Equal("4", _view.Result);

        Enter("x * ans");
        Assert.Equal("16", _view.Result);

        // A time in another place has no value, so the last answer stays
        Enter("10:00 to UTC");
        Enter("ans");
        Assert.Equal("16", _view.Result);
    }

    [Fact]
    public void Names_ALineThatFails_LeavesThemAsTheyWere()
    {
        // Arrange
        Enter("x = 4");

        // Act
        Enter("x = 1/0");
        Enter("x");

        // Assert
        Assert.Equal("4", _view.Result);
    }

    private sealed class FakeCalculatorView : ICalculatorView
    {
        public event EventHandler? EvaluateRequested;

        public event EventHandler? ToggleHistoryRequested;

        public event EventHandler? ExpressionChanged;

        public event EventHandler<int>? SuggestionChosen;

        public event EventHandler<int>? RecallRequested;

        public void Recall(int direction) => RecallRequested?.Invoke(this, direction);

        public int Caret { get; set; }

        public IReadOnlyList<string>? Suggestions { get; private set; }

        public void Type(string expression) => SetExpression(expression, expression.Length);

        public void Choose(int index) => SuggestionChosen?.Invoke(this, index);

        public void SetExpression(string expression, int caret)
        {
            (Expression, Caret) = (expression, caret);
            ExpressionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ShowSuggestions(IReadOnlyList<string> items) => Suggestions = items;

        public void HideSuggestions() => Suggestions = null;

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
