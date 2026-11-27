# Hotstrings

A hotstring is a short name you type, in any program, that My Tools replaces with a longer text as soon as you end the word: type `btw` and Space, and it becomes `by the way `. Use it for phrases, addresses, signatures, or anything you type often.

## Setting them up

Hotstrings are in `config\hotstrings.cfg`. Open it from the tray menu, **Edit hotkeys...**, and choose `hotstrings.cfg` on the left. Write one per line, the name, `=`, and the text:

```
btw = by the way
addr = 221B Baker Street, London
mail = someone@example.com
```

A text of several lines goes between `["` and `"]`, on as many lines as it has:

```
sig = ["Best regards,
Ricky"]
```

Lines starting with `#` are comments. The file starts with only comments, so nothing is replaced until you write a line. Changes apply as soon as you save; no restart is needed.

- **Names** are letters and digits only, up to 32: `btw`, `addr1`, `sig`. My Tools follows the keys you press, and other characters, such as `;` or `@`, are on different keys on each keyboard layout, so they can't be told apart.
- **Case** doesn't matter: `btw`, `Btw` and `BTW` all become the text as you wrote it.
- **The text** is typed exactly as written, after the `=` to the end of the line, `#` included. A new line in a text between `["` and `"]` is Enter, and a tab is Tab.

## How it's replaced

Type the name as a word of its own, then **Space**, **Enter** or **Tab**. My Tools erases the name with Backspace and types the text, then the key you ended with: a Space after the text, a new line after Enter, a Tab after Tab. Enter doesn't reach the program before the text, so in a chat window `btw` and Enter sends `by the way`, not `btw`.

It's a word of its own when it starts after Space, Enter, Tab, any punctuation, a click, or a shortcut: `btw` is replaced, `abtw` isn't. Backspace while you type the name is followed: `bx`, Backspace, `tw` is `btw`. A key with Ctrl, Alt or Windows held, a click, or an arrow key starts a new word.

To type the name itself without it being replaced, type it with any other character after it, then erase that character, or end it with another key than Space, Enter or Tab.

## Things to know

- Hotstrings work in any program that takes typing, as `keyboard.type` in scripts does, whatever its keyboard layout. A program run as administrator doesn't take keys from My Tools, as with hotkeys.
- Names are found in what you type, not in what a program or a script types.
- Lines that can't be used are skipped, and a notification says so when the file is saved or the app starts. The config editor underlines each one and says why: a name with other characters than letters and digits, a name without a text, a name already used on another line (the first one counts), or a `["` never closed with `"]`.
- Hotstrings follow the keyboard only while `hotstrings.cfg` has at least one.
- Like hotkeys, they're kept when you [back up your settings](10-files-and-updates.md#back-up-or-restore-settings).

---

← Previous: [JSON Editor](18-json-editor.md) | [Contents](README.md) | Next: [Regex Tester](20-regex-tester.md) →
