# Script Functions

Functions are grouped in modules: you call one as `module.function(...)`, as in `clipboard.gettext()`. Only the two conversions, `number()` and `text()`, are global, since they work on any value.

| Module | For | Functions |
|---|---|---|
| [`dialog`](#dialog) | Asking and showing | `ask` `form` `message` `confirm` `openfile` `savefile` `folder` |
| [`clipboard`](#clipboard) | The clipboard | `gettext` `settext` `clear` `paste` `pasteastext` |
| [`keyboard`](#keyboard) | Typing and pressing keys | `type` `press` |
| [`window`](#window) | Windows | `title` `tozone` `tomouse` `activate` `activateoropen` `ontop` `find` `active` `state` `hide` `show` `minimize` `maximize` `restore` `close` `position` `move` `resize` `list` `process` `wait` `waitclose` |
| [`mouse`](#mouse) | The mouse pointer and its buttons | `position` `move` `click` `down` `up` `drag` `scroll` |
| [`process`](#process) | Programs running | `isrunning` `window` `windows` `end` `waitclose` |
| [`shell`](#shell) | Web pages, files and programs | `open` `run` `runwait` |
| [`tray`](#tray) | Notifications | `notify` |
| [`system`](#system) | The computer | `waitidle` |
| [`file`](#file) | Files | `read` `write` `append` `exists` `copy` `move` `delete` `name` |
| [`folder`](#folder) | Folders | `exists` `create` `files` `folders` |
| [`date`](#date) | The date and time | `now` `format` |
| [`script`](#script) | The running script | `wait` `stop` `argument` `include` |
| [`global`](#global) | Values kept between runs | `set` `get` `has` `remove` `names` |
| [`text`](#text) | Working with text | `length` `upper` `lower` `trim` `replace` `contains` `urlencode` |
| [`list`](#list) | Working with lists | `length` `contains` `range` |
| [Global](#conversions) | Conversions | `number` `text` |

Names are not case-sensitive: `Clipboard.GetText()` works too. A function that expects a text also accepts a number or true/false, which it turns into text: `dialog.message(5)` works. Functions that return nothing can't be stored or used in a calculation.

If a script uses a name from before the modules, the error says the new one, e.g. "Unknown function 'copy': it is now clipboard.settext()". A misspelled function lists what its module has, e.g. "Unknown function 'clipboard.copy': clipboard has clear, gettext, paste, pasteastext, settext".

## dialog

| Function | What it does | Returns |
|---|---|---|
| `dialog.ask(prompt)`<br>`dialog.ask(prompt, default)` | Shows a box to type one answer, filled with `default`. | The text typed. **Cancel stops the script.** |
| `dialog.form(title, prompts)`<br>`dialog.form(title, prompts, defaults)` | Shows **one** dialog with a box for each text in the list `prompts`, filled with the list `defaults`. Missing defaults start empty; a default that is a list makes a fixed list to pick from. | A list with the answers, in order. **Cancel stops the script.** |
| `dialog.message(text)` | Shows a message and waits for OK. | Nothing |
| `dialog.confirm(question)` | Asks a Yes/No question. | `true` for Yes, `false` for No |
| `dialog.openfile(title)`<br>`dialog.openfile(title, pattern)` | Asks for a file to open, showing only those that match `pattern`, as `"*.txt"` (all of them without one). | Its full path. **Cancel stops the script.** |
| `dialog.savefile(title)`<br>`dialog.savefile(title, name)` | Asks where to save a file, starting with `name`; it asks before replacing one that's there. | Its full path. **Cancel stops the script.** |
| `dialog.folder(title)` | Asks for a folder. | Its full path. **Cancel stops the script.** |

Dialogs are titled `My Tools: <script name>` and open on top of the program you are in, with the keyboard focus, so you can type right away.

### Asking several things at once with dialog.form

`dialog.form` asks several questions in **one** dialog, instead of one `dialog.ask` after another:

```mts
answers = dialog.form("New meeting note",
    ["Title:", "People:", "Room:"],
    ["Weekly sync", "", "Room 2"])

note_title = answers[1]
people = answers[2]
room = answers[3]

keyboard.type(note_title + "\nWith: " + people + "\nWhere: " + room)
```

It takes up to three values:

| Argument | What it is |
|---|---|
| `title` | The bold heading at the top of the dialog. The window itself is titled "My Tools: *script name*". |
| `prompts` | A list with the label of each box, top to bottom: `["Title:", "People:", "Room:"]` shows three boxes. It can't be empty. |
| `defaults` | Optional: a list with the starting text of each box, in the same order. A default that is itself a **list** makes that box a fixed list to pick from (see below). It can be shorter than `prompts`; the boxes without a default start empty. It can't be longer. |

In the dialog, the first box has the keyboard focus. **Tab** moves to the next box, **Enter** is OK and **Esc** is Cancel. Each box holds one line of text.

**Picking from a fixed list.** Give a list as the default of a box, and the box becomes a drop-down with only those items; the answer is the item picked. The first item starts selected, so put the most likely one first:

```mts
answers = dialog.form("Pull request for app1",
    ["Jira task:", "Color:"],
    [global.get("jira task", "DEV-700"), ["Red", "Blue", "Green"]])

color = answers[2]     # "Red", "Blue" or "Green"
```

To start with another item selected, give the items and the one to select as a pair, `[[items], selected]`. With `global`, the list remembers the last pick:

```mts
answers = dialog.form("Pull request",
    ["Jira task:", "App:"],
    [global.get("jira task", "DEV-700"), [["Finpro", "Mobile", "Life"], global.get("app", "Finpro")]])

global.set("app", answers[2])
```

When the item to select is not in the list (for example, it was removed from the list since), the first item is selected.

Text boxes and lists can be mixed in one form. A list of choices can't be empty. Its items can be numbers too, but the answer is still a text: `["1", "2"]` and `[1, 2]` both answer `"1"` or `"2"`.

`dialog.form` returns a **list** of the answers, in the order of the prompts: `answers[1]` is the first box. Give each answer a name, as above, so the rest of the script is easy to read. A few things to know:

- **Answers are always texts**, even when a number was typed. Use `number()` to calculate with one, and `text.trim()` to ignore spaces around it.
- **An empty box gives `""`**. Check it with `if answer == ""` when the box is required.
- **Cancel (or Esc, or closing the dialog) ends the script** quietly, like `dialog.ask`, so the lines after `dialog.form` only run after OK.
- **Defaults can come from anywhere**, e.g. `[clipboard.gettext(), window.title()]`.

Another example, with a number and a required box:

```mts
answers = dialog.form("Reminder", ["Remind me to:", "In how many minutes (1 to 10):"], ["Stretch", "5"])
what = text.trim(answers[1])
minutes = number(answers[2])

if what == ""
    dialog.message("Write what to remind you of.")
    script.stop()
end

tray.notify("OK, in " + minutes + " minutes")
script.wait(minutes * 60 * 1000)
dialog.message(what)
```

If the minutes box has something that isn't a number, `number()` stops the script with an error that says so. `script.wait` waits at most 10 minutes. [Search the web with one dialog](08c-script-examples.md#search-the-web-with-one-dialog) has one more example.

## clipboard

| Function | What it does | Returns |
|---|---|---|
| `clipboard.gettext()` | Reads the text in the clipboard. | The text, or `""` when it has none. |
| `clipboard.settext(text)` | Puts text in the clipboard. An empty text clears it. | Nothing |
| `clipboard.clear()` | Empties the clipboard. | Nothing |
| `clipboard.paste(text)` | Pastes the text into the active window with Ctrl+V, then, about half a second later, puts back what the clipboard held. Much faster than `keyboard.type` for long texts, and editors don't auto-indent or auto-complete it. `clipboard.paste("")` does nothing. | Nothing |
| `clipboard.pasteastext()` | Pastes the clipboard's text without its formatting (fonts, colors, links, tables) into the active window, with Ctrl+V. Afterwards the clipboard holds what it had before, formatting included, so Ctrl+V still pastes the formatted version. Does nothing when the clipboard has no text, e.g. only a picture. | Nothing |

## keyboard

| Function | What it does |
|---|---|
| `keyboard.type(text)` | Types the text into the active window, as if from the keyboard. Every character comes out as written, whatever the keyboard layout: `@`, `ñ`, `€`, accents and emoji. `\n` presses Enter and `\t` Tab. It first waits (up to 3 seconds, then types anyway) for you to release the hotkey's Ctrl, Alt, Shift and Win keys, so your hotkey doesn't turn the text into shortcuts. |
| `keyboard.press(keys)`<br>`keyboard.press(keys, times)` | Presses keys in the active window, written as in hotkeys.cfg: `keyboard.press("Ctrl+V")` pastes, `keyboard.press("Tab", 3)` presses Tab 3 times (a whole number from 1 to 100). A key alone works too: `"Enter"`, `"Esc"`, `"Down"`. It can also hold `AltGr`, which hotkeys can't: `keyboard.press("AltGr+2")`. Like `type`, it first waits for you to release the hotkey's keys. |

`keyboard.press` presses keys; `keyboard.type` writes text. `keyboard.type("^v")` types the characters `^v` (`+`, `^` and `{` are typed as themselves), and `keyboard.press("Ctrl+V")` pastes. The program needs a moment to react to a key, so after `keyboard.press("Ctrl+C")` wait a little before reading the clipboard.

Typing blocks the app until it finishes. Typing into a program that runs as administrator may depend on Windows; My Tools does nothing special for it.

**Keyboard layouts.** `keyboard.type` sends each character itself, not the key that types it, so `keyboard.type("@")` writes `@` on a Spanish, English or any other layout, even when the window you type into has a different layout from My Tools, and characters with no key on your keyboard are typed too. A few programs read only real keys and ignore typed characters: some games, and some remote desktop and virtual machine windows. In those, use `clipboard.paste(text)` instead.

`keyboard.press` presses real keys, so what a key types still depends on the layout of the window it goes to: `keyboard.press("AltGr+2")` types `@` on a Spanish layout and nothing on an English (US) one, which has no AltGr. To write a character, use `keyboard.type`. Use `AltGr` in `keyboard.press` for shortcuts that need that key, or for a dead key that a layout puts on AltGr, pressed before the letter it changes. `AltGr` can't go with `Ctrl` or `Alt`, since to Windows it already is Ctrl+Alt. A character that needs AltGr, such as `keyboard.press("@")` on a Spanish layout, is refused with `'@' is typed with AltGr; to write it, type it as text, or press AltGr and its key.`, because it would type `@` only on that layout.

## window

| Function | Returns |
|---|---|
| `window.title()`<br>`window.title(window)` | The title of the active window, the one you are working in, e.g. `"Inbox - Outlook"`, or of the window with that number (see [A window in a variable](#a-window-in-a-variable)). `""` when it has none, or is closed. |
| `window.tozone(number)` | Moves the active window to that zone of its monitor, from 1, as set in `zones.cfg`. Nothing. A number with no zone is an error. See [Window Zones](13-window-zones.md). |
| `window.tomouse()` | Moves the active window to the monitor the mouse pointer is on, at the same place on it, as large, but no larger than that monitor; a maximized window stays maximized. Nothing when it is on that monitor already. No active window is an error. Bind it to a hotkey to bring a window to the screen you look at. |
| `window.activate(title)`<br>`window.activate(window)` | Brings to the front the window whose title has `title` in it, ignoring case, as if you had clicked it: restored when minimized. When several have it, the one you used last. Only windows you can switch to count: not hidden ones, nor those on another virtual desktop. Given a window's number instead, brings that one, shown again first if it was hidden. `true` when there was one, `false` when not. |
| `window.activateoropen(title, program)`<br>`window.activateoropen(title, program, arguments)` | Brings the window to the front, as `window.activate` does; when there's none, starts `program` (as `shell.run` does) and waits up to 10 seconds for a window with `title` to show, then brings it to the front. `true` when a window is in front, `false` when the program showed none in time. |
| `window.ontop()`<br>`window.ontop(true)`<br>`window.ontop(false)`<br>`window.ontop(window, true)` | Keeps the active window on top of the other windows (`true`), or no longer (`false`); with nothing, switches it, as the `ToggleAlwaysOnTop` hotkey does. With a window's number first, changes that window instead: `window.ontop(w)` switches it. `true` when it's on top afterwards. No active window is an error, as are a closed window and a program run as administrator, which Windows doesn't let My Tools change. |
| `window.find(title)` | The number of the window whose title has `title` in it, ignoring case, as `window.activate` finds it, or else of a hidden one. `0` when there's none. |
| `window.active()` | The number of the active window. |
| `window.state(window)` | How the window is: `"normal"`, `"minimized"`, `"maximized"`, `"hidden"`, or `"closed"` once it's gone. |
| `window.hide(window)` | Hides the window, and its button on the taskbar. The program keeps running; `window.show` or `window.activate` brings it back. |
| `window.show(window)` | Shows a hidden window again, as it was, without bringing it to the front. |
| `window.minimize(window)`<br>`window.maximize(window)`<br>`window.restore(window)` | Minimizes the window, maximizes it, or makes it neither. |
| `window.close(window)` | Closes the window, as its X does: the program may ask to save first. |
| `window.position(window)` | Where the window is and how large, as a list: `[x, y, width, height]`, in screen pixels, as you see it (without the invisible borders Windows adds around it to resize it). A closed window is an error. |
| `window.move(window, x, y)` | Puts the window's top left corner there, at the same size. Restored first when maximized or minimized. Nothing. |
| `window.resize(window, width, height)` | Makes the window that large, with its top left corner where it is. Restored first when maximized or minimized. Nothing. |
| `window.list()` | The numbers of the windows you can switch to, as a list, from the one in front: not hidden ones, nor those on another virtual desktop. Go through them with `for`. |
| `window.process(window)` | The name of the window's program, as `process.isrunning` takes it: `"notepad"`, `"chrome"`. `""` when the window is closed. |
| `window.wait(title, seconds)` | Waits for a window with `title` in its title, as `window.find` finds it. Its number once it shows, or `0` when the seconds run out. |
| `window.waitclose(window, seconds)` | Waits for the window to close. `true` once it did, `false` when the seconds run out. |

**Screen pixels** are the same on every monitor, whatever its scaling, and count from the main monitor's top left corner: a monitor on its left has negative `x`. A window on a monitor at 150% that you see 800 wide is `1200` wide. `window.move`, `window.resize`, a closed window, or a program run as administrator (which Windows doesn't let My Tools move) are errors. The waits look every tenth of a second, for at most 600 seconds; `0` looks once.

`window.hide`, `show`, `minimize`, `maximize`, `restore` and `close` give `true`, or `false` when the window is closed or Windows didn't let My Tools change it (a program run as administrator). They wait up to a second for the program to do it, so `window.state` says the new state right after.

With a hotkey, the active window is the program you pressed it in. Read the title at the start of the script: while a dialog of the script is open, the dialog is the active window. From the tray menu, the active window is usually the taskbar, so the title is `""`.

To do something only in one program, check part of the title, since the rest changes with the document:

```mts
if not text.contains(window.title(), "Visual Studio Code")
    tray.notify("This hotkey only works in VS Code")
    script.stop()
end

keyboard.press("Ctrl+Shift+P")
```

To go to a program with a hotkey, starting it when it isn't open:

```mts
# Bound in hotkeys.cfg, e.g. GoToNotepad = Ctrl+Alt+N
if not window.activateoropen("Notepad", "notepad.exe")
    tray.notify("Notepad didn't open")
end
```

### A window in a variable

`process.window`, `window.find` and `window.active` give a window as a number, which stays the same until the window closes. Keep it in a variable and give it to the other `window` functions:

```mts
w = process.window("notepad")
if w == 0
    tray.notify("Notepad isn't running")
    script.stop()
end

window.minimize(w)
window.ontop(w, true)
```

`0` means none: giving `0` to a `window` function is an error, so check for it first. A number can also be kept between runs with `global.set`, so one hotkey can hide a window and another show it again:

```mts
# HideNotepad = Ctrl+Alt+H
w = process.window("notepad")
if w != 0
    window.hide(w)
    global.set("notepad", w)
end
```

```mts
# ShowNotepad = Ctrl+Alt+Shift+H
w = global.get("notepad", 0)
if w != 0
    window.activate(w)
end
```

A hidden window is still found by `process.window` and `window.find` (after those shown), so the second script could also look for it again.

To lay out windows, as two side by side on the main monitor:

```mts
left = process.window("chrome")
right = process.window("code")
if left != 0 and right != 0
    window.move(left, 0, 0)
    window.resize(left, 960, 1040)
    window.move(right, 960, 0)
    window.resize(right, 960, 1040)
end
```

## mouse

| Function | What it does |
|---|---|
| `mouse.position()` | Where the pointer is: `[x, y]`, in screen pixels. |
| `mouse.move(x, y)`<br>`mouse.move(x, y, window)` | Puts the pointer there; with a window's number, `x` and `y` count from the window's top left corner, so a button in it is found wherever the window is. |
| `mouse.click()`<br>`mouse.click(button)`<br>`mouse.click(button, times)` | Clicks where the pointer is: `"left"` (without one), `"right"` or `"middle"`. `mouse.click("left", 2)` is a double click. Up to 10 times. |
| `mouse.down()`<br>`mouse.down(button)` | Presses the button and holds it, as for a drag of your own. |
| `mouse.up()`<br>`mouse.up(button)` | Lets go of it. |
| `mouse.drag(x, y)`<br>`mouse.drag(x, y, window)` | Holds the left button from where the pointer is to there, in small steps, as a hand moves it, then lets go. |
| `mouse.scroll(steps)` | Turns the wheel under the pointer: up for a positive number, down for a negative one, in the wheel's steps (up to 100). |

They act where the pointer is, in whatever window is there, as your own mouse would: move the pointer first. The places are in [screen pixels](#window). To click something in a window:

```mts
w = window.find("Settings")
if w != 0
    window.activate(w)
    mouse.move(120, 300, w)
    mouse.click()
end
```

Where a button is in a window changes with its size, scaling and version: a keyboard shortcut (`keyboard.press`) is often steadier, when the program has one.

## process

| Function | Returns |
|---|---|
| `process.isrunning(name)` | `true` when a program with that name is running, as Task Manager's **Details** tab shows it, with or without `.exe`: `process.isrunning("notepad")`. Case doesn't matter. |
| `process.window(name)` | The number of the program's window you used last; a hidden one when none is shown. `0` when it has none, or isn't running. |
| `process.windows(name)` | The numbers of all the program's windows, as a list: those shown, the one used last first, then those hidden. `[]` when there's none. Take one with `list[1]`, or go through them with `for`. |
| `process.end(name)` | Ends every program running with that name at once, as Task Manager's **End task** does: **without asking to save**. How many ended, `0` when none ran. One that can't be ended (a program run as administrator) is an error. To let it save, use `window.close` instead. |
| `process.waitclose(name, seconds)` | Waits until no program with that name runs. `true` once none does, `false` when the seconds run out (at most 600). |

Store apps, such as Calculator or Settings, show their windows through another program, `ApplicationFrameHost`: find those with `window.find` and a part of their title.

## shell

| Function | What it does |
|---|---|
| `shell.open(target)` | Opens a web address in the browser, or a file or folder in its default program: `shell.open("https://github.com")`, `shell.open("C:\\notes.txt")`. |
| `shell.run(program)`<br>`shell.run(program, arguments)` | Starts a program, without waiting for it: `shell.run("notepad.exe", "C:\\notes.txt")`. |
| `shell.runwait(program)`<br>`shell.runwait(program, arguments)` | Starts a program and waits for it to end, up to 10 minutes; gives its exit code, as a number, often `0` when it went well. Still running after 10 minutes is an error, and it's left running. A program that hands its work to one already open (as some editors do) gives no program to wait for, which is an error. |

In texts, a backslash is written `\\`, so the path `C:\notes.txt` is `"C:\\notes.txt"`.

## tray

| Function | What it does |
|---|---|
| `tray.notify(text)` | Shows a tray notification titled `My Tools: <script name>` for about 5 seconds, and goes on at once. |

## system

| Function | What it does | Returns |
|---|---|---|
| `system.waitidle(cpu, disk, seconds, minutes)` | Waits until the computer is quiet: the processors at or under `cpu` percent and the disks busy at or under `disk` percent, both for `seconds` in a row. Gives up after `minutes`. The app stays responsive while it waits. | `true` once it is quiet, `false` when the minutes ran out first. |

It measures 5 seconds at a time, from the same counters Task Manager shows (CPU, and the disks' active time). A measure over either limit starts the quiet time again, so `seconds` is rounded up to a multiple of 5. All four are whole numbers: `cpu` and `disk` from 1 to 100, `seconds` from 1 to 3600, and `minutes` from 1 to 120, at least as long as `seconds`.

Use it in a startup action, so a program starts only once the computer has settled after you sign in, instead of adding to the rush (see [Start a program once the computer is quiet](08c-script-examples.md#start-a-program-once-the-computer-is-quiet)):

```mts
if system.waitidle(15, 20, 60, 20)
    shell.run("C:\\Program Files\\Slack\\slack.exe")
else
    tray.notify("Slack not started: the computer stayed busy for 20 minutes")
end
```

## file

| Function | Returns |
|---|---|
| `file.read(path)` | The text of a file, read as its byte order mark says, else as UTF-8. A file that isn't there, or can't be read, is an error. |
| `file.write(path, text)` | Writes the text to the file, as UTF-8, replacing what it had. Its folder must exist. Nothing. |
| `file.append(path, text)` | Adds the text at the end of the file, as UTF-8; a file that isn't there yet is created. Its folder must exist. Nothing. |
| `file.exists(path)` | `true` when there's a file at the path, `false` when not (a folder isn't a file). |
| `file.copy(from, to)`<br>`file.copy(from, to, true)` | Copies the file to the path `to`, whose folder must exist. A file already at `to` is an error, unless `true` is given to replace it. Nothing. |
| `file.move(from, to)`<br>`file.move(from, to, true)` | Moves or renames the file, as `file.copy` copies it. Nothing. |
| `file.delete(path)` | Moves the file to the **Recycle Bin**, where you can still get it back. A file that isn't there is an error. Nothing. |
| `file.name(path)` | The path's last part, the file's name: `file.name("C:\\in\\report.pdf")` is `"report.pdf"`; a folder's name for a folder's path. |

A path that isn't full, such as `"notes.txt"` or `"data\\list.txt"`, is in the scripts folder (`config\scripts`). Variables such as `%USERPROFILE%` are filled in: `file.read("%USERPROFILE%\\Documents\\todo.txt")`. Remember to write `\\` for each `\` inside a text.

`file.write` writes the whole file at once: if it fails halfway, the file keeps what it had. To keep a log, add a line at a time with `file.append`; the first one creates the file:

```mts
file.append("log.txt", date.now() + " started\n")
```

To read a file only when it's there:

```mts
if file.exists("todo.txt")
    dialog.message(file.read("todo.txt"))
end
```

## folder

| Function | Returns |
|---|---|
| `folder.exists(path)` | `true` when there's a folder at the path. |
| `folder.create(path)` | Creates the folder, and the folders it's in; nothing when it's there already. Nothing. |
| `folder.files(path)`<br>`folder.files(path, pattern)` | The full paths of the files in the folder, as a list, in name order; with a pattern, only those that match it, as `"*.txt"` or `"report-*.pdf"`. Not those of the folders in it. A folder that isn't there is an error. |
| `folder.folders(path)` | The full paths of the folders in the folder, as a list, in name order. |

Paths are as for [`file`](#file). To move the PDFs of the Downloads folder somewhere else, asking where:

```mts
to = dialog.folder("Move the PDFs to:")
for pdf in folder.files("%USERPROFILE%\\Downloads", "*.pdf")
    file.move(pdf, to + "\\" + file.name(pdf))
end
```

## date

| Function | Returns |
|---|---|
| `date.now()` | The date and time now, as text: `"2026-11-15 14:30:05"`. Texts like this sort and compare in date order. |
| `date.format(format)` | Now, written as `format` says. |
| `date.format(date, format)` | `date`, written as `format` says. `date` is a text such as `date.now()` gives, or `"2026-11-15"`. A text that isn't a date is an error. |

In the format, these letters stand for parts of the date; everything else is written as it is:

| Letters | Part | Example |
|---|---|---|
| `yyyy` | Year | `2026` |
| `MM` / `M` | Month, with or without a leading zero | `03` / `3` |
| `MMM` / `MMMM` | Month name, short or long | `Nov` / `November` |
| `dd` / `d` | Day | `05` / `5` |
| `ddd` / `dddd` | Day of the week, short or long | `Sun` / `Sunday` |
| `HH` / `H` | Hour, 0 to 23 | `14` |
| `hh` / `h`, `tt` | Hour, 1 to 12, and AM or PM | `02`, `PM` |
| `mm` | Minutes | `30` |
| `ss` | Seconds | `05` |

Month and day names are in the language Windows uses. To write one of these letters as itself, put it in single quotes: `date.format("'Week of' d MMMM")`.

```mts
keyboard.type("Notes, " + date.format("dddd d MMMM yyyy") + "\n")
file.write("report " + date.format("yyyy-MM-dd") + ".txt", clipboard.gettext())
```

## script

| Function | What it does |
|---|---|
| `script.wait(milliseconds)` | Pauses, from `0` to `600000` (10 minutes); the app stays responsive. `script.wait(500)` is half a second. |
| `script.stop()` | Ends the script quietly, without an error. |
| `script.argument()`<br>`script.argument(default)` | The value after the shortcut in hotkeys.cfg: `app1` for `PrTemplate = Ctrl+Alt+1, app1`. When there is none (no value on the line, or run from the tray menu), `default`, or `""` without one. |
| `script.include(name)` | Runs another script from the scripts folder, with `.mts` or without, with the same variables: it sees the variables set so far, and its variables are there afterwards. Up to 10 levels deep. See [Reusing scripts](08c-script-examples.md#reusing-scripts). |

## global

A script's variables are gone when it ends. **Global variables** are kept: in the file `config\global.vars`, next to hotkeys.cfg, created the first time a script stores one. Every script can read and change them, and they are still there after My Tools restarts.

| Function | What it does | Returns |
|---|---|---|
| `global.set(name, value)` | Stores a value under a name. A number, text, true/false or list; it replaces the old value. | Nothing |
| `global.get(name)`<br>`global.get(name, default)` | Reads a value. Without a default, a name that was never set is an error. | The value, or `default` |
| `global.has(name)` | Whether the name was set. | `true` or `false` |
| `global.remove(name)` | Removes a name. A name that was never set is not an error. | Nothing |
| `global.names()` | All the names, sorted ignoring case. | A list of texts |

Names are texts and are not case-sensitive: `global.get("Count")` reads what `global.set("count", 1)` stored.

### When a name was never set

`global.get(name)` stops the script with an error when the name was never set, e.g. the first time a script runs. There are two ways to handle that.

**Give a default**, which is returned when the name was never set:

```mts
count = global.get("count", 0)              # 0 the first time
site  = global.get("last site", "")         # "" the first time
items = global.get("items", [])             # an empty list the first time
```

**Check first with `global.has`**, when the first run should do something different:

```mts
if global.has("name")
    name = global.get("name")
else
    name = dialog.ask("What's your name?")
    global.set("name", name)
end
```

There is no automatic `""` for a missing name, on purpose:

- **The default has the type you need.** `global.get("count", 0) + 1` is `1` the first time; with `""` it would be the text `"1"`, and the next run would add to that text.
- **A stored `""` stays different from "never set"**: `global.has` tells them apart.
- **A misspelled name is an error** ("there is no global variable 'cuont'"), not a wrong result later.

When `""` is the value you want, write it: `global.get(name, "")`.

A counter of how often a hotkey was pressed, using a default for the first time:

```mts
count = global.get("search count", 0) + 1
global.set("search count", count)
tray.notify("Searches so far: " + count)
```

Remembering the last answer, so the next time the dialog starts with it:

```mts
site = dialog.ask("Search on site:", global.get("last site", "github.com"))
global.set("last site", site)
shell.open("https://www.google.com/search?q=site:" + text.urlencode(site))
```

### The global.vars file

`global.vars` is a JSON file that you can also open and edit by hand. It is read on every call, so the next `global.get` reads your change. A script that stores the values above leaves:

```json
{
  "last site": "github.com",
  "search count": 3
}
```

Texts go between `"`, lists between `[ ]`, and true/false and numbers as they are. Lines starting with `//` are comments, but they are dropped the next time a script stores a value. If the file can't be read (for example, a missing quote), scripts stop with the error `global.<function>(): <path> is not valid: <reason>`, which says where.

## text

| Function | Returns | Example |
|---|---|---|
| `text.length(text)` | The number of characters. A list is refused; use `list.length`. | `text.length("hello")` is `5` |
| `text.upper(text)` | The text in capitals | `text.upper("ab")` is `"AB"` |
| `text.lower(text)` | The text in small letters | `text.lower("AB")` is `"ab"` |
| `text.trim(text)` | The text without spaces at the start and end | `text.trim("  x ")` is `"x"` |
| `text.replace(text, old, new)` | The text with every `old` replaced by `new` (case-sensitive; `old` can't be empty) | `text.replace("a-b", "-", "+")` is `"a+b"` |
| `text.contains(text, part)` | Whether `part` is in the text, ignoring case | `text.contains("Hello", "hell")` is `true` |
| `text.urlencode(text)` | The text made safe for a web address | `text.urlencode("c# & .net")` is `"c%23%20%26%20.net"` |

## list

| Function | Returns | Example |
|---|---|---|
| `list.length(list)` | The number of items | `list.length(["a", "b"])` is `2` |
| `list.contains(list, item)` | Whether an item equals `item` (`==` rules: texts are case-sensitive) | `list.contains(["a", "b"], "b")` is `true` |
| `list.range(from, to)` | The whole numbers from `from` to `to`, both included; empty when `to` is less. At most 100,000. For a `for` that runs a number of times. | `list.range(1, 3)` is `[1, 2, 3]` |

## Conversions

| Function | Returns | Example |
|---|---|---|
| `number(text)` | The text as a number. Accepts spaces around, a sign and `,` thousands separators. | `number(" 1,250.5 ")` is `1250.5` |
| `text(value)` | Any value as text: numbers without trailing zeros, `true`/`false`, lists as `[a, 1, true]` | `text(2.50)` is `"2.5"` |

---

← Previous: [Script Language](08a-script-language.md) | [Contents](README.md) | Next: [Reusing Scripts and Examples](08c-script-examples.md) →
