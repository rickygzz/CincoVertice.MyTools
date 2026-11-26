using CincoVertice.MyTools.Ui.Calculator.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Calculator;

public sealed class JsonFileCalculatorRecallStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"recall-{Guid.NewGuid():N}");

    private string FilePath => Path.Combine(_folder, "config", "calculator.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Save_ThenLoad_GivesTheLinesBack_InTheirOrder_MakingTheFolder()
    {
        // Arrange
        var store = new JsonFileCalculatorRecallStore(FilePath);

        // Act
        store.Save(["1+1", "x = \"quoted\"", "10:30 Mexico City to India"]);

        // Assert: a new store, as in the next run
        Assert.Equal(
            ["1+1", "x = \"quoted\"", "10:30 Mexico City to India"],
            new JsonFileCalculatorRecallStore(FilePath).Load());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{ not json")]
    [InlineData("{ \"lines\": null }")]
    public void Load_OfAMissingOrBrokenFile_IsNone(string? content)
    {
        // Arrange
        if (content is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, content);
        }

        // Act / Assert
        Assert.Empty(new JsonFileCalculatorRecallStore(FilePath).Load());
    }
}
