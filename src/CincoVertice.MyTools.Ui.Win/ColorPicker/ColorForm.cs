using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.ColorPicker.Services;
using CincoVertice.MyTools.Ui.ColorPicker.Views;

namespace CincoVertice.MyTools.Ui.Win.ColorPicker;

/// <summary>
///     Color window: the chosen color's values, each with a Copy button; its shades on the left, darker above and
///     lighter below; dark and light colors that go well with it on the right.
///     <see cref="Ui.ColorPicker.Presenters.ColorPresenter"/> decides what it shows.
/// </summary>
public partial class ColorForm : Form, IColorView
{
    private const int ShadeHeight = 44;
    private const int ShadeGap = 4;
    private const int HarmonyHeight = 48;
    private const int HarmonyGap = 8;
    private const int HarmonyLabelWidth = 116;
    private const int HarmonySwatchWidth = 64;

    private readonly List<ColorSwatchControl> _shades = [];
    private readonly List<ColorSwatchControl> _harmonySwatches = [];
    private readonly List<Label> _harmonyLabels = [];

    public ColorForm()
    {
        InitializeComponent();
    }

    public event EventHandler<Color>? ColorChosen;

    public event EventHandler? UseAsBaseRequested;

    public event EventHandler? BackToPickedRequested;

    public event EventHandler? PickAnotherRequested;

    /// <summary>Every swatch: the shades, then each harmony's dark and light ones, for tests.</summary>
    internal IEnumerable<ColorSwatchControl> Swatches => _shades.Concat(_harmonySwatches);

    public void ShowPalette(ColorPaletteModel palette)
    {
        EnsureSwatches(_shades, ShadesPanel, palette.Shades.Count, PlaceShade);

        for (int i = 0; i < palette.Shades.Count; i++)
        {
            SetSwatch(_shades[i], palette.Shades[i]);
            _shades[i].IsBase = palette.Shades[i] == palette.Base && i == palette.Shades.Count / 2;
        }

        EnsureSwatches(_harmonySwatches, HarmoniesPanel, 2 * palette.Harmonies.Count, PlaceHarmony);
        EnsureHarmonyLabels(palette.Harmonies.Count);

        for (int i = 0; i < palette.Harmonies.Count; i++)
        {
            ColorHarmonyModel harmony = palette.Harmonies[i];
            _harmonyLabels[i].Text = harmony.Name;
            SetSwatch(_harmonySwatches[2 * i], harmony.Dark);
            SetSwatch(_harmonySwatches[2 * i + 1], harmony.Light);
        }
    }

    public void ShowChosen(ColorValuesModel values)
    {
        ChosenPanel.BackColor = values.Color;
        HexTextBox.Text = values.Hex;
        RgbTextBox.Text = values.Rgb;
        HslTextBox.Text = values.Hsl;
        HsvTextBox.Text = values.Hsv;
        CopiedLabel.Text = string.Empty;

        // The first one of that color only: two harmonies of a gray can be the same
        bool marked = false;

        foreach (ColorSwatchControl swatch in Swatches)
        {
            swatch.IsChosen = !marked && swatch.Color == values.Color;
            marked |= swatch.IsChosen;
        }
    }

    public void SetBackToPickedEnabled(bool isEnabled)
    {
        BackToPickedButton.Enabled = isEnabled;
    }

    public IDisposable KeepOutOfPick()
    {
        // It stays on the screen, under the frozen one; only Windows older than 10, version 2004, needs it
        // see-through instead
        return (IDisposable?)CaptureExclusion.Exclude([Handle]) ?? new SeeThrough(this);
    }

    public void CopyToClipboard(string text)
    {
        Clipboard.SetText(text);
    }

    private void SetSwatch(ColorSwatchControl swatch, Color color)
    {
        swatch.Color = color;
        SwatchToolTip.SetToolTip(swatch, ColorText.Hex(color));
    }

    /// <summary>Makes the swatches the first time, or when their number changes.</summary>
    private void EnsureSwatches(
        List<ColorSwatchControl> swatches,
        Control parent,
        int count,
        Action<ColorSwatchControl, int> place)
    {
        while (swatches.Count < count)
        {
            var swatch = new ColorSwatchControl();

            // Made here, as many as the palette has, so not in the designer
            swatch.Click += Swatch_Click;
            place(swatch, swatches.Count);
            swatches.Add(swatch);
            parent.Controls.Add(swatch);
        }
    }

    private void EnsureHarmonyLabels(int count)
    {
        while (_harmonyLabels.Count < count)
        {
            int row = _harmonyLabels.Count;
            var label = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Bounds = new Rectangle(0, row * (HarmonyHeight + HarmonyGap), HarmonyLabelWidth, HarmonyHeight),
            };

            _harmonyLabels.Add(label);
            HarmoniesPanel.Controls.Add(label);
        }
    }

    private void PlaceShade(ColorSwatchControl swatch, int index)
    {
        swatch.Bounds = new Rectangle(0, index * (ShadeHeight + ShadeGap), ShadesPanel.Width, ShadeHeight);
    }

    /// <summary>Two swatches a row: the dark one, then the light one.</summary>
    private void PlaceHarmony(ColorSwatchControl swatch, int index)
    {
        int row = index / 2;
        int x = HarmonyLabelWidth + index % 2 * (HarmonySwatchWidth + HarmonyGap);

        swatch.Bounds = new Rectangle(x, row * (HarmonyHeight + HarmonyGap), HarmonySwatchWidth, HarmonyHeight);
    }

    private void Swatch_Click(object? sender, EventArgs e)
    {
        if (sender is ColorSwatchControl swatch)
        {
            ColorChosen?.Invoke(this, swatch.Color);
        }
    }

    private void UseAsBaseButton_Click(object? sender, EventArgs e)
    {
        UseAsBaseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void BackToPickedButton_Click(object? sender, EventArgs e)
    {
        BackToPickedRequested?.Invoke(this, EventArgs.Empty);
    }

    private void PickAnotherButton_Click(object? sender, EventArgs e)
    {
        PickAnotherRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CopyButton_Click(object? sender, EventArgs e)
    {
        if (sender is Button { Tag: TextBox box } button && box.Text.Length > 0)
        {
            Clipboard.SetText(box.Text);
            CopiedLabel.Text = $"Copied {box.Text}";
            button.Focus();
        }
    }

    private void ColorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    /// <summary>The window invisible, but where it is, for as long as this isn't disposed.</summary>
    private sealed class SeeThrough : IDisposable
    {
        private readonly Form _form;
        private readonly double _opacity;

        public SeeThrough(Form form)
        {
            _form = form;
            _opacity = form.Opacity;
            form.Opacity = 0;

            // The screen keeps it until it's drawn again
            Dwmapi.DwmFlush();
        }

        public void Dispose()
        {
            if (!_form.IsDisposed)
            {
                _form.Opacity = _opacity;
            }
        }
    }
}
