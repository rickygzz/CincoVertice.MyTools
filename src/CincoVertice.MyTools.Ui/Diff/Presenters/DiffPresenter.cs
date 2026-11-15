using System.ComponentModel;
using CincoVertice.MyTools.Ui.Diff.Interfaces;
using CincoVertice.MyTools.Ui.Diff.Models;
using CincoVertice.MyTools.Ui.Diff.Services;
using CincoVertice.MyTools.Ui.Diff.Views;
using System.Text;

namespace CincoVertice.MyTools.Ui.Diff.Presenters;

/// <summary>
///     Compare files: two files, or texts pasted in their place, compared line by line (<see cref="TextDiffer"/>), and
///     compared in the background whenever one side or an option changes. Obsolete work is cancelled. Next
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
    private readonly IDiffComparer _comparer;
    private CancellationTokenSource? _comparisonCancellation;
    private int _generation;
    private bool _closed;

    private DiffSourceModel? _left;
    private DiffSourceModel? _right;
    private DiffOptionsModel _options = new();
    private DiffResultModel? _result;
    private int _change = -1;

    // While asking whether to read a file again: the window becomes active again when the question closes
    private bool _isCheckingFiles;

    public DiffPresenter(IDiffView view, IDiffFileReader files, IDiffFileWriter writer, IDiffComparer? comparer = null)
    {
        _view = view;
        _files = files;
        _writer = writer;
        _comparer = comparer ?? new BackgroundDiffComparer();

        _view.FilesChosen += OnFilesChosen;
        _view.TextPasted += OnTextPasted;
        _view.TextEdited += OnTextEdited;
        _view.SaveRequested += OnSaveRequested;
        _view.SwapRequested += OnSwapRequested;
        _view.ReloadRequested += OnReloadRequested;
        _view.WindowActivated += OnWindowActivated;
        _view.CloseRequested += OnCloseRequested;
        _view.Closed += OnClosed;
        _view.OptionsChanged += OnOptionsChanged;
        _view.NextChangeRequested += OnNextChangeRequested;
        _view.PreviousChangeRequested += OnPreviousChangeRequested;
    }

    public DiffResultModel? Result => _result;

    public Task Comparison { get; private set; } = Task.CompletedTask;

    /// <summary>Shows the files given, or the window empty, ready for them.</summary>
    public void Open(string? leftPath = null, string? rightPath = null)
    {
        DiffSide[] replaced = [.. Sides.Where(side => side == DiffSide.Left ? leftPath is not null : rightPath is not null)];

        if (!ConfirmReplace(replaced))
        {
            return;
        }

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

        if (!ConfirmReplace([paste.Side]))
        {
            return;
        }

        var source = new DiffSourceModel(
            TextDiffer.SplitLines(paste.Text, preserveFinalLine: true),
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
        source = source with
        {
            Format = source.Format with { EndsWithLineBreak = edit.Lines.Count > 1 && edit.Lines[^1].Length == 0 },
        };
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
                || written == source.WrittenUtc || written == source.DeclinedReloadUtc)
            {
                continue;
            }

            if (source.NeedsSaving && !_view.AskReload(Path.GetFileName(path)))
            {
                // Kept as edited here; not asked again until the file changes again
                Keep(side, source with { DeclinedReloadUtc = written });
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
        e.Cancel = !ConfirmReplace(Sides);
    }

    private bool ConfirmReplace(IEnumerable<DiffSide> sides)
    {
        DiffSide[] unsaved = [.. sides.Where(side => SourceOf(side)?.NeedsSaving == true)];

        if (unsaved.Length == 0)
        {
            return true;
        }

        return AskWithoutReload(() => _view.AskSaveChanges(
            [.. unsaved.Select(side => Path.GetFileName(SourceOf(side)!.Path!))])) switch
        {
            DiffSaveChoice.Cancel => false,
            DiffSaveChoice.Save => unsaved.All(Save),
            _ => true,
        };
    }

    private T AskWithoutReload<T>(Func<T> ask)
    {
        bool wasChecking = _isCheckingFiles;
        _isCheckingFiles = true;
        try
        {
            return ask();
        }
        finally
        {
            _isCheckingFiles = wasChecking;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _closed = true;
        _generation++;
        _comparisonCancellation?.Cancel();
        _comparisonCancellation?.Dispose();
        _comparisonCancellation = null;
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
            if (source.IsFile && _files.WrittenUtc(path) != source.WrittenUtc
                && !AskWithoutReload(() => _view.AskOverwrite(Path.GetFileName(path))))
            {
                return false;
            }

            DateTime written = _writer.Write(path, source.Format.Join(source.Lines), source.Format.Encoding);
            Keep(side, source with { Path = path, WrittenUtc = written, IsModified = false, DeclinedReloadUtc = null });
            Compare(keepPlace: true);

            return true;
        }
        catch (EncoderFallbackException)
        {
            _view.ShowProblem($"The text contains characters that cannot be saved using {source.Format.Encoding.EncodingName}. "
                + "The file was not changed. Copy the text to an editor that supports saving as UTF-8.");
            return false;
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
                TextDiffer.SplitLines(file.Text, preserveFinalLine: true),
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
        if (_closed)
        {
            return;
        }

        int generation = ++_generation;
        _comparisonCancellation?.Cancel();
        _comparisonCancellation?.Dispose();
        _comparisonCancellation = null;
        Comparison = Task.CompletedTask;
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

        // Fresh, unaligned rows remain editable while the worker computes the comparison.
        var left = _left with { Lines = _left.Lines.ToArray() };
        var right = _right with { Lines = _right.Lines.ToArray() };
        _result = null;
        var rowsPending = new List<DiffRowModel>(Math.Max(left.Lines.Count, right.Lines.Count));
        for (int i = 0; i < Math.Max(left.Lines.Count, right.Lines.Count); i++)
        {
            rowsPending.Add(new DiffRowModel(DiffRowKind.Same,
                i < left.Lines.Count ? new DiffLineModel(i + 1, left.Lines[i], []) : null,
                i < right.Lines.Count ? new DiffLineModel(i + 1, right.Lines[i], []) : null));
        }

        _view.ShowDiff(new DiffResultModel(rowsPending, [], 0, 0, 0), keepPlace);
        _view.ShowCanNavigate(false);
        _view.ShowStatus("Comparing...", string.Empty);
        _comparisonCancellation = new CancellationTokenSource();
        Comparison = CompleteComparisonAsync(left, right, _options, keepPlace, generation, _comparisonCancellation.Token);
    }

    private async Task CompleteComparisonAsync(DiffSourceModel left, DiffSourceModel right, DiffOptionsModel options,
        bool keepPlace, int generation, CancellationToken cancellation)
    {
        DiffResultModel result;
        try
        {
            result = await _comparer.CompareAsync(left.Lines, right.Lines, options, cancellation);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            if (!_closed && generation == _generation)
            {
                _view.ShowStatus("Comparison failed.", string.Empty);
                _view.ShowProblem($"Could not compare the texts: {ex.Message}");
            }
            return;
        }

        if (_closed || cancellation.IsCancellationRequested || generation != _generation)
        {
            return;
        }

        _result = result;
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
