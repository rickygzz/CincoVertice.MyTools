# Getting Started

## Quick Start

### What you get

The release zip contains three things:

- `CincoVertice.MyTools.Ui.Win.exe`, the application. It is self-contained, so you do not need to install any .NET runtime.
- A `licenses` folder with `Roboto-OFL.txt`, the license for the Roboto font that is built into the program. Keep it next to the exe.
- A `docs\user-guide` folder with this guide, as Markdown files. Start with `README.md`. They open in any text editor, and read best in one that shows Markdown, such as Visual Studio Code.

### First run

1. Unpack the zip into a folder where you can write files. Settings are stored in a `config` folder next to the exe, so a protected folder (for example one that needs administrator rights) causes problems. See [Troubleshooting](11-troubleshooting.md).
2. Double-click `CincoVertice.MyTools.Ui.Win.exe`.
3. A tray icon appears. No window opens. The icon's tooltip reads "My Tools".
4. If the icon is not visible, check the hidden icons area of the taskbar. Whether Windows hides it there is up to Windows.

On the first run the app creates a `config` folder next to the exe. It contains:

- `hotkeys.cfg`, your hotkey list.
- `scripts\GoogleSearch.mts`, an example script.

Nothing on screen tells you where the `config` folder is. To find it, right-click the tray icon and choose **Scripts > Open scripts folder**. That opens `config\scripts` in Explorer; the `config` folder is one level up.

If another program already uses one of the default hotkeys, a notification titled "My Tools: some hotkeys are off" appears. See [Hotkey problems](02-hotkeys.md#hotkey-problems).

### Try it

Press **Ctrl+Alt+C**. The calculator opens. Type `2+2*3` and press **Enter**. The result `8` appears below. Press **Esc** to close it.

### Running it twice

Only one copy runs at a time. If you start the exe while it is already running, the new copy exits silently. Normally nothing visible happens. Any other program with the same process name also blocks the start.

---

## Core Concepts

- **Tray app.** My Tools has no main window in normal use. You work through the tray icon menu and hotkeys.
- **Action.** Every feature you can bind to a hotkey is an action with a name, such as `ShowCalculator`. Each script you add is also an action.
- **Hotkey (shortcut).** A key combination that works system-wide, whichever program is in front. You set hotkeys in `config\hotkeys.cfg`.
- **Config files.** `hotkeys.cfg` and `activity.cfg` are plain text. Each line is `Name = Value`. A line starting with `#` is a comment and is ignored.
- **Script.** A text file ending in `.mts` in `config\scripts`. Its file name (letters, digits and `_` only) becomes the action name.
- **Config folder.** All settings live in the `config` folder next to the exe. The folder is found relative to the exe, not to where you started it from.

---

## The Tray Icon and Menu

**Tray icon.** The tooltip is "My Tools". Double-click it to open the main window (see below). The icon turns **red** while the mouse mover is on.

**Right-click menu**, in order:

| Item | What it does |
|---|---|
| **Open** (bold) | Shows the main window |
| **Diagram editor** | Opens a new diagram editor window |
| **Check folder for standard...** | Checks a folder of C# and JavaScript files |
| **Fix folder for standard...** | Rewrites a folder of C# and JavaScript files to match the standard |
| (separator) | |
| **Scripts** | Submenu: one item per script (click to run it), then **Edit scripts...** and **Open scripts folder**. A separator appears between the scripts and these two items only when at least one script exists. |
| **Hotkey problems (N)...** | Appears only while some hotkeys are off. Shows all the problems. |
| **Edit hotkeys...** | Opens `hotkeys.cfg` in the config editor |
| **Reload hotkeys** | Re-reads `hotkeys.cfg` and the scripts folder |
| **Edit activity settings...** | Opens `activity.cfg` in the config editor |
| **Edit window zones...** | Opens `zones.cfg` in the config editor. See [Window Zones](13-window-zones.md). |
| (separator) | |
| **Start with Windows** | A checkable item. See below. |
| **Check for updates at start** | A checkable item, off at first. When checked, the app looks for a newer version a minute after it starts. See [Updating](10-files-and-updates.md#updating). |
| (separator) | |
| **Check for updates...** | Asks GitHub for the latest release now, and offers to open its page if it is newer |
| **About My Tools...** | Shows which version you have, and the address of the releases page |
| **Exit** | Hides the icon and ends the app |

**Main window.** Its title is "My Tools" and it has only a **File > Code review** menu. That item opens an empty Code Review window and starts nothing. Use the tray items **Check folder for standard...** and **Fix folder for standard...** instead. Minimizing or closing the main window hides it in the tray.

**Notifications.** The app shows balloons from the tray icon. Informational ones stay about 5 seconds, warnings about 10 seconds. Examples:

- "Hotkeys reloaded." (title "My Tools") appears after a reload that found no problems. It does not appear at startup.
- "Could not change Start with Windows: <reason>" is a warning shown if the Windows setting could not be changed.
- "Version <x.y.z> is out. Click here to open its page." appears after start when **Check for updates at start** is checked and a newer version is out. Clicking it opens the release page in your browser.

**Exit** does not check for running scripts, the mouse mover, or unsaved diagram, config or snip windows. Save your work first.

---

## Start with Windows

Right-click the tray icon and click **Start with Windows** to make My Tools start when you sign in. Click it again to turn that off. No administrator rights are needed.

- The check mark is refreshed each time the menu opens.
- It shows as checked only when the startup entry points to the exe that is running now and Windows has not switched it off.
- Turning it on or off also clears any switch-off you made in the Windows startup settings for My Tools.
- If you move the exe after enabling the option, the entry still points to the old location and the menu item shows unchecked. Click it once to point it at the new location.
- **Exit** does not remove the startup entry. Turn **Start with Windows** off first if you want to remove it (see [Updating and Uninstalling](10-files-and-updates.md#updating-and-uninstalling)).
- The entry starts the exe with `--startup`, which tells My Tools that Windows started it, so it runs the [startup actions](#startup-actions). An entry made by an older version has no `--startup`: it still counts as on, and My Tools adds `--startup` to it the next time it starts.

### Startup actions

`config\startup.cfg` lists actions to run when Windows starts My Tools at sign-in. Starting My Tools yourself does not run them, so restarting it, for example after an update, doesn't start your programs again. Open the file in the config editor (tray menu, **Edit hotkeys...**, then `startup.cfg` in the list). It is created the first time My Tools starts with Windows, or the config editor opens, with only comments, so nothing runs until you add a line:

```
# Each line: Run = Action, or Run = Action, value
Run = StartLater, C:\Program Files\Slack\slack.exe
Run = ShowCalculator
```

- An action is any action you can bind in hotkeys.cfg: a built-in one or a script. The value after the comma is what the script gets from `script.argument()`.
- They run in order, each after the one before ends. A script that waits holds back the lines after it, while the tray and the hotkeys keep working.
- Changes apply at the next sign-in. A line that can't be used is skipped and shown in a notification titled "My Tools: some startup actions are off", and squiggled in the config editor. So is an action that fails, and the next one still runs.

To keep a program from slowing down your sign-in, start it from a script that first waits until the computer is quiet with `system.waitidle`: see [Start a program once the computer is quiet](08-scripts.md#start-a-program-once-the-computer-is-quiet).

---

[Contents](README.md) | Next: [Hotkeys and hotkeys.cfg](02-hotkeys.md) →
