using CincoVertice.MyTools.Ui.CodeReview.Views;
using CincoVertice.MyTools.Ui.Setup;
using CincoVertice.MyTools.Ui.Win.CodeReview;
using CincoVertice.MyTools.Ui.Win.Forms.Calculator;
using CincoVertice.MyTools.Ui.Win.Main;
using CincoVertice.MyTools.Ui.Win.Services;
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

        services.AddSingleton<GlobalHotkeyService>();

        services.AddScoped<CodeReviewForm>();
        services.AddScoped<ICodeReviewView>(sp => sp.GetRequiredService<CodeReviewForm>());

        UiSetupExtensions.AddMyToolsUiLayer(services);

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
