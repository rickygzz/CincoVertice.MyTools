using System.Drawing;
using CincoVertice.MyTools.Ui.ColorPicker.Interfaces;
using CincoVertice.MyTools.Ui.ColorPicker.Models;
using CincoVertice.MyTools.Ui.ColorPicker.Presenters;
using CincoVertice.MyTools.Ui.ColorPicker.Views;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.ColorPicker;

public class ColorPresenterTests
{
    private static readonly Color DodgerBlue = Color.FromArgb(30, 144, 255);

    private readonly FakeColorView _view = new();
    private readonly FakePicker _picker = new();
    private readonly ColorPresenter _presenter;

    public ColorPresenterTests()
    {
        _picker.View = _view;
        _presenter = new ColorPresenter(_view, _picker);
    }

    [Fact]
    public void Show_ShowsTheColorsValues_AndThePaletteMadeFromIt()
    {
        // Act: a screen pixel's alpha means nothing
        _presenter.Show(Color.FromArgb(0, 30, 144, 255));

        // Assert
        Assert.Equal(
            new ColorValuesModel(
                DodgerBlue,
                "#1E90FF",
                "rgb(30, 144, 255)",
                "hsl(210, 100%, 56%)",
                "hsv(210, 88%, 100%)"),
            _view.Chosen);
        Assert.Equal(DodgerBlue, _view.Palette!.Base);
        Assert.False(_view.BackToPickedEnabled);
    }

    [Fact]
    public void ColorChosen_ShowsItsValues_AndKeepsThePalette()
    {
        // Arrange
        _presenter.Show(DodgerBlue);
        Color dark = _view.Palette!.Harmonies[1].Dark;

        // Act
        _view.Choose(dark);

        // Assert
        Assert.Equal(dark, _view.Chosen!.Color);
        Assert.Equal(1, _view.PaletteCount);
        Assert.True(_view.BackToPickedEnabled);
    }

    [Fact]
    public void ChoosingThePickedColorAgain_DisablesBackToPicked()
    {
        // Arrange
        _presenter.Show(DodgerBlue);
        _view.Choose(_view.Palette!.Shades[0]);

        // Act
        _view.Choose(DodgerBlue);

        // Assert
        Assert.False(_view.BackToPickedEnabled);
    }

    [Fact]
    public void UseAsBase_MakesThePalette_FromTheColorChosen()
    {
        // Arrange
        _presenter.Show(DodgerBlue);
        Color shade = _view.Palette!.Shades[0];
        _view.Choose(shade);

        // Act
        _view.UseAsBase();

        // Assert
        Assert.Equal(shade, _view.Palette.Base);
        Assert.Equal(shade, _view.Palette.Shades[3]);
        Assert.Equal(shade, _view.Chosen!.Color);
        Assert.True(_view.BackToPickedEnabled);
    }

    [Fact]
    public void BackToPicked_ShowsThePickedColor_AndItsPalette()
    {
        // Arrange: a palette made from a shade of a shade
        _presenter.Show(DodgerBlue);
        _view.Choose(_view.Palette!.Shades[0]);
        _view.UseAsBase();
        _view.Choose(_view.Palette.Harmonies[2].Light);

        // Act
        _view.BackToPicked();

        // Assert
        Assert.Equal(DodgerBlue, _view.Palette.Base);
        Assert.Equal(DodgerBlue, _view.Chosen!.Color);
        Assert.False(_view.BackToPickedEnabled);
    }

    [Fact]
    public void PickAnother_KeepsTheWindowOutOfThePick_ThenShowsTheNewColor_AndCopiesIt()
    {
        // Arrange
        _presenter.Show(DodgerBlue);
        _picker.Result = new PickedColorModel(Color.FromArgb(200, 40, 40), ColorTextFormat.Rgb);

        // Act
        _view.PickAnother();

        // Assert: out of the screenshot while picking only
        Assert.True(_picker.WasViewKeptOut);
        Assert.False(_view.IsKeptOut);
        Assert.Equal("rgb(200, 40, 40)", _view.Copied);
        Assert.Equal(Color.FromArgb(200, 40, 40), _view.Palette!.Base);
        Assert.Equal(Color.FromArgb(200, 40, 40), _view.Chosen!.Color);

        // And Back to picked now goes to the new color
        _view.Choose(_view.Palette.Shades[0]);
        _view.BackToPicked();
        Assert.Equal(Color.FromArgb(200, 40, 40), _view.Chosen!.Color);
    }

    [Fact]
    public void PickAnother_Cancelled_KeepsWhatsShown()
    {
        // Arrange
        _presenter.Show(DodgerBlue);
        Color shade = _view.Palette!.Shades[1];
        _view.Choose(shade);

        // Act
        _view.PickAnother();

        // Assert
        Assert.False(_view.IsKeptOut);
        Assert.Null(_view.Copied);
        Assert.Equal(shade, _view.Chosen!.Color);
    }

    private sealed class FakePicker : IScreenColorPicker
    {
        public PickedColorModel? Result { get; set; }

        public FakeColorView? View { get; set; }

        public bool WasViewKeptOut { get; private set; }

        public Task<PickedColorModel?> PickAsync()
        {
            WasViewKeptOut = View?.IsKeptOut == true;

            return Task.FromResult(Result);
        }
    }

    private sealed class FakeColorView : IColorView
    {
        public event EventHandler<Color>? ColorChosen;

        public event EventHandler? UseAsBaseRequested;

        public event EventHandler? BackToPickedRequested;

        public event EventHandler? PickAnotherRequested;

        public ColorPaletteModel? Palette { get; private set; }

        public int PaletteCount { get; private set; }

        public ColorValuesModel? Chosen { get; private set; }

        public bool BackToPickedEnabled { get; private set; }

        public bool IsKeptOut { get; private set; }

        public string? Copied { get; private set; }

        public void Choose(Color color) => ColorChosen?.Invoke(this, color);

        public void UseAsBase() => UseAsBaseRequested?.Invoke(this, EventArgs.Empty);

        public void BackToPicked() => BackToPickedRequested?.Invoke(this, EventArgs.Empty);

        public void PickAnother() => PickAnotherRequested?.Invoke(this, EventArgs.Empty);

        public void ShowPalette(ColorPaletteModel palette)
        {
            Palette = palette;
            PaletteCount++;
        }

        public void ShowChosen(ColorValuesModel values) => Chosen = values;

        public void SetBackToPickedEnabled(bool isEnabled) => BackToPickedEnabled = isEnabled;

        public IDisposable KeepOutOfPick()
        {
            IsKeptOut = true;

            return new KeptOut(this);
        }

        public void CopyToClipboard(string text) => Copied = text;

        private sealed class KeptOut(FakeColorView view) : IDisposable
        {
            public void Dispose() => view.IsKeptOut = false;
        }
    }
}
