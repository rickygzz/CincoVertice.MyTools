# Text Transforms

Select some text in any program, press **Ctrl+Alt+X**, and choose what to do with it from the menu that opens at the mouse pointer. The result replaces the selection.

| Menu item | Letter | What it does |
|---|---|---|
| **UPPER CASE** | U | `Hello world` becomes `HELLO WORLD` |
| **lower case** | L | `Hello World` becomes `hello world` |
| **Title Case** | T | `the QUICK brown fox` becomes `The Quick Brown Fox` |
| **Trim lines** | R | Takes the spaces and tabs off both ends of each line, and the blank lines before the first line and after the last |
| **Sort lines** | S | Puts the lines in alphabetical order, ignoring case |
| **Remove duplicate lines** | D | Keeps the first of each line that appears more than once, in its place; `a` and `A` are different lines |
| **Base64 encode** | B | The text's UTF-8 bytes in base64: `hello` becomes `aGVsbG8=` |
| **Base64 decode** | A | Base64 back to text; it also reads base64 without its `=` at the end, with line breaks, and in the URL-safe alphabet |
| **URL encode** | E | Escapes what a URL can't hold: `a b&c` becomes `a%20b%26c` |
| **URL decode** | C | `a%20b%26c` and `a+b` become `a b&c` and `a b` |
| **JSON pretty-print** | J | Lays JSON out on lines, indented by two spaces |
| **JSON minify** | M | JSON on one line, without spaces |

Choose with the mouse, with the underlined letter, or with the arrow keys and **Enter**. **Esc**, or a click anywhere else, closes the menu and changes nothing.

## How it works

The hotkey copies the selection as **Ctrl+C** would, and pastes the result as **Ctrl+V** would, so it works in any program that copies and pastes text: editors, browsers, chats, Office. Your clipboard is put back as it was afterwards, so whatever you had copied is still there.

- Line endings stay as they were: Windows' (`\r\n`) or Unix's (`\n`). So does a line break at the end of the selection.
- Case changes follow your Windows language settings.
- JSON may have comments and a comma after the last item; pretty-printing and minifying leave them out. Accented letters stay as themselves, not as `é` escapes.
- If the text isn't what the transform reads, nothing is pasted and a notification says why: `That isn't JSON at line 3, character 12.`, `That isn't base64.`
- With nothing selected, a notification says so. Some editors copy the whole line when nothing is selected; then that line is what gets transformed.

To use another hotkey, change `TransformText` in `hotkeys.cfg` (see [Hotkeys](02-hotkeys.md)). Scripts can do more with text (see [Scripts](08b-script-functions.md)), and the calculator encodes and hashes too (see [Calculator](03-calculator.md#what-you-can-type)).

---

← Previous: [Try MyTools: Eight Short Tutorials](16-tutorials.md) | [Contents](README.md)
