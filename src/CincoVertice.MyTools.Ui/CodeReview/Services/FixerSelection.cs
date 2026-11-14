using CincoVertice.Common.Application.CodeStandard.Models;
using CincoVertice.MyTools.Ui.CodeReview.Interfaces;
using CincoVertice.MyTools.Ui.CodeReview.Models;

namespace CincoVertice.MyTools.Ui.CodeReview.Services;

/// <summary>
///     What Fix folder does: which fixers it runs, every fixer of every language, each on unless the user turned it
///     off; and the line ending and encoding it gives every file, or keeps. A choice is saved as soon as it's made,
///     so it holds for the next fix, and the next time the app runs.
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

        FixerSettingsModel saved = store.Load();
        _off = [.. saved.Off];
        LineEnding = saved.LineEnding;
        Encoding = saved.Encoding;
    }

    /// <summary>The line ending every file gets; <see cref="LineEndingEnum.None"/> keeps them.</summary>
    public LineEndingEnum LineEnding { get; private set; }

    /// <summary>How every file is written.</summary>
    public FileEncodingEnum Encoding { get; private set; }

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

    /// <summary>What a fix does with these choices, for the fixer service.</summary>
    public FixOptionsModel Options => new(On, LineEnding, Encoding);

    public void Set(string name, bool isOn)
    {
        bool isChanged = isOn ? _off.Remove(name) : _off.Add(name);

        if (isChanged)
        {
            Save();
        }
    }

    public void SetAll(bool isOn)
    {
        _off.Clear();

        if (!isOn)
        {
            _off.UnionWith(_fixers.Select(fixer => fixer.Name));
        }

        Save();
    }

    public void SetLineEnding(LineEndingEnum lineEnding)
    {
        LineEnding = lineEnding;
        Save();
    }

    public void SetEncoding(FileEncodingEnum encoding)
    {
        Encoding = encoding;
        Save();
    }

    /// <returns>How a line ending is named in the menu, e.g. "CRLF (Windows)".</returns>
    public static string TitleOf(LineEndingEnum lineEnding) => lineEnding switch
    {
        LineEndingEnum.CRLF => "CRLF (Windows)",
        LineEndingEnum.LF => "LF (Linux, macOS)",
        LineEndingEnum.CR => "CR (classic Mac OS)",
        _ => "Keep as they are",
    };

    /// <returns>How an encoding is named in the menu, e.g. "UTF-8 with BOM".</returns>
    public static string TitleOf(FileEncodingEnum encoding) => encoding switch
    {
        FileEncodingEnum.Utf8 => "UTF-8",
        FileEncodingEnum.Utf8Bom => "UTF-8 with BOM",
        _ => "Keep as it is",
    };

    private void Save() => _store.Save(new FixerSettingsModel([.. _off], LineEnding, Encoding));
}
