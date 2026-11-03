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

## Diagram editor

Draw diagrams without opening a browser: Ctrl+Alt+D, or **Diagram editor** in the tray menu. They're saved as SVG images
that open anywhere, and open again here to keep editing them. SVG files from other programs can't be opened.

| To | Do |
|---|---|
| Add a shape | Pick it on the left (`R` rectangle, `U` rounded, `O` ellipse, `D` diamond, `P` parallelogram, `H` hexagon, `Y` cylinder, `M` document) and drag, or click for the usual size |
| Add a text | `T` and click, or double-click on nothing. Enter ends it, Shift+Enter starts a new line |
| Write in a shape, or label a connector | Double-click it |
| Connect two shapes | Hover a shape and drag from one of the circles around it to another shape; drop on a circle there to pick that side too. Or `C`, then drag from one shape to the other, and the sides are picked for you. The connector follows them when they move. `V` or Esc goes back to selecting |
| Attach a connector somewhere else | Click it, then drag one of the dots at its ends to another shape, or to a circle on it to pick the side. Dropped on nothing, it stays where it was. Dropped on the middle of its own shape, the side is picked for you again |
| Change a connector's path | Click an elbow connector, then drag any piece of its line sideways; it stays level and upright, with rounded corners. **Reset line** lets it work out its own path again, and so does choosing Elbow or Straight |
| See and change what's selected | The panel on the right: position, size, corner radius, colors, border, text, font and connector options. F4 or **Properties** hides it |
| Select | Click, Shift+click to add, or drag on nothing around them. Ctrl+A for all |
| Move | Drag, or the arrow keys (Shift for one pixel at a time) |
| Scale | Drag a square around the selection, with Shift to keep the proportions |
| Line up | Dragging snaps to other shapes' edges and middles, showing a guide, and otherwise to the grid. Alt turns it off |
| Change colors, line, text size or connector style | Use the top bar, for what's selected and for what comes next |
| Copy, paste, duplicate | Ctrl+C, Ctrl+V, Ctrl+D, also between editor windows. Ctrl+X cuts |
| Group, ungroup | Ctrl+G, Ctrl+Shift+G |
| Bring to front, send to back | Ctrl+], Ctrl+[ |
| Zoom | Ctrl+wheel, Ctrl+plus, Ctrl+minus, Ctrl+0 for 100% |
| Move the view | Wheel (Shift for sideways), or drag with the middle button or Space held |
| Undo, redo, save | Ctrl+Z, Ctrl+Y, Ctrl+S (Ctrl+Shift+S to save as) |

If you already have a `config\hotkeys.cfg`, add `DiagramEditor = Ctrl+Alt+D` to it to get the hotkey.

New text uses [Roboto](https://fonts.google.com/specimen/Roboto), which comes with the app under the
[SIL Open Font License](https://openfontlicense.org). Opening the editor installs it for your user if it's missing, so
browsers show saved diagrams in it too; that needs no administrator rights. On a computer without a diagram's font, its
SVG shows in another sans serif font.

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
