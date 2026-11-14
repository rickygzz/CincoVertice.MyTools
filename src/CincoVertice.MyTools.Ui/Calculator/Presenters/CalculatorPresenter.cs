using CincoVertice.Common.Application.Calculator;
using CincoVertice.MyTools.Ui.Calculator.Models;
using CincoVertice.MyTools.Ui.Calculator.Services;
using CincoVertice.MyTools.Ui.Calculator.Views;

namespace CincoVertice.MyTools.Ui.Calculator.Presenters;

/// <summary>
///     Shows the calculator view, evaluates the expression typed in it, and shows the result or the error. Keeps the
///     history of the operations that worked, until the app exits, and shows it when asked. While the expression is
///     typed, offers places and units to write next (<see cref="CalculatorSuggester"/>).
/// </summary>
public class CalculatorPresenter
{
    /// <summary>The oldest operations are dropped after this many.</summary>
    public const int MaxHistory = 200;

    /// <summary>What the history shows before the first operation.</summary>
    public const string NoHistoryText = "No operations yet";

    private readonly ICalculatorView _view;
    private readonly ExpressionCalculator _calculator;
    private readonly List<CalculatorHistoryEntryModel> _history = [];
    private readonly Func<DateTime> _utcNow;

    // What the list shows, to know what a choice in it writes
    private CalculatorSuggestionsModel? _suggestions;

    // What the normal height output shows, for when the history is hidden again
    private Action _showCurrent;
    private bool _historyShown;

    /// <param name="view">The window.</param>
    /// <param name="calculator">Evaluates the expressions.</param>
    /// <param name="utcNow">The time now, for the UTC offsets the suggestions show; the clock's if null.</param>
    public CalculatorPresenter(ICalculatorView view, ExpressionCalculator calculator, Func<DateTime>? utcNow = null)
    {
        _view = view;
        _calculator = calculator;
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
        _showCurrent = () => _view.ShowResult(string.Empty);

        _view.EvaluateRequested += OnEvaluateRequested;
        _view.ToggleHistoryRequested += OnToggleHistoryRequested;
        _view.ExpressionChanged += OnExpressionChanged;
        _view.SuggestionChosen += OnSuggestionChosen;
    }

    public void Show()
    {
        HideSuggestions();
        _view.ShowAndActivate();
    }

    /// <summary>
    ///     Evaluates the view's expression. An empty expression is ignored.
    /// </summary>
    public void Evaluate()
    {
        HideSuggestions();

        string expression = _view.Expression;

        if (string.IsNullOrWhiteSpace(expression))
        {
            return;
        }

        try
        {
            string result = _calculator.EvaluateToText(expression, groupThousands: true);

            AddToHistory(new CalculatorHistoryEntryModel(expression.Trim(), result));
            _showCurrent = () => _view.ShowResult(result);

            if (_historyShown)
            {
                ShowHistory();
            }
            else
            {
                _showCurrent();
            }
        }
        catch (ExpressionException ex)
        {
            // Select the character at fault, so it can be fixed right away; past the end, put the caret there
            int start = Math.Min(ex.Position, expression.Length);
            int length = ex.Position < expression.Length ? 1 : 0;

            _showCurrent = () => _view.ShowError(ex.Message, start, length);

            // Drawn again so errors don't pile up below the history
            if (_historyShown)
            {
                ShowHistory();
            }

            _showCurrent();
        }
    }

    /// <summary>
    ///     Shows the history if it is hidden. Otherwise hides it and shows the current result or error again.
    /// </summary>
    public void ToggleHistory()
    {
        _historyShown = !_historyShown;

        if (_historyShown)
        {
            ShowHistory();
        }
        else
        {
            _view.HideHistory();
            _showCurrent();
        }
    }

    private void AddToHistory(CalculatorHistoryEntryModel entry)
    {
        if (_history.Count == MaxHistory)
        {
            _history.RemoveAt(0);
        }

        _history.Add(entry);
    }

    private void ShowHistory()
    {
        _view.ShowHistory(_history, NoHistoryText);
    }

    private void OnEvaluateRequested(object? sender, EventArgs e)
    {
        Evaluate();
    }

    private void OnToggleHistoryRequested(object? sender, EventArgs e)
    {
        ToggleHistory();
    }

    private void HideSuggestions()
    {
        _suggestions = null;
        _view.HideSuggestions();
    }

    private void OnExpressionChanged(object? sender, EventArgs e)
    {
        _suggestions = CalculatorSuggester.Suggest(_view.Expression, _view.Caret, _utcNow());

        if (_suggestions is null)
        {
            _view.HideSuggestions();
        }
        else
        {
            _view.ShowSuggestions([.. _suggestions.Items.Select(item => item.Text)]);
        }
    }

    /// <summary>Writes the choice over what was typed of it; what follows it is offered next.</summary>
    private void OnSuggestionChosen(object? sender, int index)
    {
        if (_suggestions is not { } suggestions || index < 0 || index >= suggestions.Items.Count)
        {
            return;
        }

        string expression = _view.Expression;
        string insert = suggestions.Items[index].Insert;
        int end = Math.Min(suggestions.Start + suggestions.Length, expression.Length);

        _view.SetExpression(
            expression[..suggestions.Start] + insert + expression[end..],
            suggestions.Start + insert.Length);
    }
}
