using CincoVertice.Common.Application.Calculator;
using CincoVertice.Common.Application.Calculator.Models;
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

    /// <summary>How many lines typed Up and Down go back through; the oldest are dropped after this many.</summary>
    public const int MaxRecalled = 200;

    /// <summary>What the history shows before the first operation.</summary>
    public const string NoHistoryText = "No operations yet";

    private readonly ICalculatorView _view;
    private readonly ExpressionCalculator _calculator;
    private readonly List<CalculatorHistoryEntryModel> _history = [];
    private readonly Func<DateTime> _utcNow;

    // What the list shows, to know what a choice in it writes
    private CalculatorSuggestionsModel? _suggestions;

    // The names given values, and the last answer, until the app exits: x = 5, ans * 2
    private readonly Dictionary<string, CalculatorValue> _names = new(StringComparer.OrdinalIgnoreCase);

    // The lines entered, the newest last, those that failed too, to fix them; which one shows, -1 for none, and
    // what was being typed before Up, which Down past the newest brings back
    private readonly List<string> _entered = [];
    private int _recalled = -1;
    private string _draft = string.Empty;
    private bool _isRecalling;

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
        _view.RecallRequested += OnRecallRequested;
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

        Remember(expression.Trim());

        try
        {
            CalculationResult calculated = _calculator.Calculate(expression, _names, groupThousands: true);
            string result = calculated.Text;

            // A time in another place has no value, so the last answer stays
            if (calculated.Value is CalculatorValue value)
            {
                _names[ExpressionCalculator.LastAnswerName] = value;

                if (calculated.AssignedName is string name)
                {
                    _names[name] = value;
                }
            }

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

    /// <summary>A line entered, unless it's the one entered last, so Up doesn't show it twice.</summary>
    private void Remember(string expression)
    {
        if (_entered.Count == 0 || _entered[^1] != expression)
        {
            _entered.Add(expression);

            if (_entered.Count > MaxRecalled)
            {
                _entered.RemoveAt(0);
            }
        }

        _recalled = -1;
    }

    /// <summary>
    ///     Up brings back the line entered before the one shown, the newest first; Down the one after it, and past the
    ///     newest, what was being typed.
    /// </summary>
    private void OnRecallRequested(object? sender, int direction)
    {
        if (_entered.Count == 0)
        {
            return;
        }

        if (direction < 0)
        {
            if (_recalled == 0)
            {
                return;
            }

            if (_recalled < 0)
            {
                _draft = _view.Expression;
            }

            _recalled = _recalled < 0 ? _entered.Count - 1 : _recalled - 1;
            Show(_entered[_recalled]);
        }
        else if (_recalled >= 0)
        {
            _recalled++;

            if (_recalled < _entered.Count)
            {
                Show(_entered[_recalled]);
            }
            else
            {
                _recalled = -1;
                Show(_draft);
            }
        }
    }

    private void Show(string expression)
    {
        int recalled = _recalled;

        _isRecalling = true;
        _view.SetExpression(expression, expression.Length);
        _isRecalling = false;
        _recalled = recalled;
    }

    private void HideSuggestions()
    {
        _suggestions = null;
        _view.HideSuggestions();
    }

    private void OnExpressionChanged(object? sender, EventArgs e)
    {
        // A line brought back is whole, so nothing is offered for it; a line typed starts from where it is
        if (_isRecalling)
        {
            HideSuggestions();

            return;
        }

        _recalled = -1;

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
