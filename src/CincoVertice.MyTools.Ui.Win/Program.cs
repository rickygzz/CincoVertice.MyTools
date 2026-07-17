using System.Runtime.Versioning;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Win.Main;
using CincoVertice.MyTools.Ui.Win.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CincoVertice.MyTools.Ui.Win;

internal static class Program
{
    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    [SupportedOSPlatform("windows10.0")]
    static void Main()
    {
        if (ProcessHelper.IsProcessCurrentlyRunning())
        {
            Application.Exit();

            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddMyToolsConfigurationProviders();
        builder.Services.AddMyToolsUiWinLayer();

        using var host = builder.Build();

        var mainForm = host.Services.GetRequiredService<MainForm>();

        Application.Run(mainForm);
    }
}
