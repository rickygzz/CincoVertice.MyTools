# Window Zones

Window zones divide each monitor into areas (zones) that windows snap into. Hold **Shift** while you drag a window, drop it on a zone, and the window fills that zone. You can also move the active window to a zone with a hotkey or from a script.

## Dragging a window into a zone

1. Start dragging a window by its title bar, as usual.
2. Hold **Shift**. The zones of every monitor appear over the screen, numbered from 1. The zone under the mouse is shown in blue.
3. Let go of the mouse over a zone. The window moves into it, sized to fill it. Release Shift when you like.

Without Shift, dragging works as usual and no zones appear. If you let go of Shift before the mouse, the zones go away and the window stays where you dropped it. Pressing Shift while you **resize** a window by its edge shows no zones.

A maximized window is restored first, then put in the zone. The zone is the part of the window you see: Windows 10 and 11 put an invisible border of a few pixels around windows for resizing, and that border goes outside the zone, so windows in zones next to each other meet with no gap but the one you set.

## Setting the zones: zones.cfg

The zones are in `config\zones.cfg`, created with three columns the first time it is needed. Open it from the tray menu with **Edit window zones...**, or in the config editor's file list. Changes apply at the next drag; there is nothing to reload.

The same zones are used on every monitor, in percent of each monitor's work area (the monitor without the taskbar), so they fit monitors of any size.

Use **one** of these layouts:

| Line | Zones |
|---|---|
| `Layout = Columns 3` | 3 columns as wide as each other, numbered left to right |
| `Layout = Columns 25 50 25` | columns of these widths, in percent; they must add up to 100 |
| `Layout = Rows 2` | 2 rows as tall as each other, numbered top to bottom |
| `Layout = Rows 30 70` | rows of these heights, in percent |
| `Layout = Grid 3 x 2` | 3 columns and 2 rows, numbered row by row, left to right |

Or set your own zones instead, one `Zone` line each: left, top, width and height, in percent of the work area. They are numbered in the order of the lines, and may overlap; where they do, the smallest one under the mouse is used.

```
# A large zone on the left, and two on the right, one above the other
Zone = 0, 0, 60, 100
Zone = 60, 0, 40, 50
Zone = 60, 50, 40, 50
Gap = 8
```

`Gap` is the space between zones, and between them and the edges of the screen, in pixels from 0 to 100. It is 8 when not set. With a gap, a point in the gap counts as being in the zones on both sides, so the highlight doesn't flicker while you cross it.

A line that can't be used is skipped and shown in a notification titled "My Tools: some window zones are off", once until the problems change, and squiggled in the config editor. Without a layout that can be used, the three columns are used. A file with both a `Layout` and `Zone` lines uses the `Zone` lines.

## Moving the active window with a hotkey

These actions have no default hotkey. Add them to `hotkeys.cfg` (see [Hotkeys](02-hotkeys.md)):

| Action | What it does |
|---|---|
| `MoveToZone` | Moves the active window to the zone whose number follows the shortcut. Add one line per zone. |
| `MoveToNextZone` | Moves the active window to the next zone of its monitor; from the last zone to the first. A window in no zone goes to the first. |
| `MoveToPreviousZone` | The same, backwards. A window in no zone goes to the last. |
| `EditZones` | Opens `zones.cfg` in the config editor. |

```
MoveToZone = Ctrl+Alt+1, 1
MoveToZone = Ctrl+Alt+2, 2
MoveToZone = Ctrl+Alt+3, 3
MoveToNextZone = Ctrl+Alt+Win+Right
MoveToPreviousZone = Ctrl+Alt+Win+Left
```

The zones are those of the monitor the window is on. A zone number that doesn't exist, such as 4 with three columns, shows "There's no zone 4: there are 3, from 1 to 3." in a notification.

## From a script

`window.tozone(number)` moves the active window to that zone, as `MoveToZone` does. For example, a startup script can start a program and put it in its place:

```mts
shell.run("notepad.exe")
script.wait(2000)
window.tozone(3)
```

See [Scripts](08-scripts.md#window).

## Limits

- **Programs run as administrator.** Windows does not let My Tools move their windows unless My Tools runs as administrator too. Dropping one on a zone shows "Could not move the window: it runs as administrator...".
- **Minimum sizes.** Some programs can't be made smaller than a size of their own, so in a small zone they stick out.
- **Windows' own snapping.** Dragging a window to the edge or top of the screen still shows Windows' snap layouts. If they get in the way, turn off **Settings > System > Multitasking > Snap windows**.
- **One layout for every monitor.** Each monitor gets the same zones, in its own size.

---

← Previous: [Quick Reference](12-quick-reference.md) | [Contents](README.md)
