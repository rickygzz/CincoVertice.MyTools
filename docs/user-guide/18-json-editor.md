# JSON Editor

An editor for JSON files: colors, line numbers, sections that collapse, a tree of the values, the path of any value, and a search that takes text or JMESPath expressions, with suggestions as you type. It also checks a JSON Schema, sorts keys, repairs almost-JSON, compares, and converts to YAML and CSV.

Open it from the tray menu's **JSON editor**, or with **Ctrl+Alt+J**. Each time opens another window. Open a file with **Open...** (Ctrl+O), by dropping it on the window, or paste JSON into a new one.

## The text

- **Colors:** names are blue, strings red, numbers green, `true`, `false` and `null` blue, comments green, and anything JSON doesn't allow is red.
- **Line numbers** are in the gutter on the left.
- **Errors:** the first thing wrong is underlined with a red wave, and the status bar says what and where: `Line 4, character 5: Expected , or ], not {`. Comments (`//` and `/* */`) and a comma before a closing bracket are allowed, as in JSON with comments.
- **Editing** works as in any editor: select with the mouse or Shift, Ctrl+Z and Ctrl+Y undo and redo, Ctrl+X, C and V cut, copy and paste. **Enter** keeps the indentation, one step more after `{` or `[`; **Tab** adds two spaces.
- **Brackets:** with the caret beside a bracket, it and its pair are marked with a box. **Ctrl+]** jumps from one to the other. Brackets in strings and comments don't count.
- **Names:** **Ctrl+Space** in an object lists names to add. They come from the schema, if one is checked, and from the objects in the same place elsewhere: in an array of people, the names the other people have. Names the object already has aren't listed, and those starting as the word you're typing come first. Choosing one writes `"name": `.
- **Pointing:** rest the pointer on a value to see its path and what it is (`people[3].name`, `string, 5 characters`), and anything the schema finds wrong with it.
- **Large files:** above about 500 KB, the text is read on another thread after each change, so typing never waits. Until then, the status bar says **Reading…** and new text shows without colors.

## Collapsing sections

Each object or array over several lines has a marker in the gutter beside its first line: ▾ when it's open, ▸ when it's collapsed. Click it to collapse the section to its first line, which then shows how much it holds: `{ 3 keys }`, `[ 12 items ]`. Click the marker or that summary to open it again; moving the caret into it, as a search does, opens it too.

Right-click inside a section for **Collapse section**, which collapses the innermost one you clicked in. On the first line of a collapsed one, it's **Expand section**.

From the keyboard:
- **Ctrl+−** collapses the section the caret is in. Press it again to collapse the one around it, and so on outward.
- **Ctrl++** opens the collapsed section the caret is on.

**Collapse all** (Ctrl+Shift+−) collapses every section but the outermost one, to see a document's shape at a glance; **Expand all** (Ctrl+Shift++) opens them all. Both are on the toolbar and the right-click menu. The number pad's − and + work too.

## The tree

**Tree** on the toolbar (Ctrl+T) shows the document as a tree beside the text: each value with its name or index, and how much an object or array holds, `people [12]`. It follows the caret, opening the sections down to the value it's in. Click a value in the tree to go to it in the text. An array with more than 1,000 values shows the first 1,000. Whether the tree is shown is kept in `config\jsoneditor.json`.

## The path of a value

The status bar shows the steps to the value the caret is in: `people › [1] › address › "zip code"`. Click a step to go to that value. **Go to path...** (Ctrl+G, or the right-click menu) asks for a JMESPath, offering the one at the caret, and goes to the first value it gives.

Right-click a value, or its name, for:

- **Copy JMESPath**: its path. A name that isn't letters, digits and `_` is quoted, as JMESPath writes it.
- **Copy value**: the value as JSON on one line.
- **Copy name and value**: the member as JSON writes it, `"name": "Ann"`. An item of an array has no name, so this is greyed out there.

To copy several at once, select them, for example a few lines, and right-click in the selection. The menu then says **Copy values (3)** and **Copy names and values (3)**. It copies each value the selection wholly holds, one per line with a comma between them, so they paste into an array or an object. A whole object selected counts as one value; a part of one string, as the value clicked.

## Searching

Choose **Text** or **JMESPath** beside the search box, or press **Ctrl+F** for text and **Ctrl+J** for JMESPath.

- **Text** finds each place the text is, ignoring case. As you type, the list under the box offers the names and values in the document that hold it: **Up** and **Down** go through it, **Tab** or **Enter** searches for the one chosen, **Esc** closes it.
- **JMESPath** runs the expression on the document. The values it gives are marked where they are in the text, even those a projection gathers from many places, and the result, as JSON, shows under the text. An expression that can't run says why, and at which column: `Write == to compare (column 14)`.

**Enter** (while no suggestions show) or **F3** goes to the next match, **Shift+Enter** or **Shift+F3** to the one before; the box says which: `2 of 5`.

Right-click a JMESPath result for **Copy as JSON**, or **Copy as lines**: an array's values one per line, strings without quotes, ready for a spreadsheet or a list.

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
- **Sort keys** sorts the keys of every object, ignoring case, and formats the document. To sort only one object, right-click in it and choose **Sort keys here**: the rest stays as written. Both are one step for Undo and leave out the comments in what they sort.
- **Repair** turns almost-JSON into JSON, as logs, JavaScript and Python print it:
  - strings in single quotes, and names without quotes;
  - a comma too many, or one missing between two values;
  - comments;
  - `True`, `False`, `None`, `undefined`, `NaN` and `Infinity`.

  A message says what it changed, and whether something is still wrong. Undo takes it back.

## Comparing

**Compare** → **With the clipboard** or **With a file...** opens Compare files with the document on the left and the other on the right. Both are formatted the same way first when they're valid JSON, so only what differs shows, not the spacing.

## YAML and CSV

**Convert** works on the JMESPath result when the search shows one, and on the document otherwise:

- **Copy as YAML** and **Save as YAML...** write YAML in blocks. Strings YAML would read as something else are quoted: `"yes"`, `"01234"`, `"a: b"`.
- **Copy as CSV** and **Save as CSV...** need an array. An array of objects becomes a table: a column for each name, and an object or array inside goes in its cell as JSON. Other arrays become one column. The file is UTF-8 with a BOM, so Excel shows accents right.
- **Open CSV as JSON...** and **Paste CSV as JSON** make a new document from CSV: an array with an object per row, named by the first row. Commas, semicolons or tabs can separate the values. Numbers, `true`, `false` and `null` become those values; anything else is a string.

## Checking a schema

When the document has `"$schema"` naming a file, as `"$schema": "person.schema.json"`, the document is checked against it. A relative name is looked for beside the document. A schema on the web (`https://...`) isn't downloaded: the status bar says it isn't checked. Choose **Schema** → **Choose a schema...** to check against any file, or **No schema** to stop.

Values the schema finds wrong get an orange wave under them, and the status bar says how many: `Schema person.schema.json: 3 problems`. Click that, or press **F8**, to go to the next one and read what's wrong; **Shift+F8** goes to the one before. Pointing at a value also says it.

The schema is checked only while the JSON is valid. These keywords are understood: `type`, `enum`, `const`, `minLength`, `maxLength`, `pattern`, `minimum`, `maximum`, `exclusiveMinimum`, `exclusiveMaximum`, `multipleOf`, `properties`, `required`, `additionalProperties`, `patternProperties`, `minProperties`, `maxProperties`, `items`, `prefixItems`, `additionalItems`, `minItems`, `maxItems`, `uniqueItems`, `contains`, `allOf`, `anyOf`, `oneOf`, `not`, `if`/`then`/`else`, and `$ref` to a place in the same schema (`#/definitions/...`, `#/$defs/...`). Others, such as `format`, are not checked.

## Dark or light

The editor opens in Windows' mode for apps, dark or light. The **Dark** or **Light** button at the right of the toolbar switches to the other one. Your choice is kept in `config\jsoneditor.json`, so windows opened later use it too.

## Keys

| Key | Does |
|---|---|
| Ctrl+N / Ctrl+O / Ctrl+S / Ctrl+Shift+S | New, open, save, save as |
| Ctrl+F / Ctrl+J | Search text / search JMESPath |
| Enter, F3 / Shift+Enter, Shift+F3 | Next / previous match |
| Shift+Alt+F | Format |
| Ctrl+− / Ctrl++ | Collapse the section the caret is in / expand the one it's on |
| Ctrl+Shift+− / Ctrl+Shift++ | Collapse all / expand all |
| Ctrl+T | Show or hide the tree |
| Ctrl+G | Go to path |
| Ctrl+] | To the bracket's pair |
| Ctrl+Space | Names to add in the object |
| F8 / Shift+F8 | Next / previous schema problem |
| F1 | This page |

---

← Previous: [Text Transforms](17-text-transforms.md) | [Contents](README.md)
