# Mouse Spotlight

Lost the mouse pointer on a large screen, or on one of several? Tap **Left Ctrl** twice, quickly. Every monitor dims, except for a circle around the pointer, which starts large and shrinks onto it, so your eye follows it there.

The circle follows the pointer while you move the mouse. The spotlight goes away when you:

- click, or press any key, or
- keep the mouse still for 3 seconds.

It never takes the focus or the clicks: the program you're working in stays active, and a click goes to it as usual.

## What counts as tapping twice

Press and let go of **Left Ctrl** twice, with no more than about half a second between. The right Ctrl key doesn't count. These don't show the spotlight, so Ctrl works as usual:

- Holding Ctrl down, even with Windows repeating it.
- Shortcuts with Ctrl, such as Ctrl+C then Ctrl+V.
- A click between the two taps.

## From a hotkey

The action `ShowMouseSpotlight` shows the spotlight too. It has no hotkey by default: add one to `hotkeys.cfg` (see [Hotkeys](02-hotkeys.md)), for example:

```
ShowMouseSpotlight = Ctrl+Alt+F
```

Pressing the hotkey again shows the spotlight again, from the start; any other key hides it.

---

← Previous: [Window Zones](13-window-zones.md) | [Contents](README.md) | Next: [Compare Files](15-compare-files.md) →
