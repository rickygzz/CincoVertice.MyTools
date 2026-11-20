# Config Editor

The config editor opens when you choose **Edit hotkeys...**, **Edit activity settings...** or **Edit scripts...** from the tray menu.

- **Only one config editor window exists.** Asking for another file shows it in the same window.
- The title is `<file> - Config editor`, with `*` after the file name when there are unsaved changes.
- The left side lists your files: `hotkeys.cfg`, `activity.cfg`, `startup.cfg`, `zones.cfg`, `recording.cfg`, then each script. The right side is a text editor with syntax colors. Below it, a list of problems appears only when there are problems. Click a problem to jump to its line.
- The toolbar has **New script...**, **Save** (tooltip "Save (Ctrl+S)"), **Refresh** (F5), which reads the list of files again, as after you add, rename or delete a script in Explorer, and a zoom button showing a percentage such as "100%" (tooltip "Ctrl+wheel to zoom. Click, or Ctrl+0, for 100%").
- Keys: **Ctrl+S** saves, **F5** refreshes the file list, **Ctrl+0** resets zoom and **Ctrl+Space** shows [suggestions](#suggestions).

## Suggestions

In every file, a list of what you can write there opens under the caret as you type, each with what it is or does beside it. In the config files (all but scripts) it also opens when **Enter** starts a new line, with nothing chosen yet, so Enter again just makes another line; press Down to choose. **Ctrl+Space** opens it anywhere it hasn't opened by itself.

| Key | Effect |
|---|---|
| Up / Down, Page Up / Page Down | Choose |
| Enter or Tab, or a click | Write the one chosen, in place of what you typed of it; with none chosen, Enter and Tab go to the text |
| Esc | Close the list |

Typing goes on into the text and narrows the list. Moving the caret away closes it.

In **`hotkeys.cfg`**:

- At the start of a line: every action, your scripts included, with what it does. Names that start with what you typed come first, then those that have it elsewhere, so `zone` finds `MoveToZone`. On a new line, the action is written with ` = ` after it, and the list goes on with the shortcut.
- After `=` and after each `+`: the modifiers the shortcut doesn't have yet (`Ctrl`, `Alt`, `Shift`, `Win`), then the keys. A key that would make a shortcut another line already has says so, such as "already used by ToggleActivity".
- After the comma of a `MoveToZone` line: the zone numbers, up to the most zones a monitor has.

In **scripts**:

- After a module and a dot, such as `clipboard.`: its functions, with how each is called and what it does, such as `clipboard.pasteastext(): Pastes the clipboard's text without its formatting`. Choosing one writes its parentheses too, with the caret between them when it takes values.
- Elsewhere, a word you start: the variables the script sets, the keywords (`if`, `else`, `end`...), the modules, which list their functions, and `text(value)` and `number(text)`.
- In `script.include("...")`: your libraries (scripts whose names start with `_`). In `keyboard.press("...")`: the modifiers, `AltGr` too, and the keys.
- Nothing in comments, numbers or other texts.

In **`activity.cfg`**, **`recording.cfg`**, **`startup.cfg`** and **`zones.cfg`**:

- At the start of a line: the settings, with what each means, what it allows and its default, such as `Interval` "Milliseconds between checks (100 to 3,600,000; default 5000)". A setting already in the file isn't offered again, except those that can be on several lines (`Run`, `Zone`, `Monitor`); in `zones.cfg` each monitor's part of the file counts on its own. On a new line, the setting is written with ` = ` after it, and its values follow.
- After `=`: its values: the default, `true` and `false`, `Low`, `Medium` and `High`, the ready layouts (`Columns 3`, `Columns 25 50 25`, `Grid 3 x 2`...), or for `Run` every action.
- In `zones.cfg`, after `Monitor =`: the place and size of each monitor connected now, such as `1920, 0, 2560, 1440` "Monitor 2 (2560 x 1440), connected now", so you don't have to look them up.
- In `startup.cfg`, after `Run = MoveToZone,`: the zone numbers.

## Help on a name

Rest the mouse pointer on a name to see what it is: in `hotkeys.cfg` and `startup.cfg` an action, such as "ShowCalculator: Shows the calculator"; in the other config files a setting, with what it allows and its default; in scripts a function, with how it's called and what it does, or a module, with its functions. A problem underlined there is shown first.

## Problems the editor shows

A moment after each edit, the editor checks the file and lists problems as `Line N: <message>`. For `hotkeys.cfg`, `activity.cfg`, `startup.cfg` and `zones.cfg` this uses the same messages as described above, except the messages marked "tray list only" or "balloon only", which the editor never shows. For scripts, only the **first** syntax error is shown, with its line and column. Errors that can only be found when a script runs (such as an unknown function or a wrong argument type) are not shown.

## Dialogs

Titled "Config editor":

- `Save the changes to <name>?` (Yes, No or Cancel), when you switch away from a modified file or close.
- `<name> was changed elsewhere since it was opened. Replace it with this text?` (Yes or No).
- `<name> can't be opened: <reason>` and `<name> can't be saved: <reason>`. Files must be valid UTF-8; otherwise the reason is `<name> is not valid UTF-8.`

Saving keeps the file's line endings and byte-order mark. If you come back to the window and the file changed on disk while you had no edits, it reloads silently.

## New script

Click **New script...**. A dialog titled "New script" asks "Script name:" with **OK** and **Cancel**.

- `.mts` is added if you leave it off.
- The name needs letters, digits or `_`. Otherwise: `'<name>' can't be a script's name: use letters, digits or _.`
- If another action already has that name: `There's already an action named <X>.`
- If the file exists: `There's already a script named <file>.`
- Other failures: `<file> can't be created: <reason>`

The new file starts with the comment line `# Bind this script in hotkeys.cfg as <Action>. See docs/user-guide/08-scripts.md.`, which points to the [Scripts](08-scripts.md) chapter of this guide. A name starting with `_` creates a library script instead (see below), starting with `# <name>: for other scripts to run with script.include. See docs/user-guide/08-scripts.md.`

---

← Previous: [Mouse Mover and Keep Awake](06-mouse-mover.md) | [Contents](README.md) | Next: [Scripts](08-scripts.md) →
