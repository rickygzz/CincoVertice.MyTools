# Code Review: Check and Fix a Folder

This tool checks C# and JavaScript source files against a fixed set of formatting rules and can rewrite the files to follow them. The rules are built in; you can choose which fixers a fix runs (see [Choosing the fixers](#choosing-the-fixers)). It works on files in the folder you choose and all its subfolders: `.cs` files with the C# rules, and `.js`, `.mjs` and `.cjs` files with the [JavaScript rules](#javascript). It skips the folders `bin`, `obj`, `.git`, `.vs` and `node_modules`, and files ending `.Designer.cs`, `.g.cs`, `.g.i.cs` and `.min.js`.

Use the tray items **Check folder for standard...** and **Fix folder for standard > Fix a folder...**. You can also give them hotkeys with the actions `CheckFolder` and `FixFolder`. Do not use **File > Code review** in the main window; it opens an empty window.

## Check a folder

1. Choose **Check folder for standard...**.
2. A folder picker titled "Select the folder to check for the code standard" opens. It remembers your last folder while the app runs.
3. A new window titled "Code standard - <folder>" opens and shows "Code standard check", the folder, then "Checking...".
4. The result lists each file with issues as `<relative path> (<count>)`, with one line per issue: `<line>  <code>  <message>`. A summary ends the list: `No C# or JavaScript files found.`, `No issues found in N file(s).` or `N issue(s) in M of K file(s).`

The window stays responsive while it works. There is no cancel button and no progress bar. You can select text and copy it with Ctrl+C. The output box is not read-only, so you can also type over the results. Right-click offers one entry, "Clear Text". There is no save or export command. A missing folder shows `Folder not found: <path>`.

## Fix a folder

**Fix rewrites your files in place, and there is no backup, log or undo.** Commit or back up your work first.

1. In the tray menu, choose **Fix folder for standard > Fix a folder...**.
2. A folder picker titled "Select the folder to fix for the code standard" opens.
3. A warning box titled "Fix folder for code standard" asks for confirmation. **No** is the default button. It reads:

```
This rewrites the C# and JavaScript files in
<folder>
in place.

Commit or back up your changes first, so you can review the fixes and undo them.

Fix the folder now?
```

When some fixers are off, or a line ending or encoding is chosen, the box also says so, for example:

```
These fixers are off: Indentation, Trailing whitespace.
Every line ends in CRLF (Windows).
Every file is saved as UTF-8 with BOM.
```

4. If you click **Yes**, a new window titled "Code standard fix - <folder>" opens. It shows "Code standard fix", the folder and "Fixing...". Then it lists one `Fixed  <relative path>` line for each changed file and `Skipped  <relative path>: <reason>` for each skipped file. A summary follows: `Fixed X of Y file(s).`, with ` Skipped N.` added when some files were skipped, or `No C# or JavaScript files found.`
5. Last, under "Issues left (not fixable automatically)", it lists the issues that remain.

A file is rewritten only if its content changes, or its line endings or encoding are to change (see [Line endings and encoding](#line-endings-and-encoding)). Unless you choose otherwise, it keeps its own encoding, byte-order mark and line endings. A file without a byte-order mark must be valid UTF-8, otherwise it is skipped with `Not valid UTF-8; add a BOM or convert the file to UTF-8.` Text inside string and character literals is protected from the fixers.

Two things are changed even though they are not listed as rules: typographic punctuation (curly quotes, dashes, the minus sign, the ellipsis) becomes plain ASCII outside string and character literals, and every file ends with exactly one line break.

## Choosing the fixers

The **Fix folder for standard** submenu lists every fixer by name, such as "Indentation", "Braces on their own lines" or "Trailing whitespace". A tick means the fix runs it. Click one to turn it off or on; the menu stays open, so you can change several. **Turn all on** and **Turn all off** change them all at once. Hover over a fixer to see what it does and which languages it fixes: C#, JavaScript or both.

Your choice is saved in `config\fixers.json` and applies to every fix after it, including one started with the `FixFolder` hotkey, until you change it. A fixer added in a later version of My Tools starts on. If every fixer is off, **Fix a folder...** says so and does nothing.

A fixer that is off leaves its issues in place, so they appear under "Issues left" after the fix.

## Line endings and encoding

The same submenu has two more choices, saved in `config\fixers.json` too:

| Submenu | Choices | What a fix does |
|---|---|---|
| **Line endings** | **Keep as they are** (the default), **CRLF (Windows)**, **LF (Linux, macOS)**, **CR (classic Mac OS)** | Gives every line of every file the line ending chosen, whatever mix the file had, including the lines inside multi-line strings. |
| **Encoding** | **Keep as it is** (the default), **UTF-8**, **UTF-8 with BOM** | Saves every file in the encoding chosen: UTF-8 with or without the byte-order mark (EF BB BF) that Visual Studio adds. A UTF-16 file with a byte-order mark becomes UTF-8 too. |

Each submenu shows its choice in its own name, for example **Line endings: CRLF (Windows)**.

To only convert line endings or encodings, choose them, then **Turn all off**: the fix changes nothing else. A file without a byte-order mark that isn't valid UTF-8 (for example one saved as Windows-1252) is still skipped, since its encoding can't be known for sure.

## The rules

| Code | Message | Fixed by the tool? |
|---|---|---|
| CH0001 | `File not found.` | not applicable |
| CH0002 | `Error reading file: {message}` | not applicable |
| CH0010 | `Line contains whitespaces only.` | yes |
| CH0011 | `Line ends with whitespace.` | yes |
| CH0012 | `Line is tab indented.` | yes (4 spaces per level) |
| CH0013 | `Line should have four spaces per indentation level.` | yes |
| CH0014 | `Additional indentation should be four spaces only.` | yes |
| CH0016 | `No lines should be greater than 120 characters.` | no, reported only |
| CH0017 | `Line starts with forbidden token: '{token}'` | yes (the token moves to the end of the previous line; not after a line ending in `;` or `{`) |
| CH0018 | `Line ends with forbidden token: '{token}'` | yes for most (the operator moves to the start of the next line); a lone `!` is reported only |
| CH0019 | `Line contains forbidden token: '{token}'` | no, reported only (for example `this.`) |
| CH0020 | `Character immediately following /// is not a space.` | yes |
| CH0021 | `Character immediately following // is not a space.` | yes |
| CH0022 | `Keyword '{keyword}' lacks space before parenthesis.` | yes (for, foreach, if, do, while) |
| CH0023 | `Keyword '{keyword}' has spaces before parenthesis.` | no, reported only (the keyword `new`) |
| CH0024 | `Keyword '{keyword}' should have only one space before parenthesis.` | yes |
| CH0025 | `Line contains invisible character {character} at column {column}.` | yes (removed) |
| CH0026 | `Line contains Unicode space {character} at column {column}; use a normal space.` | yes |
| CH0027 | `Line is a consecutive blank line.` | yes |
| CH0028 | `Blank line before closing brace.` | yes |
| CH0029 | `Namespace should be file-scoped: namespace Name;` | yes, when the braces are on their own lines |
| CH0030 | `Empty XML comments.` | yes |
| CH0031 | `if / else / loop body must be in braces.` | yes |
| CH0032 | `Line contains consecutive spaces.` | yes |
| CH0033 | `Braces need a space before and inside them: get { return x; }` | yes |
| CH0034 | `Block braces go on their own lines: "if (x) {" and "} else" are not allowed.` | yes |
| CH0035 | `No space after ( or before ): Read(a), not Read( a ).` | yes |
| CH0036 | `Line should be indented {spaces} spaces, by the brackets around it.` | yes (JavaScript only) |

## JavaScript

JavaScript files, such as the scripts of a Bruno collection, get the rules that make sense in any language: CH0010, CH0011, CH0016, CH0020, CH0021, CH0025 to CH0028, CH0032 and CH0036, plus the typographic punctuation and the final line break. The C# rules about braces, namespaces, XML comments, keywords and forbidden tokens are not used, and braces stay where they are: in JavaScript a `{` goes on the line of its statement, and moving the `{` after `return` to its own line would change what the code does.

Text inside `'...'`, `"..."` and `` `...` `` strings (also over several lines, and the `${...}` parts) and inside `/.../` regular expressions is never changed.

**Indentation (CH0036)** is worked out from the brackets, whatever the file had before, with 4 spaces per level. Tabs and 2-space indentation are fixed.

- Inside `(`, `[` or `{` a line is one level deeper than the line that opened it. A line starting with `}`, `)` or `]` is at the level of the line that opened it.
- In a `switch`, `case` and `default` are one level in and their lines two levels in.
- A line that continues the statement above it is one level deeper. This includes a line starting with `.` (as in `.then(...)`), `&&`, `||`, `?`, `:`, `+` or `-`, a line after one ending with an operator such as `=`, `+` or `=>`, and the body of an `if`, `for`, `while` or `else` without braces.
- In a `/* */` comment, lines starting with `*` line up under its first `*`. Other comment lines move with the comment's first line.
- The lines of a multi-line `` `...` `` string are left alone.

```javascript
// Before
function sign(req) {
  const body = req.getBody();
	if (!body)
		return;
  return axios.get(url)
  .then((res) => {
  return res.data;
  });
}

// After
function sign(req) {
    const body = req.getBody();
    if (!body)
        return;
    return axios.get(url)
        .then((res) => {
            return res.data;
        });
}
```

Not changed: quotes, semicolons, a space before `;` or after `,`, and long lines, which are reported only (CH0016). Bruno's `.bru` request files are not checked, only the `.js` files.

## Examples

Typical examples: `int a;  ` (trailing spaces) becomes `int a;`; `if(x)` becomes `if (x)`; `//text` becomes `// text`; `namespace N { ... }` becomes `namespace N;`; `get{return x;}` becomes `get { return x; }`.

---

← Previous: [Scripts](08-scripts.md) | [Contents](README.md) | Next: [Files and Updates](10-files-and-updates.md) →
