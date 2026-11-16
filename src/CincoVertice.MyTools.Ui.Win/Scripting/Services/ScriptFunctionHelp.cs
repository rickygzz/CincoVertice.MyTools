using CincoVertice.MyTools.Ui.ConfigEditor.Models;

namespace CincoVertice.MyTools.Ui.Win.Scripting.Services;

/// <summary>
///     How each script function is called and what it does, in a sentence, for the config editor's suggestions. A
///     test checks it has every function of BuiltInFunctions, and only those: a new function needs its line here.
/// </summary>
public static class ScriptFunctionHelp
{
    /// <summary>The script language's keywords; a test checks the lexer knows each.</summary>
    public static IReadOnlyList<string> Keywords { get; } =
        ["if", "elseif", "else", "end", "and", "or", "not", "true", "false"];

    public static IReadOnlyList<ScriptFunctionModel> Functions { get; } =
    [
        Of("dialog.ask", "dialog.ask(prompt, default)", "Asks for an answer, filled with default; Cancel stops"),
        Of("dialog.form", "dialog.form(title, prompts, defaults)", "Asks several answers in one dialog, as a list"),
        Of("dialog.message", "dialog.message(text)", "Shows a message and waits for OK"),
        Of("dialog.confirm", "dialog.confirm(question)", "Asks Yes or No: true for Yes"),
        Of("clipboard.gettext", "clipboard.gettext()", "The clipboard's text, or \"\" when it has none"),
        Of("clipboard.settext", "clipboard.settext(text)", "Puts text in the clipboard"),
        Of("clipboard.clear", "clipboard.clear()", "Empties the clipboard"),
        Of("clipboard.paste", "clipboard.paste(text)", "Pastes the text with Ctrl+V, then puts the clipboard back"),
        Of("clipboard.pasteastext", "clipboard.pasteastext()", "Pastes the clipboard's text without its formatting"),
        Of("keyboard.type", "keyboard.type(text)", "Types the text in the active window, in any keyboard layout"),
        Of("keyboard.press", "keyboard.press(keys, times)", "Presses keys, written as in hotkeys.cfg"),
        Of("window.title", "window.title()", "The active window's title"),
        Of("window.tozone", "window.tozone(number)", "Moves the active window to that zone of its monitor"),
        Of("shell.open", "shell.open(target)", "Opens a web address, a file or a folder"),
        Of("shell.run", "shell.run(program, arguments)", "Starts a program, without waiting for it"),
        Of("tray.notify", "tray.notify(text)", "Shows a tray notification and goes on at once"),
        Of("system.waitidle", "system.waitidle(cpu, disk, seconds, minutes)", "Waits until the computer is quiet"),
        Of("script.wait", "script.wait(milliseconds)", "Pauses, up to 10 minutes"),
        Of("script.stop", "script.stop()", "Ends the script quietly"),
        Of("script.argument", "script.argument(default)", "The value after the shortcut in hotkeys.cfg, or default"),
        Of("script.include", "script.include(name)", "Runs another script, with the same variables"),
        Of("global.set", "global.set(name, value)", "Stores a value kept between runs"),
        Of("global.get", "global.get(name, default)", "Reads a stored value, or default"),
        Of("global.has", "global.has(name)", "Whether a value was stored under the name"),
        Of("global.remove", "global.remove(name)", "Removes a stored value"),
        Of("global.names", "global.names()", "The names of every stored value"),
        Of("text.length", "text.length(text)", "The number of characters"),
        Of("text.upper", "text.upper(text)", "The text in capitals"),
        Of("text.lower", "text.lower(text)", "The text in small letters"),
        Of("text.trim", "text.trim(text)", "The text without spaces at the start and end"),
        Of("text.replace", "text.replace(text, old, new)", "The text with every old replaced by new"),
        Of("text.contains", "text.contains(text, part)", "Whether part is in the text, ignoring case"),
        Of("text.urlencode", "text.urlencode(text)", "The text made safe for a web address"),
        Of("list.length", "list.length(list)", "The number of items"),
        Of("list.contains", "list.contains(list, item)", "Whether an item equals item"),
        Of("text", "text(value)", "Any value as text"),
        Of("number", "number(text)", "The text as a number"),
    ];

    private static ScriptFunctionModel Of(string name, string signature, string description) =>
        new(name, signature, description, !signature.EndsWith("()", StringComparison.Ordinal));
}
