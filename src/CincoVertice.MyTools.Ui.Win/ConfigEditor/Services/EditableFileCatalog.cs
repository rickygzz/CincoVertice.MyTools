using CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;
using CincoVertice.MyTools.Ui.ConfigEditor.Models;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;

namespace CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;

/// <summary>
///     hotkeys.cfg, activity.cfg, startup.cfg and every script. Loading each config file creates it when it doesn't
///     exist yet.
/// </summary>
public sealed class EditableFileCatalog : IEditableFileCatalog
{
    private readonly HotkeyConfigService _hotkeys;
    private readonly ActivitySettingsService _activity;
    private readonly StartupConfigService _startup;
    private readonly Func<IReadOnlyCollection<IAppAction>> _actions;
    private readonly string _scriptsFolderPath;

    public EditableFileCatalog(
        HotkeyConfigService hotkeys,
        ActivitySettingsService activity,
        StartupConfigService startup,
        Func<IReadOnlyCollection<IAppAction>> actions,
        string scriptsFolderPath)
    {
        _hotkeys = hotkeys;
        _activity = activity;
        _startup = startup;
        _actions = actions;
        _scriptsFolderPath = scriptsFolderPath;
    }

    public IReadOnlyList<EditableFileModel> GetFiles()
    {
        _hotkeys.Load(_actions());
        _activity.Load();
        _startup.Load(_actions());

        List<EditableFileModel> files =
        [
            new(_hotkeys.FilePath, Path.GetFileName(_hotkeys.FilePath), EditableFileKind.Hotkeys),
            new(_activity.FilePath, Path.GetFileName(_activity.FilePath), EditableFileKind.ActivitySettings),
            new(_startup.FilePath, Path.GetFileName(_startup.FilePath), EditableFileKind.StartupActions),
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
        string actionName = ScriptCatalog.GetActionName(fileName);

        if (baseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || actionName.Length == 0)
        {
            error = $"'{name}' can't be a script's name: use letters, digits or _.";

            return false;
        }

        // hotkeys.cfg runs the first action with a name, so a second one could never be bound
        if (!ScriptCatalog.IsLibrary(fileName) && IsActionNameTaken(actionName))
        {
            error = $"There's already an action named {actionName}.";

            return false;
        }

        string path = Path.Combine(_scriptsFolderPath, fileName);
        string firstLine = ScriptCatalog.IsLibrary(fileName)
            ? $"# {baseName}: for other scripts to run with script.include. See docs/user-guide/08-scripts.md."
            : $"# Bind this script in hotkeys.cfg as {actionName}. See docs/user-guide/08-scripts.md.";

        try
        {
            Directory.CreateDirectory(_scriptsFolderPath);

            // CreateNew: this never replaces a file, even one another program creates at this very moment
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            using var writer = new StreamWriter(stream);

            writer.Write(firstLine + "\n\n");
        }
        catch (IOException) when (File.Exists(path))
        {
            error = $"There's already a script named {fileName}.";

            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = $"{fileName} can't be created: {ex.Message}";

            return false;
        }

        file = ToScript(path);

        return true;
    }

    private bool IsActionNameTaken(string actionName)
    {
        IEnumerable<string> scriptActions = ScriptCatalog.GetScriptFiles(_scriptsFolderPath)
            .Where(path => !ScriptCatalog.IsLibrary(path))
            .Select(ScriptCatalog.GetActionName);

        return _actions()
            .Select(action => action.Name)
            .Concat(scriptActions)
            .Contains(actionName, StringComparer.OrdinalIgnoreCase);
    }

    private static EditableFileModel ToScript(string path) =>
        new(path, Path.GetFileName(path), EditableFileKind.Script);
}
