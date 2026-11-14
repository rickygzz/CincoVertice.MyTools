# Reusing Scripts and Examples

## Reusing scripts

There are two ways to avoid copying a script: pass a **value from the hotkey** when hotkeys differ only in a setting, and **include** a shared piece when different scripts do the same steps.

### One script, several hotkeys: script.argument()

A pull request text for two apps, each with its own build number. One script, and one hotkeys.cfg line per app:

```
PrTemplate = Ctrl+Alt+1, app1
PrTemplate = Ctrl+Alt+2, app2
```

```mts
# PrTemplate.mts: app is "app1" or "app2", from the hotkey; "app1" from the tray menu
app = script.argument("app1")
build_name = app + " build"

answers = dialog.form("Pull request for " + app,
    ["Jira task:", "Build:"],
    [global.get("jira task", "DEV-700"), global.get(build_name, "1")])

# Remembered for next time: the Jira task for both apps, the build per app
global.set("jira task", answers[1])
global.set(build_name, answers[2])

pr = """
    PR $jira for $app, build $build
    B
    C
    """
pr = text.replace(pr, "$jira", answers[1])
pr = text.replace(pr, "$app", app)
pr = text.replace(pr, "$build", answers[2])
clipboard.paste(pr)
```

Each line in hotkeys.cfg needs its own shortcut, and the same script and value can't be on two lines. A built-in action (such as `ShowCalculator`) takes no value: a line with one is listed under **Hotkey problems**.

### Shared steps: script.include()

`script.include("name")` runs another script in the middle of this one, with the same variables. Put the shared steps in a file whose name starts with `_`, so it isn't in the menu, and set what it needs before including it:

```mts
# _PrCommon.mts: needs the variable app
answers = dialog.form("Pull request for " + app, ["Jira task:"], [global.get("jira task", "DEV-700")])
global.set("jira task", answers[1])
pr = "PR " + answers[1] + " for " + app
```

```mts
# App1Pr.mts
app = "app1"
script.include("_PrCommon")
clipboard.paste(pr)
```

- The included script sees the variables set before `script.include`, and what it sets is there afterwards (`pr` above).
- `script.stop()`, or Cancel in a dialog, inside the included script ends the whole script.
- An included script can include others, up to 10 levels deep, but a script can't include itself, directly or through others.
- The included script is read when `script.include` runs, so an error in it is found then, not before the script starts. It is reported at the `script.include` line, with the place in the included script: `script.include(): in _PrCommon, line 2, column 45: Unknown variable 'app'` when `app` was not set.

The two work together: `PrTemplate.mts` could read `app = script.argument("app1")` and then include `_PrCommon`.

## Examples

### Search the web with one dialog

```mts
answers = dialog.form("Search Google",
    ["Search for:", "Only on site:", "Language:"],
    [clipboard.gettext(), "", "en"])

query = answers[1]
site = answers[2]
lang = answers[3]

if text.trim(query) == ""
    script.stop()
end

if site != ""
    query = query + " site:" + site
end

shell.open("https://www.google.com/search?hl=" + lang + "&q=" + text.urlencode(query))
```

### Type a ticket template

Asks for a ticket number and a type, chosen from a drop-down, then types a template.

```mts
answers = dialog.form("New ticket", ["Ticket number:", "Type:"], ["", [["Bug", "Feature", "Task"], "Task"]])
number = answers[1]
kind = answers[2]

if number == ""
    script.stop()
end

keyboard.type("[" + kind + " #" + number + "] Summary:" + "\n" + "Steps:\n\t1. ")
```

With number `7` and type `Task`, it types `[Task #7] Summary:`, Enter, `Steps:`, Enter, Tab, `1. `.

### Quote the clipboard

Wraps the clipboard text in quotes, pastes it and shows how long it is.

```mts
original = clipboard.gettext()

if text.length(original) == 0
    tray.notify("The clipboard has no text")
    script.stop()
end

wrapped = "\"" + text.trim(original) + "\""
clipboard.paste(wrapped)
tray.notify("Pasted " + text.length(wrapped) + " characters")
```

With `clip text` on the clipboard, it pastes `"clip text"` and shows `Pasted 11 characters`.

### Open a site given by the hotkey

Opens the web site in the hotkey's value and counts how many times it ran.

```mts
site = script.argument("example.com")
shell.open("https://" + site)
count = global.get("opened", 0) + 1
global.set("opened", count)
dialog.message("Opened " + site + " (" + count + " times so far)")
```

Bind it with two lines in `hotkeys.cfg`:

```
OpenSite = Ctrl+Alt+1, example.com
OpenSite = Ctrl+Alt+2, wikipedia.org
```

Pressing Ctrl+Alt+2 opens `https://wikipedia.org`. Run from the tray **Scripts** menu, or from a line without a value, the script uses its default, `example.com`.

### Paste the clipboard in capitals

```mts
copied = clipboard.gettext()

if copied == ""
    tray.notify("The clipboard has no text")
    script.stop()
end

keyboard.type(text.upper(copied))
```

### Search for the selected text

```mts
# Emptied first, so an old clipboard text isn't searched when nothing is selected
clipboard.clear()
keyboard.press("Ctrl+C")
script.wait(200)
selected = clipboard.gettext()

if selected == ""
    tray.notify("Select some text first")
else
    shell.open("https://www.google.com/search?q=" + text.urlencode(selected))
end
```

### Paste and send

```mts
clipboard.settext("Thanks, I'll take a look today.")
keyboard.press("Ctrl+V")
keyboard.press("Enter")
```

### Open your morning pages

```mts
shell.open("https://mail.google.com")
shell.open("https://calendar.google.com")
shell.open("https://github.com/notifications")
tray.notify("Good morning!")
```

### Ask before starting a program

```mts
if dialog.confirm("Open today's notes?")
    shell.run("notepad.exe", "C:\\notes\\today.txt")
end
```

### Start a program once the computer is quiet

Right after you sign in, Windows and the programs that start with it keep the processors and the disks busy. A program you start then adds to the rush, and everything starts slower. This script waits until the computer has been quiet for a minute (processors at most 15% busy, disks at most 20%) and then starts the program. If that hasn't happened in 20 minutes, it leaves the program alone and says so.

Save it as `config\scripts\StartLater.mts`:

```mts
# The program comes from startup.cfg (Run = StartLater, <program>), or Notepad from the tray menu
program = script.argument("notepad.exe")

if system.waitidle(15, 20, 60, 20)
    shell.run(program)
else
    tray.notify("Not started, the computer stayed busy: " + program)
end
```

Then have it run when you sign in: in the config editor, open `startup.cfg` and add a line:

```
Run = StartLater, C:\Program Files\Slack\slack.exe
```

Startup actions run only when Windows starts My Tools, so **Start with Windows** in the tray menu must be on; starting My Tools yourself does not run them. See [Startup actions](01-getting-started.md#startup-actions).

### Type a signature

```mts
name = dialog.ask("Sign as:", "Ricky")
keyboard.type("Best regards,\n" + name)
```

### Clean up copied text

```mts
copied = text.trim(clipboard.gettext())
copied = text.replace(copied, "\n", " ")
clipboard.settext(copied)
tray.notify("Copied: " + copied)
```

---

← Previous: [Script Functions](08b-script-functions.md) | [Contents](README.md) | Next: [Script Errors and Limits](08d-script-errors.md) →
