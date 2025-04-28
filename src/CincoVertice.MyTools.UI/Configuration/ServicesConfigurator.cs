using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.UI.Configuration;

public static class ServicesConfigurator
{
    public static void Configure(IServiceCollection services)
    {
        services.AddTransient<MainForm>();
    }
}
