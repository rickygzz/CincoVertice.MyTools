# Calculator

Press **Ctrl+Alt+C**. A small bar opens, centered on the screen where your mouse is, with the text box ready. The box shows the hint "Enter expression (e.g. 2+2*3)".

## Using it

| Key or action | Effect |
|---|---|
| **Enter** | Calculates. Your text stays as typed and the result appears below it. |
| **Esc** | Hides the calculator |
| **Ctrl+H** | Shows or hides the history |
| **Up** / **Down** | Brings back the lines you entered before, the newest first; Down past the newest brings back what you were typing |
| **Ctrl+Arrow** | Moves the window 10 pixels (with Shift, 120 pixels) |
| Drag with the left mouse button | Moves the window |

The calculator also hides when it loses focus. The calculator has no title-bar buttons, and Alt+F4 also hides it rather than closing it.

**Suggestions.** While you type, a list under the box offers what can come next in a conversion:

- After a time and a space (`10:30 `, `3 pm `, `now `), the places, each with its UTC offset today: `Mexico City (UTC-6)`. Type the start of a place to narrow them; any of its names works, in any case and without accents (`sao`, `cdmx`). After a 12-hour time, `a` or `p` also offers `am` or `pm`.
- After a number and a space (`10 `), the units, each with its name and kind: `km (kilometer, length)`. Type the start of a symbol or a name (`kilo`) to narrow them. After anything other than a single number, such as `1 + 2 `, the units wait for a letter, so they don't pop up at every space of a sum.
- After a number in a line with a date (`today + 30 `), the lengths of time to move it by: `d (days)`, `wk (weeks)`, `mo (months)`, `yr (years)`.
- Choosing the first place or unit writes `to` after it, and the list goes on with the second: the places to convert to, or the units of the same kind. So `10:30 `, then Mexico City, then India writes `10:30 Mexico City to India`.

| Key | In the list |
|---|---|
| **Down** / **Up** | Goes through the list |
| **Tab** | Writes the one chosen, or else the first |
| **Enter** | Writes the one chosen; with none chosen, it calculates |
| **Esc** | Hides the list (pressed again, it hides the calculator) |

Clicking one writes it too. The list shows only while the caret is at the end of the text.

The calculator never touches your clipboard. To copy a result, select it with the mouse and press **Ctrl+C**.

**History.** Kept until you exit the app, up to 200 entries; the oldest are dropped. It is shown as `operation = result` lines, and with "No operations yet" when empty. Only successful calculations are added.

**Bringing a line back.** **Up** puts the line you entered last back in the box, to change it or calculate it again; each Up goes one further back, through up to 200 lines, until you exit the app. A line that gave an error comes back too, so you can fix it. **Down** goes forward again, and past the newest line brings back what you were typing before you pressed Up. While the suggestion list is open, Up and Down go through it instead.

## What you can type

- **Numbers:** `123`, `1.5`, `.5`. Use a decimal point only. `1,000` and `1e3` do not work.
- **Operators:** `+ - * / % ^` and parentheses.
  - `^` is right-associative and binds tighter than a leading minus: `-2^2` is `-4`, `2^3^2` is `512`.
  - `%` is the remainder, with the sign of the first number: `-7 % 3` is `-1`.
- **Constants:** `pi`, `e` (any letter case).
- **The last answer:** `ans` is the result of the last calculation that worked: after `2+3`, `ans * 2` is `10`. A time in another place has no number, so `ans` stays as it was.
- **Names:** `x = 5` keeps 5 as `x`, and shows 5; then `x * 2` is `10`. A name keeps whatever its line gives: `rate = 5 km to m` keeps 5000, and `christmas = 2026-12-25` a date. Names are any letter case, kept until you exit the app, and can be given a new value at any time. A constant, a function, `ans`, `today`, `now` and `to` can't be given a value. A line that gives an error leaves the names as they were.
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

## Dates

- **A date** is year-month-day: `2026-12-25` (`2026-1-5` works too). `today` is today's date. A result that is a date shows its day of the week: `2026-12-25 (Friday)`.
- **Moving a date:** add or subtract a number of days, or a length of time: `today + 30`, `today + 30 d`, `today - 2 wk`, `2026-01-31 + 1 month` (`2026-02-28 (Saturday)`: the month's last day when the day doesn't exist), `today + 1 yr`. The units are `d` (`day`, `days`), `wk` (`w`, `week`, `weeks`), `mo` (`month`, `months`) and `yr` (`y`, `year`, `years`). A date moves by whole days and months only.
- **Between two dates:** subtract them: `2026-12-25 - today` gives the days between, such as `42 days`; negative when the first is earlier.
- **Lengths of time** add up and multiply: `2 wk + 3 d` is `17 days`, `1 yr + 2 mo` is `14 months`, `2 * 1 wk` is `14 days`. A month isn't a fixed number of days, so months and days are kept apart: `1 month, 3 days`.
- **Conversions are as before:** before `to`, a unit of time is converted: `1 yr to d` is `365.25 d`. And `2026 - 12 - 25`, with spaces, is still a subtraction.

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

## Time zones

Write `<time> <place> to <place>`, for example `10:30 Mexico to India` or `3 pm Miami to Sao Paulo`. The result is the time there, the place and its UTC offset: `22:00 India (UTC+5:30)`. When the day changes, the result ends with `, next day` or `, previous day`.

- **The time:** 24-hour (`9:00`, `21:30`), or 12-hour with `am` or `pm` (`9 pm`, `9:30 pm`). With am or pm, the answer uses am or pm too. Minutes always have two digits. `now` is the time now.
- **Without a first place**, the time is your computer's time: `10:00 to China`, `now to India`. `local` also means your computer's time zone: `9:00 Miami to local`.
- **Today's date** is used, because daylight saving time changes the difference. In winter `10:00 Mexico to Miami` gives `11:00 Miami (UTC-5)`, and in summer `12:00 Miami (UTC-4)`. A time that is skipped when the clocks move forward is refused. A time that happens twice, when the clocks move back, is taken as the first one, in standard time.
- **A number without am or pm is a number**, so `3 h to min` still converts hours. Write `15:00` or `3 pm` for a time.

Places can be written in any letter case, with spaces or `_` between words, and without accents:

| Region | Places (other names) |
|---|---|
| Mexico | Mexico City (Mexico, CDMX, Ciudad de Mexico), Guadalajara, Monterrey, Tijuana, Cancun |
| US and Canada | Miami, New York (NYC), Chicago, Denver, Los Angeles, Toronto |
| South America | Bogotá (Bogota, Colombia), Cali, Medellín (Medellin), Lima (Peru), Santiago (Chile), Buenos Aires (Argentina), São Paulo (Sao Paulo, SaoPaulo, Brazil, Brasil), Rio de Janeiro (Rio) |
| Europe | London, Madrid (Spain, Espana), Paris (France), Berlin (Germany) |
| Asia and Oceania | India (Delhi, New Delhi, Mumbai, Kolkata, Bangalore, Bengaluru), China (Beijing, Shanghai, Shenzhen), Hong Kong, Tokyo (Japan), Singapore, Dubai, Sydney |

For other places, use `UTC` or `GMT`, alone or with an offset (`UTC-5`, `UTC+5:30`), or a time zone name such as `America/Lima` or `Europe/Rome`. Abbreviations like `CST` or `IST` are not accepted, because each one means more than one time zone.

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
| `10:00 Mexico to India` | `21:30 India (UTC+5:30)` |
| `10:00 Mexico to China` | `0:00 China (UTC+8), next day` |
| `3 pm Mexico to Miami` | `4:00 pm Miami (UTC-5)` in winter |
| `10:00 Mexico to Sao Paulo` | `13:00 São Paulo (UTC-3)` |
| `10:00 Mexico to utc-3` | `13:00 UTC-3` |
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
- Names: `There's no last answer yet`, `Expected a value after '='`, `'<name>' is a constant` (or `a function`, `the last answer`, `today's date`, `a word the calculator uses`) `, so it can't be given a value`, `A time in another place can't be kept in a name`
- Dates: `There's no such date as <date>`, `A date moves by whole days and whole months`, `The date is out of range`, `A date can't be negative`, `Can't <add | subtract | multiply | divide> a <date | number | length of time> and a <...>`, `Expected a number, not a date` and `Expected a number, not a length of time`
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
- Time zones: `Expected minutes after ':'`, `Expected a time zone or 'to' after the time`, `Expected a time zone after 'to'`, `The hour must be from 0 to 23`, `With am or pm, the hour must be from 1 to 12`, `The minutes must be two digits, from 00 to 59`, `Write a time as 10:30 or 10:30 pm`, `Unknown time zone '<name>'`, `A UTC offset must be from UTC-14 to UTC+14`, `<time> doesn't exist in <place> today: the clocks move forward`

---

← Previous: [Hotkeys and hotkeys.cfg](02-hotkeys.md) | [Contents](README.md) | Next: [Snipping and the Snip Editor](04-snipping.md) →
