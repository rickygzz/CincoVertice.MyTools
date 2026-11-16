# Marketing materials

These materials introduce MyTools to general Windows users, starting with GitHub and release pages.
They describe the current source. Check a release's actual contents before using the copy for that release.

## Ready to use

| Asset | Purpose |
|---|---|
| [Root README](../../README.md) | Download link, first two minutes, complete feature overview, and a screenshot tour with seven real app-window captures |
| [Eight tutorials](../user-guide/16-tutorials.md) | Beginner walkthroughs; automatically included in the app's bundled user guide |
| [Tutorial samples](../tutorials/README.md) | Two text files to compare, an editable SVG diagram, and a search script |
| [Release overview](release-overview.md) | Reusable introduction for a release description |
| [Demo storyboard](demo-storyboard.md) | A short screen-recording sequence and small feature-demo ideas |
| [Screenshot files](../images) | PNGs to attach to GitHub releases or reuse in announcements |

## The message

**Name:** MyTools. The running application's tray label is **My Tools**.

**One line:** Everyday Windows tools, one shortcut away.

**Short description:** Capture and annotate screenshots, record quick how-tos, compare text, draw diagrams, and
organize your workspace from the Windows system tray. Free, open source, and no installer.

Lead with something a person wants to do. Show the result, give the shortcut, then link to a short tutorial.
Put scripting and code formatting after the everyday tools. Use real examples; avoid claims about speed, memory,
security, or compatibility that have not been measured or verified.

## Prepare a GitHub release description

1. Start with the actual changes in the release. The overview is product context, not a list of newly added features.
2. Use the introduction in [release-overview.md](release-overview.md) and add the release-specific changes below it.
3. Attach the Windows ZIP produced by the existing publishing workflow and include its SHA-256 hash.
4. Drag `snip-editor.png` and either `compare-files.png` or `diagram-editor.png` into GitHub's release-description
   editor. GitHub supplies image URLs; use those rather than a relative path from a release page.
5. Include the download, screenshot-tour, and tutorial links. Check them in Preview before publishing.
6. Recheck the default shortcuts against [the quick reference](../user-guide/12-quick-reference.md).

This folder prepares the content. It does not create tags, push commits, upload assets, or publish releases.

## Regenerate the screenshots

On Windows with the repository's .NET SDK and dependencies available, run from the repository root:

```powershell
.\scripts\Capture-Marketing.ps1
```

The helper builds into `artifacts/marketing-capture`, opens the real application forms with sample data, and saves
seven window-only PNGs in `docs/images`. It also regenerates `docs/tutorials/samples/weekend-plan.svg`, the editable
diagram shown in the capture. It briefly shows each demo window while Windows paints it, then disposes it.

The capture does not start the tray application, register global shortcuts, capture desktop pixels, read the clipboard,
run scripts, write user configuration, or install fonts. It captures each demo window using Windows' `PrintWindow`,
with the form's own rendering as a fallback. No screenshots are AI-generated.

Run the helper from a normal, unlocked desktop session. Inspect every image after regenerating it: text must be
readable, tools must match the app, and all visible content must remain sample data. Windows theme and DPI settings
can affect the captures. The fixture uses 100% logical scaling for consistent dimensions.

The sample-data capture is separate from demonstrating real interactions. Use the storyboard for recording actual
hotkeys, dragging, OCR, and the recording bar.

## Keep the assets current

- When shortcuts change, update the README, tutorial, reference table, and demo captions together.
- When a screen changes, regenerate and visually check its screenshot.
- Keep examples free of real filenames, email addresses, private documents, and account details.
- Link feature claims to a user-guide chapter. Do not present planned features as available.
- Keep the README's opening focused, followed by its visual feature tour. Put detailed reference material in the guide.
