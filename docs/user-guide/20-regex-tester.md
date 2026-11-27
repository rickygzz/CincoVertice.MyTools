# Regex Tester

A window to write a regular expression and see at once what it matches in a text of yours: each match colored, its groups in their own colors, a list of the matches and groups, and the text with a replacement. Open it from the tray menu, **Regex tester**; each time opens another window. It has no hotkey at first: to give it one, add a line to `hotkeys.cfg`, as `OpenRegexTester = Ctrl+Alt+Shift+X` (see [Hotkeys](02-hotkeys.md)).

## The window

- **Pattern**, at the top: the regular expression, as .NET writes them, the same as scripts' `text.match` and `text.regexreplace` take, but with each backslash written once: `\d+`, not `"\\d+"`.
- Under it, three options:
  - **Ignore case**: letters match in either case.
  - **^ and $ at each line**: `^` and `$` match at the start and end of each line, not only of the whole text.
  - **. matches new lines too**: `.` matches any character, a new line included.
- **Text to try it on**, on the left: paste or type a sample. Each match is colored yellow, the next one orange, and so on, so two matches side by side are told apart. Each group has a color of its own over its match: group 1 blue, group 2 green, group 3 pink, group 4 lavender, group 5 teal, then blue again.
- **Matches**, on the right: a row per match, `Match 1`, `Match 2`..., with what it matched and where, then a row per group under it, gray, by its number or its name. A new line in a value shows as `↵` and a tab as `→`. Choose a row to select that part of the text and scroll to it.
- **Replace with**, at the bottom: what each match becomes. `$1` is group 1, `${name}` a named group, `$0` the whole match, and `$$` a `$`. **Result** shows the whole text with every match replaced.
- The status bar says how many matches there are, or why the pattern can't run.

Everything is run again once you pause typing, a quarter of a second after the last key.

## Things to know

- Positions in the list count from 0, as .NET does: `at 5, 3 long` is the 6th to 8th characters.
- Named groups are numbered after the unnamed ones, as .NET does: in `(?<first>\w+) (\w+)`, group 1 is the second word and `first` is group 2.
- A group that took no part in a match, as an optional `(x)?`, shows `(took no part)`.
- At most 1000 matches are shown; the status bar says when there are more.
- A pattern that takes more than a second on the text is stopped, with a message: a part that repeats inside another part that repeats, as `(a+)+`, can try without end on a text that almost matches.
- The text keeps its own undo: **Ctrl+Z** and **Ctrl+Y** undo and redo what you typed, never the colors.
- The pattern, the options, the text and the replacement are kept, in `config\regextester.json`, so the next window starts with them, even after a restart.

---

← Previous: [Hotstrings](19-hotstrings.md) | [Contents](README.md)
