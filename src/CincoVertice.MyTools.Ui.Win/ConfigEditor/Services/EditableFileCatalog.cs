using CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

namespace CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;

/// <summary>
///     hotkeys.cfg, activity.cfg and every script. Loading each config file creates it when it doesn't exist yet.
/// </summary>
public sealed class EditableFileCatalog : IEditableFileCatalog
{
    private readonly HotkeyConfigService _hotkeys;
    private readonly ActivitySettingsService _activity;
    private readonly Func<IReadOnlyCollection<IAppAction>> _actions;
    private readonly string _scriptsFolderPath;

    public EditableFileCatalog(
        HotkeyConfigService hotkeys,
        ActivitySettingsService activity,
        Func<IReadOnlyCollection<IAppAction>> actions,
        string scriptsFolderPath)
    {
        _hotkeys = hotkeys;
        _activity = activity;
        _actions = actions;
        _scriptsFolderPath = scriptsFolderPath;
    }

    public IReadOnlyList<EditableFileModel> GetFiles()
    {
        _hotkeys.Load(_actions());
        _activity.Load();

        List<EditableFileModel> files =
        [
            new(_hotkeys.FilePath, Path.GetFileName(_hotkeys.FilePath), EditableFileKind.Hotkeys),
            new(_activity.FilePath, Path.GetFileName(_activity.FilePath), EditableFileKind.ActivitySettings),
        ];

        files.AddRange(ScriptCatalog.GetScriptFiles(_scriptsFolderPath).Select(ToScript));

        return files;
    }

    public bool TryCreateScript(string name, out EditableFileModel? file, out string error)
    {
        file = null;
        error = string.Empty;

        string baseName = name.Trim();

        if (baseName.EndsWith(ScriptCatalog.Extension, StringComparison.OrdinalIgnoreCase))
        {
            baseName = baseName[..^ScriptCatalog.Extension.Length];
        }

        string fileName = baseName + ScriptCatalog.Extension;

        if (baseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || ScriptCatalog.GetActionName(fileName).Length == 0)
        {
            error = $"'{name}' can't be a script's name: use letters, digits or _.";

            return false;
        }

        string path = Path.Combine(_scriptsFolderPath, fileName);

        if (File.Exists(path))
        {
            error = $"There's already a script named {fileName}.";

            return false;
        }

        try
        {
            Directory.CreateDirectory(_scriptsFolderPath);
            File.WriteAllText(path, $"# {baseName}: bind it in hotkeys.cfg by its name. See SCRIPTING.md.\n\n");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = $"{fileName} can't be created: {ex.Message}";

            return false;
        }

        file = ToScript(path);

        return true;
    }

    private static EditableFileModel ToScript(string path) =>
        new(path, Path.GetFileName(path), EditableFileKind.Script);
}
