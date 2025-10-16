using CincoVertice.MyTools.UI.Win.CodeReview;
using CincoVertice.MyTools.UI.Win.Main;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.UI.Win.Setup;

public static class UiWinSetupExtensions
{
    public static IServiceCollection AddMyToolsUiLayer(IServiceCollection services)
    {
        services.AddTransient<MainForm>();
        services.AddTransient<CodeReviewForm>();

        return services;
    }

    public static IConfigurationBuilder AddMyToolsConfigurationProviders(IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        return configurationBuilder;
    }
}
