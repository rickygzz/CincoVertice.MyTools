using CincoVertice.Common.Application.Calculator.Setup;
using CincoVertice.Common.Application.Setup;
using CincoVertice.MyTools.Ui.Calculator.Views;
using CincoVertice.MyTools.Ui.CodeReview.Views;
using CincoVertice.MyTools.Ui.Setup;
using CincoVertice.MyTools.Ui.Win.Calculator;
using CincoVertice.MyTools.Ui.Win.Calculator.Actions;
using CincoVertice.MyTools.Ui.Win.CodeReview;
using CincoVertice.MyTools.Ui.Win.CodeReview.Actions;
using CincoVertice.MyTools.Ui.Win.CodeReview.Services;
using CincoVertice.MyTools.Ui.Win.Shell;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.Ui.Win.Setup;

public static class UiWinSetupExtensions
{
    public static IServiceCollection AddMyToolsUiWinLayer(this IServiceCollection services)
    {
        services.AddSingleton<TrayApplicationContext>();

        services.AddSingleton<MainForm>();
        services.AddSingleton<CalculatorForm>();
        services.AddSingleton<ICalculatorView>(sp => sp.GetRequiredService<CalculatorForm>());

        services.AddSingleton<GlobalHotkeyService>();
        services.AddSingleton<CodeReviewWindowService>();
        services.AddSingleton<CheckFolderAction>();
        services.AddSingleton<FixFolderAction>();
        services.AddSingleton<ShowCalculatorAction>();

        services.AddScoped<CodeReviewForm>();
        services.AddScoped<ICodeReviewView>(sp => sp.GetRequiredService<CodeReviewForm>());

        UiSetupExtensions.AddMyToolsUiLayer(services);
        ApplicationSetup.AddApplicationLayer(services);
        CalculatorSetup.AddApplcationCalculatorLayer(services);

        return services;
    }

    public static IConfigurationBuilder AddMyToolsConfigurationProviders(
        this IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

        return configurationBuilder;
    }
}
