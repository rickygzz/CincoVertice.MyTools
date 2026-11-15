using CincoVertice.MyTools.Ui.Diff.Interfaces;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Services;
using CincoVertice.MyTools.Ui.Diff.Views;

namespace CincoVertice.MyTools.Ui.Diff.Presenters;

/// <summary>
///     Compare files: two files, or texts pasted in their place, compared line by line (<see cref="TextDiffer"/>), and
///     compared again whenever one side or an option changes. Next and Previous go from change to change. When the
///     window becomes active again, files written meanwhile are read again, so the comparison stays current while the
///     user edits them elsewhere.
/// </summary>
public sealed class DiffPresenter
{
    public const string Title = "Compare files";
    public const string PastedText = "Pasted text";
    public const string NoSource = "Choose a file, drop one here, or paste text";

    private readonly IDiffView _view;
    private readonly IDiffFileReader _files;

    private DiffSourceModel? _left;
    private DiffSourceModel? _right;
    private DiffOptionsModel _options = new();
    private DiffResultModel? _result;
    private int _change = -1;

    public DiffPresenter(IDiffView view, IDiffFileReader files)
    {
        _view = view;
        _files = files;

        _view.FilesChosen += OnFilesChosen;
        _view.TextPasted += OnTextPasted;
        _view.SwapRequested += OnSwapRequested;
        _view.ReloadRequested += OnReloadRequested;
        _view.WindowActivated += OnWindowActivated;
        _view.OptionsChanged += OnOptionsChanged;
        _view.NextChangeRequested += OnNextChangeRequested;
        _view.PreviousChangeRequested += OnPreviousChangeRequested;
    }

    public DiffResultModel? Result => _result;

    /// <summary>Shows the files given, or the window empty, ready for them.</summary>
    public void Open(string? leftPath = null, string? rightPath = null)
    {
        if (leftPath is not null)
        {
            _left = Read(leftPath) ?? _left;
        }

        if (rightPath is not null)
        {
            _right = Read(rightPath) ?? _right;
        }

        Compare(keepPlace: false);
    }

    /// <summary>A summary of what differs, for the status bar.</summary>
    public static string Summary(DiffResultModel result, string left, string right, DiffOptionsModel options)
    {
        if (result.HasChanges)
        {
            var parts = new List<string>();
            AddPart(parts, result.Added, "added");
            AddPart(parts, result.Removed, "removed");
            AddPart(parts, result.Changed, "changed");
            string changes = result.Hunks.Count == 1 ? "1 change" : $"{result.Hunks.Count} changes";

            return $"{changes}: {string.Join(", ", parts)}";
        }

        if (left == right)
        {
            return "They're the same.";
        }

        if (TextDiffer.SplitLines(left).SequenceEqual(TextDiffer.SplitLines(right), StringComparer.Ordinal))
        {
            return "The lines are the same: only the line endings differ.";
        }

        string ignored = (options.IgnoreWhitespace, options.IgnoreCase) switch
        {
            (true, true) => "whitespace and case",
            (true, false) => "whitespace",
            _ => "case",
        };

        return $"No differences, ignoring {ignored}.";
    }

    private static void AddPart(List<string> parts, int count, string what)
    {
        if (count == 0)
        {
            return;
        }

        // "3 lines added, 1 removed": the first part says what's counted
        string unit = parts.Count > 0 ? string.Empty : count == 1 ? " line" : " lines";
        parts.Add($"{count}{unit} {what}");
    }

    private static string NameOf(DiffSourceModel? source) => source switch
    {
        null => "?",
        { Path: { } path } => Path.GetFileName(path),
        _ => PastedText,
    };

    private static string DescriptionOf(DiffSourceModel? source) => source switch
    {
        null => NoSource,
        { Path: { } path } => path,
        _ => PastedText,
    };

    private void OnFilesChosen(object? sender, DiffFilesModel files)
    {
        if (files.Paths.Count == 0)
        {
            return;
        }

        if (files.Paths.Count >= 2)
        {
            Open(files.Paths[0], files.Paths[1]);
        }
        else if (files.Side == DiffSide.Left)
        {
            Open(files.Paths[0], null);
        }
        else
        {
            Open(null, files.Paths[0]);
        }
    }

    private void OnTextPasted(object? sender, DiffPasteModel paste)
    {
        if (paste.Text.Length == 0)
        {
            _view.ShowProblem("The clipboard has no text.");

            return;
        }

        var source = new DiffSourceModel(paste.Text, null, null);

        if (paste.Side == DiffSide.Left)
        {
            _left = source;
        }
        else
        {
            _right = source;
        }

        Compare(keepPlace: false);
    }

    private void OnSwapRequested(object? sender, EventArgs e)
    {
        (_left, _right) = (_right, _left);
        Compare(keepPlace: false);
    }

    private void OnReloadRequested(object? sender, EventArgs e)
    {
        _left = Reread(_left, quietly: false);
        _right = Reread(_right, quietly: false);
        Compare(keepPlace: true);
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        DiffSourceModel? left = ChangedOnDisk(_left);
        DiffSourceModel? right = ChangedOnDisk(_right);

        if (left is null && right is null)
        {
            return;
        }

        _left = left ?? _left;
        _right = right ?? _right;
        Compare(keepPlace: true);
    }

    private void OnOptionsChanged(object? sender, DiffOptionsModel options)
    {
        if (options == _options)
        {
            return;
        }

        _options = options;
        Compare(keepPlace: true);
    }

    private void OnNextChangeRequested(object? sender, int fromRow)
    {
        if (_result is null)
        {
            return;
        }

        int next = _result.Hunks.ToList().FindIndex(hunk => hunk.FirstRow > fromRow);

        if (next >= 0)
        {
            GoTo(next);
        }
    }

    private void OnPreviousChangeRequested(object? sender, int fromRow)
    {
        if (_result is null)
        {
            return;
        }

        int previous = _result.Hunks.ToList().FindLastIndex(hunk => hunk.FirstRow < fromRow);

        if (previous >= 0)
        {
            GoTo(previous);
        }
    }

    private void GoTo(int change)
    {
        _change = change;
        _view.ShowChange(_result!.Hunks[change]);
        ShowStatus();
    }

    // Read again, the file at the same path; pasted text stays. On a problem, it stays as it was
    private DiffSourceModel? Reread(DiffSourceModel? source, bool quietly)
    {
        if (source?.Path is not { } path)
        {
            return source;
        }

        return Read(path, quietly) ?? source;
    }

    private DiffSourceModel? ChangedOnDisk(DiffSourceModel? source)
    {
        if (source?.Path is not { } path || _files.WrittenUtc(path) is not { } written || written == source.WrittenUtc)
        {
            return null;
        }

        return Read(path, quietly: true);
    }

    private DiffSourceModel? Read(string path, bool quietly = false)
    {
        try
        {
            DiffFileModel file = _files.Read(path);

            if (file.Text.Contains('\0'))
            {
                if (!quietly)
                {
                    _view.ShowProblem($"{Path.GetFileName(path)} isn't a text file, so it can't be compared.");
                }

                return null;
            }

            return new DiffSourceModel(file.Text, path, file.WrittenUtc);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!quietly)
            {
                _view.ShowProblem($"Could not read {path}: {ex.Message}");
            }

            return null;
        }
    }

    private void Compare(bool keepPlace)
    {
        _view.ShowSides(DescriptionOf(_left), DescriptionOf(_right));
        _view.ShowTitle(_left is null && _right is null ? Title : $"{NameOf(_left)} vs {NameOf(_right)} - {Title}");

        if (_left is null || _right is null)
        {
            _result = null;
            _change = -1;
            _view.ShowDiff(new DiffResultModel([], [], 0, 0, 0), keepPlace: false);
            _view.ShowChange(null);
            _view.ShowCanNavigate(false);
            _view.ShowStatus(
                _left is null && _right is null
                    ? "Choose two files, drop them here, or paste text on each side."
                    : $"Now choose the {(_left is null ? "left" : "right")} side.",
                string.Empty);

            return;
        }

        _result = TextDiffer.Compare(_left.Text, _right.Text, _options);
        _view.ShowDiff(_result, keepPlace);
        _view.ShowCanNavigate(_result.HasChanges);

        // The changes may be others now: the first one, unless staying in place
        _change = -1;
        _view.ShowChange(null);

        if (!keepPlace && _result.HasChanges)
        {
            GoTo(0);
        }
        else
        {
            ShowStatus();
        }
    }

    private void ShowStatus()
    {
        if (_result is null || _left is null || _right is null)
        {
            return;
        }

        string position = _change >= 0 ? $"Change {_change + 1} of {_result.Hunks.Count}" : string.Empty;
        _view.ShowStatus(Summary(_result, _left.Text, _right.Text, _options), position);
    }
}
