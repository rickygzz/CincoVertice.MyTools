# Script Errors and Limits

## Errors

Scripts are checked only when they run, or while you edit them in the config editor; starting the app does not check them. Before a script runs, My Tools reads the whole file, so a mistake on line 10 stops it before line 1 does anything. Mistakes found while running stop the script at that point.

Either way, a message box with an error icon, titled `My Tools: <script name>`, shows the file, the line and column, and what is wrong, often with a hint:

```
GoogleSearch.mts, Line 3, column 9: Use == to compare; = only assigns a value at the start of a line
```

A file that can't be read shows `Could not read <path>: <reason>`.

Cancelling `dialog.ask()` or `dialog.form()`, or calling `script.stop()`, ends the script without an error. There is no command to cancel a running script from outside: press Cancel in its dialog, or use **Exit** in the tray menu, which ends the whole app.

Some examples:

| Message | Meaning |
|---|---|
| `Unknown variable 'nmae'` | A variable was read before being set, or is misspelled. |
| `Unknown variable 'clipboard.gettext': to call the function, write clipboard.gettext()` | A function was used without `( )`. |
| `Unknown function 'copy': it is now clipboard.settext()` | A name from before the modules. |
| `Unknown function 'clipboard.copy': clipboard has clear, gettext, paste, pasteastext, settext` | A misspelled function of a module. |
| `dialog.message() takes 1 argument, not 0` | A function was given the wrong number of arguments. |
| `The condition must be true or false, not a text` | `if` needs a comparison, e.g. `if answer == "yes"`. |
| `Missing 'end' for the 'if' on line 4` | An `if` has no `end`. |
| `There is no item 4: the list has 3 items, and the first is 1` | A list position is out of range. |
| `shell.open(): The system cannot find the file specified.` | Windows could not do what the function asked. |

### All syntax messages

- `Unexpected character '<c>'`, with hints: `: use 'not' instead of '!'`, `: use 'and' instead of '&&'`, `: use 'or' instead of '||'`, `: texts use double quotes, "like this"`
- `The text is not closed: add a " at its end`
- `Unknown escape \<c>: use \" \\ \n or \t`
- `The number <n> is too large`
- `A variable name can't contain '.': '<name>' looks like a function`
- `A line must assign a value (name = ...) or call a function (name(...))`
- `'<keyword>' without an 'if'`
- `Missing 'end' for the 'if' on line N`
- `Compare two values at a time: use 'and', as in a < b and b < c`
- `A list's items can't be changed: build a new list, e.g. names = [first, second]`
- `Use == to compare; = only assigns a value at the start of a line`
- `Missing ']'`, `Missing ')'`
- `Expected a value before the end of the line`, `Expected a value, not '<t>'`, `Expected the end of the line, not '<t>'`

### Run-time messages

- `Unknown function '<f>'`, with the hint `: it is now <new>`, `: <module> has <functions>` or `: there is no module '<m>'`
- `<f>() takes <no arguments|1 argument|N arguments|A to B arguments>, not M`
- `Unknown variable '<v>'`, with the hint `: to call the function, write <v>()`
- `<f>() returns no value to store` or `... to use`
- `The condition must be true or false, not <type>`
- `There is no item N: the list has M items, and the first is 1`
- `Division by zero`
- `<function>(): <message>` for problems inside a function, for example:
  - `argument N must be a number, not <type>`
  - `the time must be from 0 to 600000 milliseconds`
  - `the CPU percent must be a whole number from 1 to 100, not <n>`, and likewise for the disk percent, the quiet seconds and the minutes
  - `the quiet seconds (<s>) are longer than the minutes (<m>) to wait`
  - `Could not open the performance counters (error 0x<code>).`, when Windows' counters are turned off
  - `the text to replace can't be empty`
  - `there is no global variable '<name>'; global.get("<name>", value) gives a value then`
  - `'<name>' must be the name of a script in the scripts folder`
  - `There is no script <file> in <folder>`
  - `scripts include each other more than 10 levels deep: does <name> include itself?`

## Limits

The language is kept small on purpose. It does not have:

- Loops (`while`, `for`) or your own functions.
- Dates and times, math functions such as `sqrt` or `round`, or reading and writing files.
- Changing a list item in place.

Numbers are decimals with up to 28 to 29 significant digits; a result too large for that is an error.

---

← Previous: [Reusing Scripts and Examples](08c-script-examples.md) | [Contents](README.md) | Next: [Code Review: Check and Fix a Folder](09-code-review.md) →
