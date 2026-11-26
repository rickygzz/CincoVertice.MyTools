# JSON Editor

An editor for JSON files: colors, line numbers, sections that collapse, the path of any value, and a search that takes text or JMESPath expressions, with suggestions as you type.

Open it from the tray menu's **JSON editor**, or with **Ctrl+Alt+J**. Each time opens another window. Open a file with **Open...** (Ctrl+O), by dropping it on the window, or paste JSON into a new one.

## The text

- **Colors:** names are blue, strings red, numbers green, `true`, `false` and `null` blue, comments green, and anything JSON doesn't allow is red.
- **Line numbers** are in the gutter on the left.
- **Errors:** the first thing wrong is underlined with a red wave, and the status bar says what and where: `Line 4, character 5: Expected , or ], not {`. Comments (`//` and `/* */`) and a comma before a closing bracket are allowed, as in JSON with comments.
- **Editing** works as in any editor: select with the mouse or Shift, Ctrl+Z and Ctrl+Y undo and redo, Ctrl+X, C and V cut, copy and paste. **Enter** keeps the indentation, one step more after `{` or `[`; **Tab** adds two spaces.

## Collapsing sections

Each object or array over several lines has a marker in the gutter beside its first line: ▾ when it's open, ▸ when it's collapsed. Click it to collapse the section to its first line, which then shows how much it holds: `{ 3 keys }`, `[ 12 items ]`. Click the marker or that summary to open it again; moving the caret into it, as a search does, opens it too.

**Collapse all** on the toolbar collapses every section but the outermost one, to see a document's shape at a glance; **Expand all** opens them all.

## The path of a value

The status bar shows the JMESPath of the value the caret is in: `people[1].address."zip code"`. Right-click a value, or its name, for **Copy JMESPath** with its path, and **Copy value**, the value as JSON on one line. A name that isn't letters, digits and `_` is quoted, as JMESPath writes it.

## Searching

Choose **Text** or **JMESPath** beside the search box, or press **Ctrl+F** for text and **Ctrl+J** for JMESPath.

- **Text** finds each place the text is, ignoring case. As you type, the list under the box offers the names and values in the document that hold it.
- **JMESPath** runs the expression on the document. The values it gives are marked where they are in the text, even those a projection gathers from many places, and the result, as JSON, shows under the text. An expression that can't run says why, and at which column: `Write == to compare (column 14)`.

**Enter** or **F3** goes to the next match, **Shift+Enter** or **Shift+F3** to the one before; the box says which: `2 of 5`.

**Suggestions for JMESPath** come from the document itself: after `people[0].` the list offers the names that object has; after `people[*].`, those of the people; in a filter, `people[?a`, those of the elements filtered; after `|`, those of what the left side gives. Where a value can start, the functions are offered too. **Up** and **Down** go through the list, **Tab** or **Enter** writes the one chosen, **Esc** closes it.

### JMESPath in short

| Expression | Gives |
|---|---|
| `people[0].name` | The first person's name |
| `people[-1]` | The last person |
| `people[1:3]` | The second and third people; `[::-1]` reverses |
| `people[*].name` | Every person's name |
| `people[?age > \`25\`].name` | The names of those older than 25; `&&`, `\|\|`, `!` combine conditions |
| `people[?name == 'Ann']` | Strings in single quotes; numbers and other JSON in backticks |
| `reservations[].instances[]` | Arrays of arrays flattened into one |
| `address.*` | Every value of an object |
| `people[*].{n: name, a: age}` | New objects, and `[name, age]` new arrays |
| `people[*].age \| max(@)` | `\|` runs the right side on what the left gives; `@` is the value at hand |
| `sort_by(people, &age)[*].name` | `&` passes an expression to `sort_by`, `max_by`, `min_by` and `map` |

The functions are JMESPath's: `abs`, `avg`, `ceil`, `contains`, `ends_with`, `floor`, `join`, `keys`, `length`, `map`, `max`, `max_by`, `merge`, `min`, `min_by`, `not_null`, `reverse`, `sort`, `sort_by`, `starts_with`, `sum`, `to_array`, `to_number`, `to_string`, `type` and `values`.

## Formatting and files

- **Format** (Shift+Alt+F) lays the JSON out on lines, indented by two spaces; **Minify** puts it on one line. Both need valid JSON, leave comments out, and are one step for Undo.
- **Save** (Ctrl+S) writes the file with the encoding and line breaks it had; **Save as...** (Ctrl+Shift+S) under another name. A new document is saved as UTF-8 with Windows line breaks.
- The title shows `*` while there are changes not saved, and closing the window, opening another file or **New** asks first.

## Keys

| Key | Does |
|---|---|
| Ctrl+N / Ctrl+O / Ctrl+S / Ctrl+Shift+S | New, open, save, save as |
| Ctrl+F / Ctrl+J | Search text / search JMESPath |
| Enter, F3 / Shift+Enter, Shift+F3 | Next / previous match |
| Shift+Alt+F | Format |
| F1 | This page |

---

← Previous: [Text Transforms](17-text-transforms.md) | [Contents](README.md)
