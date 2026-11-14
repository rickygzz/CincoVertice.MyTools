using CincoVertice.MyTools.Ui.Calculator.Presenters;
using CincoVertice.MyTools.Ui.Shell.Presenters;
using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.Ui.Setup;

public static class UiSetupExtensions
{
    public static IServiceCollection AddMyToolsUiLayer(IServiceCollection services)
    {
        services.AddSingleton<CalculatorPresenter>();
        services.AddSingleton<TrayPresenter>();

        return services;
    }
}
