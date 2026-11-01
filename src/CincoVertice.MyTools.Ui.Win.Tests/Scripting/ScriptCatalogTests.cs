using CincoVertice.Common.Application.Scripting;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Scripting;

public class ScriptCatalogTests
{
    [Theory]
    [InlineData(@"C:\scripts\GoogleSearch.mts", "GoogleSearch")]
    [InlineData(@"C:\scripts\Google Search.mts", "GoogleSearch")]
    [InlineData(@"C:\scripts\open-site_2.mts", "opensite_2")]
    [InlineData(@"C:\scripts\ñ.mts", "")]
    public void GetActionName_KeepsOnlyLettersDigitsAndUnderscores(string filePath, string expected)
    {
        Assert.Equal(expected, ScriptCatalog.GetActionName(filePath));
    }

    [Fact]
    public void GetScriptFiles_CreatesExampleScriptThatParses()
    {
        // Arrange
        string folder = Path.Combine(Path.GetTempPath(), "ScriptCatalogTests_" + Guid.NewGuid());

        try
        {
            // Act
            var files = ScriptCatalog.GetScriptFiles(folder);

            // Assert
            string example = Assert.Single(files, f => Path.GetFileName(f) == "GoogleSearch.mts");
            var tokens = new ScriptLexer().Tokenize(File.ReadAllText(example));
            Assert.NotEmpty(new ScriptParser().Parse(tokens));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
