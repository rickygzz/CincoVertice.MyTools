# My Tools

**A Windows tray app for screen snips, global hotkeys and your own scripts.**

My Tools runs in the system tray and is always a hotkey away. Snip part of the screen and mark it up with arrows,
shapes and text, use the calculator, review code, or write small scripts and bind them to any shortcut.

## Documentation

This page is a short tour. The [user guide](docs/user-guide/README.md) covers every tool in full:

- [Getting started](docs/user-guide/01-getting-started.md): first run, the tray menu, Start with Windows
- [Hotkeys and hotkeys.cfg](docs/user-guide/02-hotkeys.md)
- [Calculator](docs/user-guide/03-calculator.md)
- [Snipping and the snip editor](docs/user-guide/04-snipping.md)
- [Diagram editor](docs/user-guide/05-diagram-editor.md)
- [Mouse mover (activity)](docs/user-guide/06-mouse-mover.md)
- [Config editor](docs/user-guide/07-config-editor.md)
- [Scripts](docs/user-guide/08-scripts.md), with the whole language in [SCRIPTING.md](SCRIPTING.md)
- [Code review: check and fix a folder](docs/user-guide/09-code-review.md)
- [Files and updates](docs/user-guide/10-files-and-updates.md): what the app writes, updating, uninstalling
- [Troubleshooting](docs/user-guide/11-troubleshooting.md)
- [Quick reference: default shortcuts](docs/user-guide/12-quick-reference.md)

To work on the code, start with [ARCHITECTURE.md](ARCHITECTURE.md).

## Running the app

Windows may warn that the publisher can't be verified, because this build isn't code signed. To run it, click
**Run**. If you see "Windows protected your PC" instead, click **More info**, then **Run anyway**.

To have it start when you sign in to Windows, tick **Start with Windows** in the tray menu; untick it to stop. This needs
no administrator rights. If you move the app to another folder, tick it again.

**About My Tools...** in the tray menu shows which version you have. **Check for updates...** asks GitHub for the
latest release and offers to open its page; nothing is downloaded or installed for you. Tick **Check for updates at
start** to have it look a minute after it starts; it then only speaks up, with a notification you can click, when a
newer version is out. It's off until you tick it, since it goes on the network. See
[Files and updates](docs/user-guide/10-files-and-updates.md) for how to update and what the app leaves behind.

## Calculator

Type an expression and press Enter. Ctrl+H shows or hides the history of operations.

Add a unit and `to` to convert the result: `10 km to mi`, `100 C to F`, `(2+3)*4 GB to MiB`, `100 km/h to mph`. It
knows units of length, mass, temperature, area, volume, time, speed and data; see
[Calculator](docs/user-guide/03-calculator.md) for all of them and what you can type.

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

The editor remembers the style you used last, for new shapes and connectors, along with whether the properties panel is
shown and the window's size, in `config\diagrameditor.json`.

New text uses [Roboto](https://fonts.google.com/specimen/Roboto), which comes with the app under the
[SIL Open Font License](https://openfontlicense.org). Opening the editor installs it for your user if it's missing, so
browsers show saved diagrams in it too; that needs no administrator rights. On a computer without a diagram's font, its
SVG shows in another sans serif font. More in [Diagram editor](docs/user-guide/05-diagram-editor.md).

## Scripts

Hotkeys can run your own scripts: ask something, open a web page, start a program, type text, and more. See
[SCRIPTING.md](SCRIPTING.md) for the language and everything it can do, and [Scripts](docs/user-guide/08-scripts.md)
for getting started and sample scripts.

## Config editor

The tray menu's **Edit hotkeys...**, **Edit activity settings...** and **Scripts → Edit scripts...** open one window with
those files listed on the left. It colors them and underlines what the app couldn't use while you type: hover over the
line, or click the problem in the list below the text. Ctrl+S saves. Hotkeys apply when you save, and scripts the next
time they run. Ctrl+wheel zooms; click the percentage at the top right, or press Ctrl+0, to go back to 100%. More in
[Config editor](docs/user-guide/07-config-editor.md).

```
## Git Configuration

git config core.ignorecase false
git config core.autocrlf false
git config push.autosetupremote true
```

## License
This project is licensed under the [GNU General Public License v3.0](https://www.gnu.org/licenses/gpl-3.0.en.html).

Please feel free to use, redistribute, modify and contribute to this project under the terms of this license. :)
