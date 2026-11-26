using System.Windows.Forms;
using CincoVertice.MyTools.Ui.Calculator.Interfaces;
using CincoVertice.MyTools.Ui.Calculator.Presenters;
using CincoVertice.MyTools.Ui.Win.Calculator;
using CincoVertice.MyTools.Ui.Win.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests.Calculator;

public class CalculatorRecallSetupTests
{
    [Fact]
    public void Setup_GivesTheCalculatorTheStore_ThatKeepsItsLinesBetweenRuns()
    {
        // Arrange: the app's services, with the lines of a run before in memory instead of config\calculator.json
        var store = new MemoryRecallStore(["7*6"]);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddMyToolsUiWinLayer();
        services.AddSingleton<ICalculatorRecallStore>(store);
        using ServiceProvider provider = services.BuildServiceProvider();
        var presenter = provider.GetRequiredService<CalculatorPresenter>();
        var form = provider.GetRequiredService<CalculatorForm>();

        // Act
        ((TextBox)form.Controls.Find("CalculatorTextBox", true).Single()).Text = "1+1";
        presenter.Evaluate();

        // Assert
        Assert.Equal(["7*6", "1+1"], store.Lines);
    }

    private sealed class MemoryRecallStore(IReadOnlyList<string> lines) : ICalculatorRecallStore
    {
        public IReadOnlyList<string> Lines { get; private set; } = lines;

        public IReadOnlyList<string> Load() => Lines;

        public void Save(IReadOnlyList<string> lines) => Lines = lines;
    }
}
