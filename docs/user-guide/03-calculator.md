# Calculator

Press **Ctrl+Alt+C**. A small bar opens, centered on the screen where your mouse is, with the text box ready. The box shows the hint "Enter expression (e.g. 2+2*3)".

## Using it

| Key or action | Effect |
|---|---|
| **Enter** | Calculates. Your text stays as typed and the result appears below it. |
| **Esc** | Hides the calculator |
| **Ctrl+H** | Shows or hides the history |
| **Ctrl+Arrow** | Moves the window 10 pixels (with Shift, 120 pixels) |
| Drag with the left mouse button | Moves the window |

The calculator also hides when it loses focus. The calculator has no title-bar buttons, and Alt+F4 also hides it rather than closing it.

The calculator never touches your clipboard. To copy a result, select it with the mouse and press **Ctrl+C**.

**History.** Kept until you exit the app, up to 200 entries; the oldest are dropped. It is shown as `operation = result` lines, and with "No operations yet" when empty. Only successful calculations are added. You cannot recall an entry into the input box with a key; retype it or copy it with the mouse.

## What you can type

- **Numbers:** `123`, `1.5`, `.5`. Use a decimal point only. `1,000` and `1e3` do not work.
- **Operators:** `+ - * / % ^` and parentheses.
  - `^` is right-associative and binds tighter than a leading minus: `-2^2` is `-4`, `2^3^2` is `512`.
  - `%` is the remainder, with the sign of the first number: `-7 % 3` is `-1`.
- **Constants:** `pi`, `e` (any letter case).
- **Functions** (any letter case):

| Function | Meaning |
|---|---|
| `abs(x)`, `sqrt(x)` | absolute value, square root |
| `round(x)` or `round(x, decimals)` | rounds (decimals 0 to 28; halves round away from zero) |
| `floor(x)`, `ceil(x)` | round down, round up |
| `min(a, ...)`, `max(a, ...)` | smallest, largest (one or more values) |
| `sin(x)`, `cos(x)`, `tan(x)` | trigonometry, in radians |
| `ln(x)`, `log(x)`, `exp(x)` | natural log, base-10 log, e to the power x |
| `len("text")` | length of a text |
| `find("part", "text")` or `find("part", "text", start)` | position of the part in the text, counting from 1, case-sensitive |

Text goes in double quotes (`""` inside means a quote) and is allowed only as a function argument.

## Number format

Results use `.` as the decimal separator and `,` for thousands. They show up to 28 decimals, with trailing zeros removed and never an exponent. Arithmetic is exact decimal arithmetic, so `0.1+0.2` is `0.3` and `1/3` is `0.3333333333333333333333333333`. Functions such as `ln`, `log`, `exp`, `sin`, `cos`, `tan` and fractional powers have about 15 digits of precision.

## Unit conversion

Write `<expression> <unit> to <unit>`. The conversion must be at the very end of the whole expression, with one unit before `to`. A unit can be a name, `name/name` (for example `km/h`) or `name^number` (for example `m^2`).

Examples that work: `(2 + 30) min to s` gives `1,920 s`, and `5 * 2 km to m` gives `10,000 m`. A sum of values in different units, such as `2 h + 30 min to min`, does not work and reports `Expected 'to' after 'h' at position 3`.

Conversion results are rounded to 10 significant digits and followed by the unit symbol.

**Unit matching.** An exact (case-sensitive) name or symbol is tried first. Otherwise any letter case works if only one unit fits. `MB` means megabytes and `Mb` means megabits. `mb` is ambiguous and gives an error.

| Kind | Units (symbol first, then some alternate names) |
|---|---|
| Length | nm, um, mm, cm, m, km, in, ft, yd, mi, nmi |
| Mass | mg, g, kg, t (metric tonne), oz, lb, st, ton (US short ton) |
| Temperature | K, C, F |
| Area | mm2, cm2, m2, km2, ha, in2, ft2, yd2, ac, mi2 (the `m^2` forms are accepted) |
| Volume | mL, cm3 (cc), L, m3, in3, ft3, tsp, tbsp, floz, cup, pt, qt, gal (US), ukgal |
| Time | ms, s, min, h, d, wk, yr (365.25 days) |
| Speed | km/h (kmh, kph), m/s, mph (mi/h), ft/s, knot (kn, knots) |
| Data, bits (1000-based) | b, kb (Kb, kbit), Mb, Gb, Tb |
| Data, bytes (1000-based) | B, kB (KB, kilobyte, kilobytes), MB, GB, TB, PB |
| Data, bytes (1024-based) | KiB, MiB, GiB, TiB, PiB |

Long names and plurals such as `meters`, `pounds`, `fahrenheit` and `miles` are accepted. Results print the canonical symbol: `10 m^2 to ft^2` prints `107.6391042 ft2`. You cannot convert between groups, for example `km` to `kg`.

## Verified examples

| You type | You get |
|---|---|
| `2+2*3` | `8` |
| `15 % 4` | `3` |
| `-7 % 3` | `-1` |
| `2^10` | `1,024` |
| `-2^2` | `-4` |
| `2^3^2` | `512` |
| `sqrt(2)` | `1.4142135623730950488016887242` |
| `round(3.14159, 2)` | `3.14` |
| `max(1, 5, 3)` | `5` |
| `100 C to F` | `212 F` |
| `32 F to C` | `0 C` |
| `0 C to K` | `273.15 K` |
| `5 km/h to mph` | `3.106855961 mph` |
| `10 km to mi` | `6.213711922 mi` |
| `1000 g to lb` | `2.204622622 lb` |
| `1 MB to Mb` | `8 Mb` |
| `1 MB to KiB` | `976.5625 KiB` |
| `1 GiB to MB` | `1,073.741824 MB` |
| `1 KB to B` | `1,000 B` |
| `1 kb to B` | `125 B` |
| `60 min to h` | `1 h` |
| `1 yr to d` | `365.25 d` |
| `1 gal to floz` | `128 floz` |
| `10 m^2 to ft^2` | `107.6391042 ft2` |
| `sin(pi)` | `0` |
| `ln(e)` | `1` |
| `find("l", "hello")` | `3` |
| `len("hello")` | `5` |
| `1 mb to MB` | error: `Unit 'mb' could be Mb or MB at position 3` |
| `1,000 + 1` | error: `Unexpected ',' at position 2` |

## Errors

When an expression is wrong, the message appears in a highlighted line and the offending character is selected in the input box. Every message ends with `at position N`, counting from 1. Empty input is ignored.

Messages you may see (each is followed by ` at position N`):

- `Invalid character '<c>'`
- `Invalid number format: multiple decimal points`
- `Invalid number format '.'`
- `Number is too large '<n>'` and `Number is too large`
- `Text is missing its closing quote`
- `Empty expression`
- `Unexpected '<t>'` and `Unexpected end of expression`
- `Expected ')'`, `Expected ',' or ')'`
- `Expected a unit before 'to'`, `Expected 'to' after '<unit>'`, `Expected a unit after 'to'`
- `Unknown name '<n>'`, `Unknown function '<n>'`
- `Function '<f>' expects <1 argument | N arguments | A to B arguments | at least N arguments>`
- `Division by zero`
- `Text can only be used as a function argument`
- `Unknown unit '<u>'`
- `Unit '<u>' could be A or B` (more than two names can be listed, joined with "or")
- `Can't convert <a> to <b>`
- `Square root of a negative number`
- `Result is not a real number`
- `Logarithm of zero or a negative number`
- `Expected a number` and `Expected text`
- `Start must be a whole number from 1 to <N>`
- `Text not found`
- `Decimal places must be a whole number from 0 to 28`

---

← Previous: [Hotkeys and hotkeys.cfg](02-hotkeys.md) | [Contents](README.md) | Next: [Snipping and the Snip Editor](04-snipping.md) →
