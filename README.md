# My Tools

**A Windows tray app for screen snips, global hotkeys and your own scripts.**

My Tools runs in the system tray and is always a hotkey away. Snip part of the screen and mark it up with arrows,
shapes and text, use the calculator, review code, or write small scripts and bind them to any shortcut.

## Running the app

Windows may warn that the publisher can't be verified, because this build isn't code signed. To run it, click
**Run**. If you see "Windows protected your PC" instead, click **More info**, then **Run anyway**.

## Calculator

Type an expression and press Enter. Ctrl+H shows or hides the history of operations.

Add a unit and `to` to convert the result: `10 km to mi`, `100 C to F`, `(2+3)*4 GB to MiB`, `100 km/h to mph`.

| Group | Units |
|---|---|
| Length | `nm` `um` `mm` `cm` `m` `km` `in` `ft` `yd` `mi` `nmi` |
| Mass | `mg` `g` `kg` `t` (tonne) `oz` `lb` `st` `ton` (US, 2000 lb) |
| Temperature | `C` `F` `K` |
| Area | `mm2` `cm2` `m2` `km2` `ha` `in2` `ft2` `yd2` `ac` `mi2` (or `m^2`...) |
| Volume | `mL` `cm3` `L` `m3` `in3` `ft3` `tsp` `tbsp` `floz` `cup` `pt` `qt` `gal` (all US) `ukgal` |
| Time | `ms` `s` `min` `h` `d` `wk` `yr` (365.25 days) |
| Speed | `km/h` `m/s` `mph` `ft/s` `knot` |
| Data | `B` `kB` `MB` `GB` `TB` `PB` (1000) `KiB` `MiB` `GiB` `TiB` `PiB` (1024), bits: `b` `kb` `Mb` `Gb` `Tb` |

Long names like `meters` or `hours` work too. Any case works, unless it could mean two units: `mb` is an error because
`MB` is megabytes and `Mb` is megabits. Results are rounded to 10 significant digits.

## Scripts

Hotkeys can run your own scripts: ask something, open a web page, start a program, type text, and more. See
[SCRIPTING.md](SCRIPTING.md) for the language and everything it can do.

```
## Git Configuration

git config core.ignorecase false
git config core.autocrlf false
git config push.autosetupremote true
```

## License
This project is licensed under the [GNU General Public License v3.0](https://www.gnu.org/licenses/gpl-3.0.en.html).

Please feel free to use, redistribute, modify and contribute to this project under the terms of this license. :)
