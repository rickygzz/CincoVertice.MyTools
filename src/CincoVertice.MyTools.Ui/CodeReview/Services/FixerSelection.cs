using CincoVertice.Common.Application.CodeStandard.Models;
using CincoVertice.MyTools.Ui.CodeReview.Interfaces;
using CincoVertice.MyTools.Ui.CodeReview.Models;

namespace CincoVertice.MyTools.Ui.CodeReview.Services;

/// <summary>
///     Which fixers Fix folder runs: every fixer of every language, each on unless the user turned it off. A choice
///     is saved as soon as it's made, so it holds for the next fix, and the next time the app runs.
/// </summary>
public sealed class FixerSelection
{
    private readonly IFixerSelectionStore _store;

    // Every fixer's name, with the languages it fixes
    private readonly IReadOnlyList<(string Name, IReadOnlyList<string> Languages)> _fixers;
    private readonly HashSet<string> _off;

    public FixerSelection(IFixerSelectionStore store, IEnumerable<CodeLanguageModel> languages)
    {
        _store = store;
        _fixers =
        [
            .. languages
                .SelectMany(language => language.Fixers.Names.Select(name => (Name: name, Language: language.Name)))
                .GroupBy(fixer => fixer.Name)
                .Select(group => (group.Key, (IReadOnlyList<string>)[.. group.Select(fixer => fixer.Language)])),
        ];
        _off = [.. store.LoadOff()];
    }

    /// <summary>Every fixer, by its name in words, and whether it's on.</summary>
    public IReadOnlyList<FixerChoiceModel> Choices =>
    [
        .. _fixers
            .Select(fixer => new FixerChoiceModel(
                fixer.Name,
                FixerCatalog.TitleOf(fixer.Name),
                FixerCatalog.DescriptionOf(fixer.Name),
                string.Join(" and ", fixer.Languages),
                !_off.Contains(fixer.Name)))
            .OrderBy(choice => choice.Title, StringComparer.CurrentCultureIgnoreCase),
    ];

    /// <summary>The names of the fixers on, for the fixer service.</summary>
    public IReadOnlyCollection<string> On =>
        [.. _fixers.Select(fixer => fixer.Name).Where(name => !_off.Contains(name))];

    /// <summary>The fixers off, by their names in words, for the warning before a fix.</summary>
    public IReadOnlyList<string> OffTitles => [.. Choices.Where(choice => !choice.IsOn).Select(choice => choice.Title)];

    public void Set(string name, bool isOn)
    {
        bool isChanged = isOn ? _off.Remove(name) : _off.Add(name);

        if (isChanged)
        {
            _store.SaveOff(_off);
        }
    }

    public void SetAll(bool isOn)
    {
        _off.Clear();

        if (!isOn)
        {
            _off.UnionWith(_fixers.Select(fixer => fixer.Name));
        }

        _store.SaveOff(_off);
    }
}
