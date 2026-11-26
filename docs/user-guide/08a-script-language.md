# Script Language

How scripts are written: lines and comments, values, variables, operators, `if`, lists, `for` and your own functions. For what a script can do with them, see [Script Functions](08b-script-functions.md).

## Lines and comments

Each line is one step: storing a value, calling a function, or part of an `if`, a `for` or a `function`. `#` starts a comment, to the end of the line, also after code. Blank lines are ignored, and indentation is free.

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

Keywords (`if`, `elseif`, `else`, `end`, `for`, `in`, `function`, `return`, `and`, `or`, `not`, `true`, `false`) are not case-sensitive, and can't be the names of variables or functions.

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

## for

`for name in list` runs the lines up to its `end` once for each item of the list, with the item in the variable:

```mts
for w in process.windows("notepad")
    window.minimize(w)
end

for site in ["github.com", "google.com"]
    shell.open("https://" + site)
end
```

- The list is read once, when the `for` starts, so the loop always ends, even if the lines inside change the variable that held it.
- An empty list runs nothing. Anything else than a list is an error: `for c in "abc"` says `'for' goes through a list, not a text`.
- After the loop, the variable keeps the last item.
- A `for` can contain `if`s and other `for`s. Every `for` needs its `end`.

To run a number of times, go through `list.range(from, to)`, the whole numbers from one to the other. There is no `while`: to wait until something happens, try a number of times in a function, and leave with `return`:

```mts
# Waits up to 10 seconds for a window with this title
function waitFor(title)
    for i in list.range(1, 20)
        w = window.find(title)
        if w != 0
            return w
        end
        script.wait(500)
    end
    return 0
end
```

## Functions

`function name(parameter, ...)` defines steps you can call by name, as you call the built-in functions. `return value` gives the call its value:

```mts
function shrink(program)
    for w in process.windows(program)
        window.minimize(w)
    end
end

function firstWindow(title)
    w = window.find(title)
    if w == 0
        dialog.message("No window has " + title + " in its title")
        script.stop()
    end
    return w
end

shrink("notepad")
shrink("calc")
window.maximize(firstWindow("Outlook"))
```

- A function goes at the top of the script, not inside an `if`, a `for` or another function. It can be anywhere in the file, even after the lines that call it.
- Its name follows the rules of variable names, without a `.`, and can't be a built-in function's: `function text(x)` is an error.
- It must be called with as many values as it has parameters.
- `return` ends the function, also from inside a `for`. `return` alone, or reaching `end`, gives no value: `x = shrink("notepad")` is then an error.
- **Variables:** the parameters, and what the function assigns, are its own, and are gone when it returns. It can read the script's variables, but assigning one gives the function a variable of its own, so the script's keeps its value.
- A function can call other functions and itself, up to 100 calls deep.
- Functions in a script you include (`script.include("_Windows")`) can be called after the `script.include` line. An error inside one is reported at the call, with its place in that script: `shrink(): in _Windows, line 3, column 9: ...`. See [Shared steps](08c-script-examples.md#shared-steps-scriptinclude).

---

← Previous: [Scripts](08-scripts.md) | [Contents](README.md) | Next: [Script Functions](08b-script-functions.md) →
