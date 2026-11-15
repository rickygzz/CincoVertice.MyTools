# Compare Files

**Compare files** shows two text files side by side, line by line, with what differs between them colored, and lets you edit either side, seeing the differences change as you type. Use it to see what changed between two versions of a file, a config before and after, or two texts copied from anywhere, and to fix one to match the other.

Open it from the tray menu: **Compare files...**. Each time opens a new window, so several comparisons can be open at once. To open it with a hotkey, bind `CompareFiles` in `hotkeys.cfg` (see [Hotkeys](02-hotkeys.md)).

## Choosing what to compare

Each side shows a file or a text:

| To | Do this |
|---|---|
| Choose a file for a side | **Left file...** (Ctrl+O) or **Right file...**. Pick two files in the dialog to put them on both sides at once, the first on the left. |
| Drop files | Drag a file from Explorer onto the left or right half of the window. Drop two files to compare them. |
| Compare text you copied | Copy it, then **Paste left** or **Paste right**. A side can be a file and the other a pasted text. |
| Type a text | Click an empty side and type, or paste with Ctrl+V. |
| Swap the sides | **Swap**: the left goes to the right, the right to the left. |

The path of each side's file shows above it, and the window's title gives both names.

Files are read as their byte order mark says (UTF-8, UTF-16), else as UTF-8, else as Latin-1 for older files with accents. A file that isn't text, such as a program or an image, isn't compared: a message says so. Files of more than 64 MB aren't read.

## Reading the comparison

The two sides are shown in rows that scroll together, so each line is next to the line it matches:

| Row | Left | Right |
|---|---|---|
| The same line | white | white |
| A changed line | red, with the words that changed darker | green, with the words that changed darker |
| A removed line | red | blank, gray |
| An added line | blank, gray | green |

Each side has its line numbers at the left, as in the file, with `-` before a removed or changed line and `+` before an added or changed one. Tabs are shown as spaces, to the next multiple of 4.

Where lines were removed and others added in their place, they are shown as changed, in pairs: the first removed next to the first added, and so on. The rest are shown removed or added. Inside a changed pair, a word counts as a run of letters, digits and `_`, a run of spaces, or any other single character, so a change of `price` to `cost` marks those words only. Where a changed word is mostly the same, only the characters that differ are marked: `abc` changed to `abcd` marks just the `d`, and `colour` to `color` just the `u` on the left. A word replaced by one quite different, such as `price` by `cost`, is marked whole.

The narrow strip at the right shows the whole text from top to bottom, with each change in green (only added), red (only removed) or yellow (changed). The gray part is what's showing. Click or drag in the strip to go there.

The status bar sums it up, such as "3 changes: 2 lines added, 1 removed, 4 changed", and says which change is the current one, such as "Change 2 of 3". When there are no differences, it says why the two still aren't the very same text, if they aren't: only the line endings differ, or what's ignored does.

Line endings never count: a file with Windows line endings (CRLF) and the same file with Unix ones (LF) are the same, and so are a file that ends with a line break and one that doesn't.

## Going from change to change

**Next change** (F7) and **Previous change** (Shift+F7) go from one change to the next, marking the current one with a blue line above and below it. They go from the caret's line, if it's showing, or else from the top of what's showing, and put the caret at the change. A new comparison starts at its first change.

## Ignoring whitespace and case

- **Ignore whitespace**: lines that differ only in spaces and tabs, anywhere in the line, count as the same, as with `git diff -w`. Inside a changed line, spaces aren't marked.
- **Ignore case**: lines that differ only in upper and lower case count as the same.

Turning either on or off compares again, staying where you are.

## When a file changes

When you come back to the window after a file changed, for example after saving it in another editor, it is read and compared again, staying where you are. If you changed that file here too, you're asked first whether to read it again and lose your changes; if not, you aren't asked again until it changes again. **Reload** (F5) reads both files again yourself, such as for a file on a network drive, asking the same way. Pasted text stays as it was.

## Editing

Both sides are editors. Click a side to put the caret there, and type: the two sides are compared again at every change, so the colors, the strip and the status bar always show the texts as they are now. **F6** moves the caret to the other side, at the same row. The path of a file changed and not saved ends with `*`, as does its name in the title.

| Key | Effect |
|---|---|
| Arrows, Home / End, Page Up / Page Down | Move the caret; with Shift, select. Home goes to the first character that isn't a space, then to the start of the line. |
| Ctrl+Left / Ctrl+Right | A word left / right |
| Ctrl+Home / Ctrl+End | The start / end of the text |
| Ctrl+Up / Ctrl+Down, wheel | Scroll, without moving the caret; Shift+wheel scrolls sideways |
| Enter | A new line, as indented as the one before |
| Backspace / Delete | The character before / after; with Ctrl, the word |
| Ctrl+Z / Ctrl+Y or Ctrl+Shift+Z | Undo / redo, on the side with the caret. Typing a run of characters is one step. |
| Ctrl+X / Ctrl+C / Ctrl+V | Cut / copy / paste |
| Ctrl+A | Select the whole side |
| Double-click | Select a word |
| Esc | Clear the selection |

The right-click menu has the same: Undo, Redo, Cut, Copy, Paste and Select all.

A click on a blank row of a side, where only the other side has a line, puts the caret at the start of that side's next line.

## Saving

**Save** (Ctrl+S) saves the side with the caret to its file, written as it was read: the same encoding, with its byte order mark or without, the same line breaks (those of its first line, for a file that mixed them) and a line break at the end only if it had one. Text that was pasted or typed is saved where you choose.

Closing the window with a file changed and not saved asks whether to save it. Pasted or typed text isn't asked for: save it first if you want to keep it.

## Limits

- **A plain text editor.** No syntax colors, and no search or replace: for those, edit in your own editor, and the comparison follows when you come back to the window.
- **Two files, not folders.** Dropping a folder shows a message.
- **Very long lines.** A changed line with more than 4000 words is marked whole rather than word by word.

---

← Previous: [Mouse Spotlight](14-mouse-spotlight.md) | [Contents](README.md)
