using System.Text;

namespace CincoVertice.MyTools.Ui.Win.Scripting.Services;

/// <summary>
///     The script files in config\scripts next to the executable. Each one becomes an action named after the file,
///     which can be bound to a shortcut in hotkeys.cfg.
/// </summary>
public static class ScriptCatalog
{
    public const string Extension = ".mts";

    private const string GoogleSearchScript =
        """
        # My Tools script. Bind it in hotkeys.cfg, e.g. GoogleSearch = Ctrl+Alt+G; it applies when you save.
        # The action's name is the file name; changes to this file apply the next time it runs.
        #
        # Functions, by module (see docs/user-guide/08b-script-functions.md for details):
        #   dialog.ask(prompt, default)  dialog.form(title, [prompts], [defaults])  dialog.message(text)
        #   dialog.confirm(question)  clipboard.gettext()  clipboard.settext(text)  keyboard.type(text)
        #   clipboard.clear()  clipboard.pasteastext()  window.title()  window.tozone(number)
        #   keyboard.press("Ctrl+V")  keyboard.press("Tab", 3)
        #   window.activate(title)  window.activateoropen(title, program)  file.read(path)  file.write(path, text)
        #   window.ontop()  file.append(path, text)  file.exists(path)
        #   w = process.window("notepad")  window.hide(w)  window.show(w)  window.minimize(w)  window.state(w)
        #   window.position(w)  window.move(w, x, y)  window.resize(w, width, height)  window.wait(title, seconds)
        #   mouse.move(x, y)  mouse.click()  mouse.click("right")  mouse.drag(x, y)  mouse.scroll(-3)
        #   file.copy(from, to)  file.move(from, to)  file.delete(path)  folder.files(path, "*.txt")  dialog.openfile(title)
        #   process.end(name)  shell.runwait(program, arguments)
        #   date.now()  date.format(format)  date.format(date, format)
        #   shell.open(address or file)  shell.run(program, arguments)  tray.notify(text)
        #   script.wait(ms)  script.stop()  system.waitidle(cpu %, disk %, quiet seconds, most minutes)
        #   global.set(name, value)  global.get(name, default)  (kept between runs, in config\global.vars)
        #   text.length  text.upper  text.lower  text.trim  text.replace(text, old, new)  text.contains(text, part)
        #   text.urlencode  list.length(list)  list.contains(list, item)  number(value)  text(value)
        #   text.split(text, ",")  text.join(list, ",")  text.substring(text, 1, 3)  text.match(text, pattern)
        #   math.round(x, 2)  math.min(a, b)  math.random(1, 6)  list.sort(list)  list.indexof(list, item)
        # Lists:   [a, b, c]  list[1] is the first item  list + list  list.range(1, 10)
        # Logic:   name = value   if ... elseif ... else ... end   == != < <= > >=   and or not   + - * / %
        # Maps:    m = {"name": "Ann"}  m["name"]  map.set(m, "age", 30)  map.keys(m)  map.get(m, "x", 0)
        # Loops:   for item in list ... end   break   continue
        # Functions: function name(a, b) ... return value ... end   (at the top of the script; call it as name(1, 2))
        #
        # To ask several things in one dialog:
        #   answers = dialog.form("Search Google", ["Search for:", "Only on site:"], [clipboard.gettext(), ""])
        #   query = answers[1]

        query = dialog.ask("Search Google for:", clipboard.gettext())

        if text.trim(query) != ""
            shell.open("https://www.google.com/search?q=" + text.urlencode(query))
        end

        """;

    private const string PasteAsTextScript =
        """
        # My Tools script: pastes the clipboard's text without its formatting (fonts, colors, links, tables), as
        # "Paste as plain text" does in programs that have it. Bind it in hotkeys.cfg, e.g. PasteAsText = Ctrl+Alt+V.
        # The clipboard keeps its formatting afterwards, so Ctrl+V still pastes the formatted version.

        if clipboard.gettext() == ""
            tray.notify("The clipboard has no text to paste")
            script.stop()
        end

        clipboard.pasteastext()

        """;

    /// <summary>The example scripts written with the folder, the first time: file names and their text.</summary>
    public static IReadOnlyList<(string FileName, string Script)> Examples { get; } =
    [
        ("GoogleSearch" + Extension, GoogleSearchScript),
        ("PasteAsText" + Extension, PasteAsTextScript),
    ];

    public static string FolderPath { get; } =
        Path.Combine(AppContext.BaseDirectory, "config", "scripts");

    /// <summary>
    ///     config\global.vars next to the executable: the global variables of global.set and global.get.
    /// </summary>
    public static string GlobalVariablesFilePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "config", "global.vars");

    /// <summary>
    ///     The script files in <paramref name="folderPath"/> (normally <see cref="FolderPath"/>), sorted by name.
    ///     Creates the folder with the <see cref="Examples"/> the first time.
    /// </summary>
    public static IReadOnlyList<string> GetScriptFiles(string folderPath)
    {
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                foreach ((string fileName, string script) in Examples)
                {
                    File.WriteAllText(Path.Combine(folderPath, fileName), script);
                }
            }

            // EnumerationOptions avoids the legacy match where *.mts also matches *.mtsx
            return [.. Directory.EnumerateFiles(folderPath, "*" + Extension, new EnumerationOptions())
                .Order(StringComparer.OrdinalIgnoreCase)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>
    ///     Whether the file is only for script.include: its name starts with "_", as in "_PrCommon.mts". It is not an
    ///     action, so it is not in the Scripts menu and can't be bound in hotkeys.cfg.
    /// </summary>
    public static bool IsLibrary(string filePath) => Path.GetFileName(filePath).StartsWith('_');

    /// <summary>
    ///     The action name for a script file: its name without the letters hotkeys.cfg can't use, so
    ///     "Google Search.mts" is "GoogleSearch". Empty when nothing is left.
    /// </summary>
    public static string GetActionName(string filePath)
    {
        var sb = new StringBuilder();

        foreach (char c in Path.GetFileNameWithoutExtension(filePath))
        {
            if (char.IsAsciiLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
