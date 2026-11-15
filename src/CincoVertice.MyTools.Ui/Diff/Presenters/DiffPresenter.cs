using System.ComponentModel;
using CincoVertice.MyTools.Ui.Diff.Interfaces;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Services;
using CincoVertice.MyTools.Ui.Diff.Views;

namespace CincoVertice.MyTools.Ui.Diff.Presenters;

/// <summary>
///     Compare files: two files, or texts pasted in their place, compared line by line (<see cref="TextDiffer"/>), and
///     compared again whenever one side or an option changes, including at each edit the user makes in the view. Next
///     and Previous go from change to change. Saving writes a side as it was read: same encoding and line breaks.
///     <para>
///         When the window becomes active again, files written meanwhile are read again, so the comparison stays
///         current while the user edits them elsewhere; one changed here too is read again only if the user agrees.
///         Closing asks to save files changed here; pasted or typed text is never asked for.
///     </para>
/// </summary>
public sealed class DiffPresenter
{
    public const string Title = "Compare files";
    public const string PastedText = "Pasted text";
    public const string NoSource = "Choose a file, drop one here, paste text or type";

    private readonly IDiffView _view;
    private readonly IDiffFileReader _files;
    private readonly IDiffFileWriter _writer;

    private DiffSourceModel? _left;
    private DiffSourceModel? _right;
    private DiffOptionsModel _options = new();
    private DiffResultModel? _result;
    private int _change = -1;

    // While asking whether to read a file again: the window becomes active again when the question closes
    private bool _isCheckingFiles;

    public DiffPresenter(IDiffView view, IDiffFileReader files, IDiffFileWriter writer)
    {
        _view = view;
        _files = files;
        _writer = writer;

        _view.FilesChosen += OnFilesChosen;
        _view.TextPasted += OnTextPasted;
        _view.TextEdited += OnTextEdited;
        _view.SaveRequested += OnSaveRequested;
        _view.SwapRequested += OnSwapRequested;
        _view.ReloadRequested += OnReloadRequested;
        _view.WindowActivated += OnWindowActivated;
        _view.CloseRequested += OnCloseRequested;
        _view.OptionsChanged += OnOptionsChanged;
        _view.NextChangeRequested += OnNextChangeRequested;
        _view.PreviousChangeRequested += OnPreviousChangeRequested;
    }

    public DiffResultModel? Result => _result;

    /// <summary>Shows the files given, or the window empty, ready for them.</summary>
    public void Open(string? leftPath = null, string? rightPath = null)
    {
        if (leftPath is not null && Read(leftPath) is { } left)
        {
            SetSource(DiffSide.Left, left);
        }

        if (rightPath is not null && Read(rightPath) is { } right)
        {
            SetSource(DiffSide.Right, right);
        }

        Compare(keepPlace: false);
    }

    /// <summary>A summary of what differs, for the status bar.</summary>
    public static string Summary(
        DiffResultModel result,
        DiffSourceModel left,
        DiffSourceModel right,
        DiffOptionsModel options)
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

        if (left.Lines.SequenceEqual(right.Lines, StringComparer.Ordinal))
        {
            bool sameBreaks = left.Format.LineBreak == right.Format.LineBreak
                && left.Format.EndsWithLineBreak == right.Format.EndsWithLineBreak;

            return sameBreaks || !left.IsFile || !right.IsFile
                ? "They're the same."
                : "The lines are the same: only the line endings differ.";
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
        { Path: { } path } => Path.GetFileName(path) + (source.NeedsSaving ? "*" : string.Empty),
        _ => PastedText,
    };

    private static string DescriptionOf(DiffSourceModel? source) => source switch
    {
        null => NoSource,
        { Path: { } path } => path + (source.NeedsSaving ? " *" : string.Empty),
        _ => PastedText,
    };

    // Rows of one side alone, while the other has nothing yet
    private static DiffResultModel OneSided(DiffSourceModel source, DiffSide side)
    {
        DiffRowModel[] rows =
        [
            .. source.Lines.Select((text, i) =>
            {
                var line = new DiffLineModel(i + 1, text, []);

                return side == DiffSide.Left
                    ? new DiffRowModel(DiffRowKind.Same, line, null)
                    : new DiffRowModel(DiffRowKind.Same, null, line);
            }),
        ];

        return new DiffResultModel(rows, [], 0, 0, 0);
    }

    private static DiffSide[] Sides => [DiffSide.Left, DiffSide.Right];

    private DiffSourceModel? SourceOf(DiffSide side) => side == DiffSide.Left ? _left : _right;

    // A new text for a side, which the view shows to edit
    private void SetSource(DiffSide side, DiffSourceModel? source)
    {
        Keep(side, source);
        _view.ShowText(side, source?.Lines ?? []);
    }

    // The side's text, already in the view
    private void Keep(DiffSide side, DiffSourceModel? source)
    {
        if (side == DiffSide.Left)
        {
            _left = source;
        }
        else
        {
            _right = source;
        }
    }

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

        var source = new DiffSourceModel(
            TextDiffer.SplitLines(paste.Text),
            null,
            null,
            DiffTextFormatModel.Of(paste.Text, DiffTextFormatModel.Default.Encoding));
        SetSource(paste.Side, source);
        Compare(keepPlace: false);
    }

    private void OnTextEdited(object? sender, DiffEditModel edit)
    {
        DiffSourceModel source = SourceOf(edit.Side) is { } old
            ? old with { Lines = edit.Lines, IsModified = true }
            : new DiffSourceModel(edit.Lines, null, null, DiffTextFormatModel.Default, IsModified: true);
        Keep(edit.Side, source);
        Compare(keepPlace: true);
    }

    private void OnSaveRequested(object? sender, DiffSide side)
    {
        Save(side);
    }

    private void OnSwapRequested(object? sender, EventArgs e)
    {
        (DiffSourceModel? left, DiffSourceModel? right) = (_right, _left);
        SetSource(DiffSide.Left, left);
        SetSource(DiffSide.Right, right);
        Compare(keepPlace: false);
    }

    private void OnReloadRequested(object? sender, EventArgs e)
    {
        foreach (DiffSide side in Sides)
        {
            if (SourceOf(side) is { Path: { } path } source
                && (!source.NeedsSaving || _view.AskReload(Path.GetFileName(path)))
                && Read(path) is { } read)
            {
                SetSource(side, read);
            }
        }

        Compare(keepPlace: true);
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        if (_isCheckingFiles)
        {
            return;
        }

        _isCheckingFiles = true;

        try
        {
            CheckFiles();
        }
        finally
        {
            _isCheckingFiles = false;
        }
    }

    private void CheckFiles()
    {
        bool changed = false;

        foreach (DiffSide side in Sides)
        {
            if (SourceOf(side) is not { Path: { } path } source
                || _files.WrittenUtc(path) is not { } written
                || written == source.WrittenUtc)
            {
                continue;
            }

            if (source.NeedsSaving && !_view.AskReload(Path.GetFileName(path)))
            {
                // Kept as edited here; not asked again until the file changes again
                Keep(side, source with { WrittenUtc = written });
                continue;
            }

            if (Read(path, quietly: true) is { } read)
            {
                SetSource(side, read);
                changed = true;
            }
        }

        if (changed)
        {
            Compare(keepPlace: true);
        }
    }

    private void OnCloseRequested(object? sender, CancelEventArgs e)
    {
        DiffSide[] unsaved = [.. Sides.Where(side => SourceOf(side)?.NeedsSaving == true)];

        if (unsaved.Length == 0)
        {
            return;
        }

        DiffSaveChoice choice =
            _view.AskSaveChanges([.. unsaved.Select(side => Path.GetFileName(SourceOf(side)!.Path!))]);

        if (choice == DiffSaveChoice.Cancel)
        {
            e.Cancel = true;
        }
        else if (choice == DiffSaveChoice.Save)
        {
            e.Cancel = !unsaved.All(Save);
        }
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

    /// <returns>Whether it was saved, or there was nothing to save.</returns>
    private bool Save(DiffSide side)
    {
        if (SourceOf(side) is not { } source)
        {
            return true;
        }

        string? path = source.Path ?? _view.AskSavePath(side);

        if (path is null)
        {
            return false;
        }

        try
        {
            DateTime written = _writer.Write(path, source.Format.Join(source.Lines), source.Format.Encoding);
            Keep(side, source with { Path = path, WrittenUtc = written, IsModified = false });
            Compare(keepPlace: true);

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowProblem($"Could not save {path}: {ex.Message}");

            return false;
        }
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

            return new DiffSourceModel(
                TextDiffer.SplitLines(file.Text),
                path,
                file.WrittenUtc,
                DiffTextFormatModel.Of(file.Text, file.Encoding));
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
        _change = -1;
        _view.ShowChange(null);

        if (_left is null || _right is null)
        {
            DiffSourceModel? only = _left ?? _right;
            _result = null;
            DiffResultModel rows = only is null
                ? new DiffResultModel([], [], 0, 0, 0)
                : OneSided(only, _left is null ? DiffSide.Right : DiffSide.Left);
            _view.ShowDiff(rows, keepPlace);
            _view.ShowCanNavigate(false);
            _view.ShowStatus(
                only is null
                    ? "Choose two files, drop them here, or paste or type text on each side."
                    : $"Now choose the {(_left is null ? "left" : "right")} side, or paste or type it.",
                string.Empty);

            return;
        }

        _result = TextDiffer.Compare(_left.Lines, _right.Lines, _options);
        _view.ShowDiff(_result, keepPlace);
        _view.ShowCanNavigate(_result.HasChanges);

        // The changes may be others now: the first one, unless staying in place
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
        _view.ShowStatus(Summary(_result, _left, _right, _options), position);
    }
}
