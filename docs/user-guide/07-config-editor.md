# Config Editor

The config editor opens when you choose **Edit hotkeys...**, **Edit activity settings...** or **Edit scripts...** from the tray menu.

- **Only one config editor window exists.** Asking for another file shows it in the same window.
- The title is `<file> - Config editor`, with `*` after the file name when there are unsaved changes.
- The left side lists your files: `hotkeys.cfg`, `activity.cfg`, `startup.cfg`, `zones.cfg`, `recording.cfg`, then each script. The right side is a text editor with syntax colors. Below it, a list of problems appears only when there are problems. Click a problem to jump to its line.
- The toolbar has **New script...**, **Save** (tooltip "Save (Ctrl+S)") and a zoom button showing a percentage such as "100%" (tooltip "Ctrl+wheel to zoom. Click, or Ctrl+0, for 100%").
- Keys: **Ctrl+S** saves and **Ctrl+0** resets zoom.

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
