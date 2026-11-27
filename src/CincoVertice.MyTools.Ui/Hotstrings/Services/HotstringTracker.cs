using System.Text;
using CincoVertice.MyTools.Ui.Hotstrings.Models;

namespace CincoVertice.MyTools.Ui.Hotstrings.Services;

/// <summary>
///     Follows what's typed since the last word break, to tell when a hotstring was typed whole: its letters and
///     digits, then an ending key (Space, Enter or Tab). Anything else, as punctuation, an arrow, a shortcut or a click,
///     starts over, so a hotstring is found only as a word of its own: "btw" is, "abtw" isn't.
/// </summary>
public sealed class HotstringTracker
{
    /// <summary>The longest hotstring: what's typed past that is a word no hotstring can be.</summary>
    public const int MaxLength = 32;

    private readonly StringBuilder _typed = new();

    private Dictionary<string, HotstringModel> _hotstrings = new(StringComparer.OrdinalIgnoreCase);

    // Typed past MaxLength since the last word break
    private bool _isTooLong;

    /// <summary>Whether there's any hotstring to look for.</summary>
    public bool IsEmpty => _hotstrings.Count == 0;

    /// <summary>Looks for these from now on, the first of each name; what was typed is forgotten.</summary>
    public void Use(IEnumerable<HotstringModel> hotstrings)
    {
        _hotstrings = hotstrings
            .DistinctBy(hotstring => hotstring.Abbreviation, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(hotstring => hotstring.Abbreviation, StringComparer.OrdinalIgnoreCase);
        Reset();
    }

    /// <summary>A letter or a digit typed.</summary>
    public void Type(char character)
    {
        if (_typed.Length == MaxLength)
        {
            _isTooLong = true;

            return;
        }

        _typed.Append(character);
    }

    /// <summary>Backspace: the last letter typed is gone. Past the longest hotstring, it can't be told which.</summary>
    public void Erase()
    {
        if (!_isTooLong && _typed.Length > 0)
        {
            _typed.Length--;
        }
    }

    /// <summary>Anything that isn't a letter, a digit, Backspace or an ending key: a new word starts.</summary>
    public void Reset()
    {
        _typed.Clear();
        _isTooLong = false;
    }

    /// <summary>An ending key: the hotstring typed just before it, if any; a new word starts either way.</summary>
    public HotstringModel? End()
    {
        HotstringModel? typed = !_isTooLong && _hotstrings.TryGetValue(_typed.ToString(), out HotstringModel? found)
            ? found
            : null;

        Reset();

        return typed;
    }
}
