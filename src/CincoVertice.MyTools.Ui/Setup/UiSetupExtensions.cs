using CincoVertice.MyTools.Ui.Calculator.Presenters;
using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.Ui.Setup;

public static class UiSetupExtensions
{
    public static IServiceCollection AddMyToolsUiLayer(IServiceCollection services)
    {
        services.AddSingleton<CalculatorPresenter>();

        return services;
    }
}
