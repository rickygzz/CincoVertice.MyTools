using System.ComponentModel;
using CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.ConfigEditor.Views;

namespace CincoVertice.MyTools.Ui.ConfigEditor.Presenters;

/// <summary>
///     Edits one file at a time from the catalog: reads it, shows its colors and problems as it changes, and saves it.
///     Nothing is lost by leaving a file with changes, closing the window, or saving over a file changed elsewhere,
///     without asking.
/// </summary>
public sealed class ConfigEditorPresenter
{
    public const string WindowTitle = "Config editor";

    private readonly IConfigEditorView _view;
    private readonly IEditableFileCatalog _catalog;
    private readonly IEditableFileStore _store;
    private readonly IEditableFileLanguage _language;

    private IReadOnlyList<EditableFileModel> _files = [];
    private EditableFileModel? _file;
    private EditableFileContentModel? _saved;
    private IReadOnlyList<EditableFileProblemModel> _problems = [];

    // What the suggestions showing would replace, and with what
    private CodeSuggestionsModel? _suggestions;

    public ConfigEditorPresenter(
        IConfigEditorView view,
        IEditableFileCatalog catalog,
        IEditableFileStore store,
        IEditableFileLanguage language)
    {
        _view = view;
        _catalog = catalog;
        _store = store;
        _language = language;

        _view.FileSelected += View_FileSelected;
        _view.TextEdited += View_TextEdited;
        _view.SaveRequested += View_SaveRequested;
        _view.NewScriptRequested += View_NewScriptRequested;
        _view.RefreshFilesRequested += View_RefreshFilesRequested;
        _view.ProblemSelected += View_ProblemSelected;
        _view.ViewActivated += View_ViewActivated;
        _view.CloseRequested += View_CloseRequested;
        _view.SuggestionsRequested += View_SuggestionsRequested;
        _view.SuggestionChosen += View_SuggestionChosen;
        _view.HoverRequested += View_HoverRequested;
    }

    /// <summary>The file shown; null before one is.</summary>
    public string? FilePath => _file?.Path;

    private bool IsModified => _saved is not null && _view.CodeText != _saved.Text;

    /// <summary>Shows a file, after dealing with the changes to the one open; or the first when path is null.</summary>
    public void Open(string? path)
    {
        if (_file is not null && path is not null && SamePath(_file.Path, path))
        {
            return;
        }

        if (!ConfirmLeave())
        {
            return;
        }

        RefreshFiles();

        EditableFileModel? file = _files.FirstOrDefault(f => path is not null && SamePath(f.Path, path))
            ?? _file
            ?? _files.FirstOrDefault();

        Load(file);
    }

    private static bool SamePath(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private void RefreshFiles()
    {
        _files = _catalog.GetFiles();
    }

    private void Load(EditableFileModel? file)
    {
        _file = file;
        _saved = null;

        if (file is not null)
        {
            try
            {
                _saved = _store.Read(file.Path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _view.ShowError($"{file.Name} can't be opened: {ex.Message}");
            }
        }

        _view.ShowFiles(_files, file);
        _view.ShowCode(_saved?.Text ?? string.Empty);
        _view.ShowCanSave(_saved is not null);
        Analyze();
    }

    private void Analyze()
    {
        if (_file is null || _saved is null)
        {
            _problems = [];
            _view.ShowHighlights([]);
        }
        else
        {
            string text = _view.CodeText;

            _problems = _language.Check(_file.Kind, text);
            _view.ShowHighlights(_language.Highlight(_file.Kind, text));
        }

        _view.ShowProblems(_problems);
        ShowTitle();
    }

    private void ShowTitle()
    {
        string name = _file is null ? string.Empty : _file.Name + (IsModified ? "*" : string.Empty) + " - ";

        _view.ShowTitle(name + WindowTitle);
    }

    private bool Save()
    {
        if (_file is null || _saved is null)
        {
            return false;
        }

        try
        {
            if (IsChangedElsewhere() && !_view.AskOverwrite(_file.Name))
            {
                return false;
            }

            EditableFileContentModel content = _saved with { Text = _view.CodeText };

            _store.Write(_file.Path, content);
            _saved = content;
            ShowTitle();

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _view.ShowError($"{_file.Name} can't be saved: {ex.Message}");

            return false;
        }
    }

    // Removed elsewhere counts too, and a save brings it back
    private bool IsChangedElsewhere()
    {
        try
        {
            return _store.Read(_file!.Path) != _saved!;
        }
        catch (FileNotFoundException)
        {
            return true;
        }
    }

    private bool ConfirmLeave()
    {
        if (!IsModified)
        {
            return true;
        }

        return _view.AskSaveChanges(_file!.Name) switch
        {
            ConfigEditorSaveChoice.Save => Save(),
            ConfigEditorSaveChoice.Discard => true,
            _ => false,
        };
    }

    private void View_FileSelected(object? sender, EditableFileModel file)
    {
        if (file == _file)
        {
            return;
        }

        if (ConfirmLeave())
        {
            Load(file);
        }
        else
        {
            _view.ShowFiles(_files, _file);
        }
    }

    private void View_TextEdited(object? sender, EventArgs e) => Analyze();

    private void View_SuggestionsRequested(object? sender, SuggestionTrigger trigger)
    {
        _suggestions = _file is null || _saved is null
            ? null
            : _language.Suggest(_file.Kind, _view.CodeText, _view.CaretPosition, trigger);

        // Nothing to offer, or only what's typed already
        if (_suggestions is not { Items.Count: > 0 } suggestions
            || (suggestions.Items.Count == 1 && suggestions.Items[0].Insert == TypedOf(suggestions)))
        {
            _suggestions = null;
            _view.HideSuggestions();

            return;
        }

        // Offered on a new line, nothing is chosen until the user moves to it, so Enter can make another line
        _view.ShowSuggestions(suggestions.Items, selectFirst: trigger != SuggestionTrigger.NewLine);
    }

    private void View_SuggestionChosen(object? sender, int index)
    {
        if (_suggestions is not { } suggestions || index < 0 || index >= suggestions.Items.Count)
        {
            return;
        }

        string insert = suggestions.Items[index].Insert;
        _suggestions = null;
        _view.HideSuggestions();
        _view.ReplaceText(suggestions.Start, suggestions.Length, insert);

        // What comes next is offered at once: a function after "module.", a key after "Ctrl+", a shortcut after " = "
        if (insert.EndsWith('.') || insert.EndsWith('+') || insert.EndsWith(' '))
        {
            View_SuggestionsRequested(this, SuggestionTrigger.Typing);
        }
    }

    private void View_HoverRequested(object? sender, CodeHoverModel hover)
    {
        if (_file is not null && _saved is not null)
        {
            hover.Text = _language.Describe(_file.Kind, _view.CodeText, hover.Position);
        }
    }

    private string TypedOf(CodeSuggestionsModel suggestions)
    {
        string text = _view.CodeText;
        int end = Math.Min(text.Length, suggestions.Start + suggestions.Length);

        return suggestions.Start <= end ? text[suggestions.Start..end] : string.Empty;
    }

    private void View_SaveRequested(object? sender, EventArgs e) => Save();

    private void View_NewScriptRequested(object? sender, EventArgs e)
    {
        if (_view.AskNewScriptName() is not string name || !ConfirmLeave())
        {
            return;
        }

        if (!_catalog.TryCreateScript(name, out EditableFileModel? file, out string error))
        {
            _view.ShowError(error);

            return;
        }

        RefreshFiles();
        Load(file);
    }

    // The file open stays open, selected in the new list, or shown alone when it's no longer there
    private void View_RefreshFilesRequested(object? sender, EventArgs e)
    {
        RefreshFiles();
        EditableFileModel? selected = _file is null
            ? null
            : _files.FirstOrDefault(file => SamePath(file.Path, _file.Path)) ?? _file;
        _view.ShowFiles(_files, selected);
    }

    private void View_ProblemSelected(object? sender, int index)
    {
        if (index >= 0 && index < _problems.Count)
        {
            _view.GoToLine(_problems[index].Line);
        }
    }

    // Saved in another program while this file is open without changes: show what it has now
    private void View_ViewActivated(object? sender, EventArgs e)
    {
        if (_file is null || _saved is null || IsModified)
        {
            return;
        }

        try
        {
            if (_store.Read(_file.Path) != _saved)
            {
                Load(_file);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Told when saving, if it's still so
        }
    }

    private void View_CloseRequested(object? sender, CancelEventArgs e)
    {
        e.Cancel = !ConfirmLeave();
    }
}
