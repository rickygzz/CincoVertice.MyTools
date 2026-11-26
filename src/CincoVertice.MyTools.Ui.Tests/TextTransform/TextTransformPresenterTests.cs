using CincoVertice.MyTools.Ui.TextTransform.Interfaces;
using CincoVertice.MyTools.Ui.TextTransform.Models;
using CincoVertice.MyTools.Ui.TextTransform.Presenters;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.TextTransform;

public class TextTransformPresenterTests
{
    private readonly FakeHost _host = new();

    [Fact]
    public async Task Run_PastesTheTransformChosen_OverTheSelection()
    {
        // Arrange
        _host.Selection = "hello";
        _host.Choice = TextTransformKind.Upper;

        // Act
        await new TextTransformPresenter(_host).RunAsync();

        // Assert
        Assert.Equal(["HELLO"], _host.Pasted);
        Assert.Empty(_host.Problems);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Run_WithNothingSelected_SaysSo_WithoutAMenu(string? selection)
    {
        // Arrange
        _host.Selection = selection;

        // Act
        await new TextTransformPresenter(_host).RunAsync();

        // Assert
        Assert.Equal([TextTransformPresenter.NothingSelected], _host.Problems);
        Assert.Equal(0, _host.Asked);
        Assert.Empty(_host.Pasted);
    }

    [Fact]
    public async Task Run_WithTheMenuClosed_ChangesNothing()
    {
        // Arrange
        _host.Selection = "hello";
        _host.Choice = null;

        // Act
        await new TextTransformPresenter(_host).RunAsync();

        // Assert
        Assert.Empty(_host.Pasted);
        Assert.Empty(_host.Problems);
    }

    [Fact]
    public async Task Run_OfWhatTheTransformCantRead_SaysWhy_AndLeavesTheSelection()
    {
        // Arrange
        _host.Selection = "not json";
        _host.Choice = TextTransformKind.JsonPrettyPrint;

        // Act
        await new TextTransformPresenter(_host).RunAsync();

        // Assert
        Assert.Empty(_host.Pasted);
        Assert.StartsWith("That isn't JSON", Assert.Single(_host.Problems));
    }

    [Fact]
    public async Task Run_ThatChangesNothing_PastesNothing()
    {
        // Arrange
        _host.Selection = "ALREADY";
        _host.Choice = TextTransformKind.Upper;

        // Act
        await new TextTransformPresenter(_host).RunAsync();

        // Assert
        Assert.Empty(_host.Pasted);
    }

    private sealed class FakeHost : ITextTransformHost
    {
        public string? Selection { get; set; }

        public TextTransformKind? Choice { get; set; }

        public int Asked { get; private set; }

        public List<string> Pasted { get; } = [];

        public List<string> Problems { get; } = [];

        public Task<string?> CopySelectionAsync() => Task.FromResult(Selection);

        public Task<TextTransformKind?> AskTransformAsync(IReadOnlyList<TextTransformModel> transforms)
        {
            Asked++;

            return Task.FromResult(Choice);
        }

        public Task PasteAsync(string text)
        {
            Pasted.Add(text);

            return Task.CompletedTask;
        }

        public void ShowProblem(string message) => Problems.Add(message);
    }
}
