using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Win.Main;
using CincoVertice.MyTools.Ui.Win.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CincoVertice.MyTools.Ui.Win;

public static class Program
{
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

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => HandleUnhandledException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => HandleUnhandledException(e.ExceptionObject as Exception);

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

    private static void HandleUnhandledException(Exception? ex)
    {
        MessageBox.Show(ex?.ToString() ?? "Unknown error.", "Application error");
    }
}
