using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.UI.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.UI;

internal static class Program
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        if (ProcessHelper.IsProcessCurrentlyRunning())
        {
            Application.Exit();

            return;
        }

        var serviceCollection = new ServiceCollection();
        ServicesConfigurator.Configure(serviceCollection);
        ServiceProvider = serviceCollection.BuildServiceProvider();

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(ServiceProvider.GetRequiredService<MainForm>());
    }
}
