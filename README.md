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

Draw diagrams without opening a browser: Ctrl+Alt+D, or **Diagram editor** in the tray menu. Shapes, connectors that
follow them, text, groups, snapping and zoom. They're saved as SVG images that open anywhere, and open again here to
keep editing them. See [Diagram editor](docs/user-guide/05-diagram-editor.md) for the tools and every key.

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
