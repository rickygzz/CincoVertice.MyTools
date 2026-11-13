# Window Zones

Window zones divide each monitor into areas (zones) that windows snap into. Hold **Shift** while you drag a window, drop it on a zone, and the window fills that zone. You can also move the active window to a zone with a hotkey or from a script.

## Dragging a window into a zone

1. Start dragging a window by its title bar, as usual.
2. Hold **Shift**. The zones of every monitor appear over the screen, numbered from 1. The zone under the mouse is shown in blue.
3. Let go of the mouse over a zone. The window moves into it, sized to fill it. Release Shift when you like.

Without Shift, dragging works as usual and no zones appear. If you let go of Shift before the mouse, the zones go away and the window stays where you dropped it. Pressing Shift while you **resize** a window by its edge shows no zones.

A maximized window is restored first, then put in the zone. The zone is the part of the window you see: Windows 10 and 11 put an invisible border of a few pixels around windows for resizing, and that border goes outside the zone, so windows in zones next to each other meet with no gap but the one you set.

## The window zones editor

Right-click the tray icon and choose **Edit window zones...**. The window shows a monitor at its real shape, with the zones over it as they'll be on the screen, numbered, gaps included. Only one editor is open at a time.

| To | Do this |
|---|---|
| Resize zones | Drag a border. Every border on the same line moves with it, so a grid stays a grid. A zone can't get smaller than 5% of the screen. |
| Resize one zone | Click it to select it: white handles appear at its corners and the middles of its sides. Drag a corner to move two sides at once, or a middle handle to move one. Only that zone changes, so it can go over the zones next to it. |
| Move a zone | Drag inside it. |
| Add a zone | Drag on an empty part of the monitor. A drag too small to make a zone of at least 5% adds nothing. |
| Split a zone | Right-click it where it should split, then **Split into left and right here** or **Split into top and bottom here**. Near an edge, it splits in the middle. |
| Delete a zone | Right-click it and choose **Delete**, or select it and press **Delete**. |
| Start over | **Layout**: 2, 3 or 4 columns, a wide center (25 / 50 / 25), 2 or 3 rows, or a grid of 2 x 2, 3 x 2 or 3 x 3. It replaces the zones. |
| Change the gap | **Gap**: the space between zones and around them, in pixels of the screen. |
| Give a monitor its own zones | **Edit layout of**: pick the monitor, then change its zones. See [The default layout and each monitor's own](#the-default-layout-and-each-monitors-own). |

Borders and moved zones snap to the edges of the screen and of the other zones when they come near. Where zones overlap, a click goes to the one whose center is nearest; the selected zone is drawn on top of the others, so all of it shows.

**Undo** (Ctrl+Z) and **Redo** (Ctrl+Y) cover every change, in every layout: undoing a change made on another monitor goes back to that monitor. Nothing is used until you **Save** (Ctrl+S), which saves every layout; the title shows `*` while there are unsaved changes, and closing asks whether to save them. The next drag uses what you saved. **Edit as text...** opens `zones.cfg` in the config editor and closes the window editor, asking first about unsaved changes.

## The default layout and each monitor's own

Every monitor uses the **default layout**, unless you give it **its own**. In the editor, **Edit layout of** lists:

- **Default layout**, shown at the shape of your first monitor. It is for every monitor without its own.
- Each monitor connected now, as `Monitor 1 (1920 x 1080)`, with `, own layout` after the ones that have their own.

A monitor without its own layout shows the default one, with the note "Uses the default; a change gives it its own". Your first change, such as picking a **Layout** or dragging a border, gives it its own, starting from the default's zones; a click that changes nothing doesn't. **Use default layout** drops the monitor's own layout, so it uses the default one again.

Changes to one layout are kept while you edit another, until you save them all.

A monitor is told apart by where Windows places it and its resolution, not by its number, which Windows can change when you plug monitors in or out. If you change a monitor's resolution, or move it in **Settings > System > Display**, it uses the default layout until you give it its own again. A monitor that isn't connected keeps its layout in the file, for when it is again, such as a laptop's external monitor at the office.

Zone numbers, for `MoveToZone` and `window.tozone`, are those of the layout of the monitor the window is on.

## Setting the zones as text: zones.cfg

The zones are in `config\zones.cfg`, created with three columns the first time it is needed. The editor above writes it; you can also write it yourself: open it with **Edit as text...** in the editor, or in the config editor's file list. Changes apply at the next drag; there is nothing to reload. Saving from the window editor rewrites the file as `Zone` lines.

Zones are in percent of each monitor's work area (the monitor without the taskbar), so a layout fits monitors of any size. The lines at the top of the file are the **default layout**; a monitor's own layout comes after a `Monitor` line (see below).

Use **one** of these layouts:

| Line | Zones |
|---|---|
| `Layout = Columns 3` | 3 columns as wide as each other, numbered left to right |
| `Layout = Columns 25 50 25` | columns of these widths, in percent; they must add up to 100 |
| `Layout = Rows 2` | 2 rows as tall as each other, numbered top to bottom |
| `Layout = Rows 30 70` | rows of these heights, in percent |
| `Layout = Grid 3 x 2` | 3 columns and 2 rows, numbered row by row, left to right |

Or set your own zones instead, one `Zone` line each: left, top, width and height, in percent of the work area. They are numbered in the order of the lines, and may overlap. Where they do, the zone used is the one whose center is nearest the mouse, so aim at the middle of the zone you want; while you drag, the zone the window will go to is drawn on top of the others.

```
# A large zone on the left, and two on the right, one above the other
Zone = 0, 0, 60, 100
Zone = 60, 0, 40, 50
Zone = 60, 50, 40, 50
Gap = 8
```

`Gap` is the space between zones, and between them and the edges of the screen, in pixels from 0 to 100. It is 8 when not set. With a gap, a point in the gap counts as being in the zones on both sides, so the highlight doesn't flicker while you cross it.

A monitor's own layout is every line after a `Monitor` line, up to the next `Monitor` line. The `Monitor` line says where Windows places the monitor, in pixels: left, top, width and height. The window zones editor writes it for you; to write it yourself, the monitor's left and top are its place in **Settings > System > Display**, counted from the top left corner of the main monitor, so they are below 0 for a monitor left of or above it.

```
# The default layout
Layout = Columns 3
Gap = 8

# A wide monitor right of the main one: its own layout, with the default gap
Monitor = 1920, 0, 3440, 1440
Layout = Columns 25 50 25

# A monitor turned on its side, left of the main one
Monitor = -1080, -420, 1080, 1920
Layout = Rows 2
Gap = 0
```

A monitor's layout without `Gap` uses the default layout's gap, and one with only a `Gap` uses the default layout's zones.

A line that can't be used is skipped and shown in a notification titled "My Tools: some window zones are off", once until the problems change, and squiggled in the config editor. Without a layout that can be used, the three columns are used. A layout with both a `Layout` and `Zone` lines uses the `Zone` lines. A `Monitor` line that can't be used, or a second one for the same monitor, is a problem too, and the lines after it, up to the next `Monitor` line, are not used.

## Moving the active window with a hotkey

These actions have no default hotkey. Add them to `hotkeys.cfg` (see [Hotkeys](02-hotkeys.md)):

| Action | What it does |
|---|---|
| `MoveToZone` | Moves the active window to the zone whose number follows the shortcut. Add one line per zone. |
| `MoveToNextZone` | Moves the active window to the next zone of its monitor; from the last zone to the first. A window in no zone goes to the first. A window counts as in the zone whose top left corner it is at, even when its program keeps it larger or smaller than the zone. |
| `MoveToPreviousZone` | The same, backwards. A window in no zone goes to the last. |
| `EditZones` | Opens the window zones editor. |

```
MoveToZone = Ctrl+Alt+1, 1
MoveToZone = Ctrl+Alt+2, 2
MoveToZone = Ctrl+Alt+3, 3
MoveToNextZone = Ctrl+Alt+Win+Right
MoveToPreviousZone = Ctrl+Alt+Win+Left
```

The zones are those of the monitor the window is on: its own layout, or the default one. A zone number that doesn't exist, such as 4 with three columns, shows "There's no zone 4: there are 3, from 1 to 3." in a notification.

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
- **Monitors are told apart by place and resolution.** After changing either, a monitor uses the default layout until you give it its own again.

---

← Previous: [Quick Reference](12-quick-reference.md) | [Contents](README.md)
