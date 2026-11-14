# Script Language

How scripts are written: lines and comments, values, variables, operators, `if` and lists. For what a script can do with them, see [Script Functions](08b-script-functions.md).

## Lines and comments

Each line is one step: storing a value, calling a function, or part of an `if`. `#` starts a comment, to the end of the line, also after code. Blank lines are ignored, and indentation is free.

```mts
# Say hello
name = "Ricky"
dialog.message("Hello " + name)
```

A line can continue inside parentheses `( )` or brackets `[ ]`, so long calls and lists can span lines:

```mts
shell.open("https://www.google.com/search?q="
    + text.urlencode("my tools"))
```

A line either stores a value (`name = ...`) or calls a function (`name(...)`). A function that returns a value may stand alone on its line; the value is discarded.

## Values

| Type | Examples | Notes |
|---|---|---|
| Number | `3`, `2.5`, `.5`, `-4` | Decimal numbers with a `.` (not `,`). Shown without trailing zeros: `10 / 4` is `2.5`. |
| Text | `"Hello"`, `""` | Double quotes only. Escapes: `\"` quote, `\\` backslash, `\n` new line, `\t` tab. For several lines, see [Multi-line texts](#multi-line-texts). |
| True or false | `true`, `false` | The result of comparisons; needed by `if`, `and`, `or`, `not`. |
| List | `[1, "two", true]`, `[]` | Items of any type, even lists. See [Lists](#lists). |

## Multi-line texts

A text of several lines goes between `"""` at the end of a line and a line starting with `"""`. It is taken exactly as written: quotes, `\` and `#` are plain characters (escapes don't work there), so you can paste a template in as it is.

```mts
signature = """
    Best regards,
    Ricky
    "My Tools" user
    """
clipboard.paste(signature)
```

The spaces before the closing `"""` are removed from every line, so the text can be indented with the script; above, the text starts with `Best`. A line may start further right (it keeps the extra spaces), but not further left. The closing line can go on, as in `""")` to end a call.

## Variables

`name = value` stores a value. Use the name to read it back. Names start with a letter or `_`, and can contain letters, digits and `_`, but not a `.`. Names are not case-sensitive: `Name` and `name` are the same variable.

```mts
count = 3
count = count + 1
dialog.message(count)
```

A variable can hold any type, and a new value replaces the old one. Reading a variable that was never set is an error.

## Operators

From lowest to highest priority:

| Operators | Meaning |
|---|---|
| `or` | True if either side is true. The right side is skipped when the left is true. |
| `and` | True if both sides are true. The right side is skipped when the left is false. |
| `not` | Turns true into false and false into true. |
| `==` `!=` `<` `<=` `>` `>=` | Compare two values. |
| `+` `-` | Add, subtract. `+` also joins texts and lists. |
| `*` `/` `%` | Multiply, divide, remainder. Dividing by zero is an error. |
| `-` (in front) | Negative: `-x`. |
| `[ ]` | A list item: `list[1]`. |

Use parentheses to change the order: `(1 + 2) * 3` is `9`.

**Joining text:** when either side of `+` is a text, both are joined as text, so `"Total: " + 3` is `"Total: 3"`, and `"x" + true` is `"xtrue"`.

**Comparing:**

- `==` and `!=` work on any values. Texts are case-sensitive (`"A" == "a"` is false), and a text never equals a number (`"1" == 1` is false). Lists are equal when all their items are equal.
- `<` `<=` `>` `>=` compare two numbers or two texts. Texts are compared character by character.
- Compare two values at a time: write `a < b and b < c`, not `a < b < c`.
- `=` stores a value; `==` compares. `if x = 1` is an error.

## if

```mts
size = number(dialog.ask("Size?", "10"))

if size < 10
    dialog.message("small")
elseif size < 100
    dialog.message("medium")
else
    dialog.message("large")
end
```

`elseif` and `else` are optional, and you can use as many `elseif` as you like. An `if` can contain another `if`. Every `if` needs its `end`. The condition must be true or false: `if "yes"` is an error, write `if answer == "yes"`.

Keywords (`if`, `elseif`, `else`, `end`, `and`, `or`, `not`, `true`, `false`) are not case-sensitive.

## Lists

```mts
sites = ["github.com", "google.com", "stackoverflow.com"]

dialog.message(sites[1])             # github.com: the first item is 1
dialog.message(list.length(sites))   # 3
dialog.message(sites + ["bing.com"])
```

- `list[n]` is item `n`, from `1` to `list.length(list)`. Other positions are an error that says how many items there are.
- Items can't be changed: `sites[1] = "x"` is an error. Build a new list instead.
- `list + list` joins two lists; `list.contains(list, item)` checks for an item.
- `dialog.message(list)` shows the items, as in `[github.com, google.com]`.

---

← Previous: [Scripts](08-scripts.md) | [Contents](README.md) | Next: [Script Functions](08b-script-functions.md) →
