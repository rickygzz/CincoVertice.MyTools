using CincoVertice.MyTools.Ui.RegexTester.Interfaces;
using CincoVertice.MyTools.Ui.RegexTester.Models;
using CincoVertice.MyTools.Ui.RegexTester.Services;
using CincoVertice.MyTools.Ui.RegexTester.Views;

namespace CincoVertice.MyTools.Ui.RegexTester.Presenters;

/// <summary>
///     Runs the pattern on the text at each pause in typing (<see cref="RegexRunner"/>) and shows what it gives: the
///     matches colored in the text, every other one in another color, and each group in its own over them; a row per
///     match and per group; and the text replaced. What was written is kept for the next window.
/// </summary>
public sealed class RegexTesterPresenter
{
    // A value longer than this is cut in the list, where it's one line
    public const int MaxShownLength = 200;

    private readonly IRegexTesterView _view;
    private readonly IRegexTesterSettingsStore _settings;

    public RegexTesterPresenter(IRegexTesterView view, IRegexTesterSettingsStore settings)
    {
        _view = view;
        _settings = settings;

        _view.InputChanged += (_, _) => Update(save: true);
        _view.RowChosen += (_, row) => OnRowChosen(row);
    }

    public void Start()
    {
        _view.ShowInput(_settings.Load() ?? new RegexInputModel(string.Empty, string.Empty));
        Update(save: false);
    }

    /// <summary>Each match, then its groups, as the list shows them.</summary>
    public static IReadOnlyList<RegexRowModel> RowsOf(RegexResultModel result) =>
    [
        .. result.Matches.SelectMany(match => (IEnumerable<RegexRowModel>)
        [
            new RegexRowModel(
                $"Match {match.Number}",
                Shown(match.Value),
                Place(match.Index, match.Length),
                match.Index,
                match.Length,
                IsGroup: false),
            .. match.Groups.Select(group => group.Success
                ? new RegexRowModel(
                    NameOf(group),
                    Shown(group.Value),
                    Place(group.Index, group.Length),
                    group.Index,
                    group.Length,
                    IsGroup: true)
                : new RegexRowModel(NameOf(group), "(took no part)", string.Empty, -1, 0, IsGroup: true)),
        ]),
    ];

    /// <summary>The matches, every other one as the other kind, each followed by its groups, which go over it.</summary>
    public static IReadOnlyList<RegexHighlightModel> HighlightsOf(RegexResultModel result) =>
    [
        .. result.Matches.SelectMany(match => (IEnumerable<RegexHighlightModel>)
        [
            new RegexHighlightModel(
                match.Index,
                match.Length,
                match.Number % 2 == 1 ? RegexHighlightKind.Match : RegexHighlightKind.OtherMatch),
            .. match.Groups
                .Where(group => group.Success && group.Length > 0)
                .Select(group => new RegexHighlightModel(
                    group.Index,
                    group.Length,
                    RegexHighlightKind.Group,
                    group.Number)),
        ]),
    ];

    public static string StatusOf(RegexInputModel input, RegexResultModel result)
    {
        if (result.Error is string error)
        {
            return error;
        }

        if (input.Pattern.Length == 0)
        {
            return "Write a pattern to see what it matches.";
        }

        int count = result.Matches.Count;
        string matches = count switch
        {
            0 => "No match",
            1 => "1 match",
            _ => $"{count} matches",
        };

        return result.HasMore ? $"{matches} shown; there are more" : matches;
    }

    private static string NameOf(RegexGroupModel group) =>
        group.Name is string name ? $"  {name}" : $"  Group {group.Number}";

    // Where a part is, as .NET counts: from 0
    private static string Place(int index, int length) => $"at {index}, {length} long";

    // On one line: new lines and tabs as signs, and cut when long
    private static string Shown(string value)
    {
        if (value.Length == 0)
        {
            return "(empty)";
        }

        string oneLine = value.Replace("\r\n", "↵").Replace('\n', '↵').Replace('\r', '↵').Replace('\t', '→');

        return oneLine.Length > MaxShownLength ? oneLine[..MaxShownLength] + "…" : oneLine;
    }

    private void Update(bool save)
    {
        RegexInputModel input = _view.Input;
        RegexResultModel result = RegexRunner.Run(input);

        _view.ShowHighlights(HighlightsOf(result));
        _view.ShowRows(RowsOf(result));
        _view.ShowStatus(StatusOf(input, result), isProblem: result.Error is not null);
        _view.ShowReplaced(result.Replaced);

        if (save)
        {
            _settings.Save(input);
        }
    }

    private void OnRowChosen(RegexRowModel row)
    {
        if (row.Start >= 0)
        {
            _view.SelectText(row.Start, row.Length);
        }
    }
}
