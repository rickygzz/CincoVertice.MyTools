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

## Quick reference

**Quick reference**, at the right of the pattern, shows or hides a list beside the matches: the parts of a pattern and what each means. **Basics** has the ones used most (`.`, `\d`, `\w`, `\s`, `[abc]`, `^` and `$`, `*` `+` `?`, `{n,m}`, groups and `a|b`); **More** has their opposites, ranges, word boundaries, escaping, lazy counts, named groups and back references, lookarounds, and `$1`, `${name}` and `$0` for the replacement. Point at a row to read the whole of a description the column cuts.

## Examples

**Examples**, beside **Add**, has ready-made patterns to use as they are or start from; pointing at one says what it matches. Choosing one writes its pattern, its options and, for some, a replacement, and runs it on your text. If the text box is empty, the example's own sample goes in too, so you see it work, including what it doesn't match.

| Submenu | Examples |
|---|---|
| Contacts and the web | email address, web address (URL), phone number |
| Dates, times and numbers | date year first (`2026-11-26`, replaced as `26/11/2026`), date day first (`21/09/2015`), time (`17:30`, `23:59:59`), number, amount of money (`$3,332,000.00`, replaced by the digits), percentage |
| Codes | IP address, GUID, color (`#fff`, `#1E90FF`), HTML tag with its content |
| Text and lines | repeated word (`is is`, replaced by one), text in quotes, `name = value` lines (as the app's config files), spaces at the end of lines, blank lines, a line starting with ERROR or WARNING |

Most of them name their groups, so the matches list shows what each part is (`year`, `amount`, `name`...), and the replacement shows how to use them.

## Building a pattern with Add

You don't need to know the syntax to start: **Add**, beside the pattern, writes a part of one for you. Each line of its menu shows what it writes, and pointing at it says what it matches.

- If part of the pattern is selected, the new part goes **around it**: select `error`, choose **Line containing…**, and the pattern becomes `^.*error.*$`.
- If nothing is selected, the part is written at the caret with a placeholder, as `text`, selected: type over it.

At the top of the menu:

| Choice | Writes | Matches |
|---|---|---|
| Starts with… | `^text` | the text, or a line, starting with it |
| Ends with… | `text$` | the text, or a line, ending with it |
| Is exactly… | `^text$` | a whole text, or line, that is just it |
| Line containing… | `^.*text.*$` | each whole line that has it; ticks **^ and $ at each line** |
| Line not containing… | `^(?!.*text).*$` | each whole line that doesn't; ticks it too |
| Whole word… | `\bword\b` | it, but not inside a longer word |
| Text selected in the sample | the text, escaped | exactly the text you selected on the left: `.`, `(`, `$` and the like get a backslash, so they stand for themselves |

**Starts with**, **Ends with** and **Is exactly** go by the whole text, or by each line once **^ and $ at each line** is ticked.

What you type over a placeholder is a pattern too: in `a.b` the `.` is any character. To match a text exactly as it is, select it in the sample and choose **Text selected in the sample**.

The submenus have the smaller parts:

- **Characters**: a digit `\d`, a letter `\p{L}` (accented ones too), a letter, digit or `_` `\w`, a space `\s`, any character `.`, their opposites, and one of (or none of) a set of characters, as `[abc]` or `[a-z]`.
- **How many times**: one or more `+`, zero or more `*`, optional `?`, exactly `{3}`, between `{2,5}`, at least `{2,}`. These go after what's before the caret, or after the part selected; a selected part longer than one character gets a group around it first, so `ab` becomes `(?:ab)+`, the whole of it repeated, not only the `b`. The number is selected, to type over.
- **Groups and choices**: a group `(…)` (`$1` in the replacement), a named group `(?<name>…)` (`${name}`), and either…or `(?:this|other)`.
- **Before or after**: followed by, not followed by, preceded by, not preceded by. These check what's around a match without making it part of it: `\d+(?= kg)` matches `12` in `12 kg`.
- **Edges**: the start `^` and end `$` of the text (or of each line), and the edge of a word `\b`.

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
