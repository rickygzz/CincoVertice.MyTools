using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Shell.Presenters;
using CincoVertice.MyTools.Ui.Win.Setup;
using CincoVertice.MyTools.Ui.Win.Shell;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Zones.Services;
using CincoVertice.MyTools.Ui.Zones.Presenters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CincoVertice.MyTools.Ui.Win;

public static class Program
{
    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    /// <param name="args">
    ///     <see cref="StartWithWindowsService.StartupArgument"/> when Windows started it at sign-in, which runs the
    ///     actions in startup.cfg.
    /// </param>
    [STAThread]
    static void Main(string[] args)
    {
        if (ProcessHelper.IsProcessCurrentlyRunning())
        {
            Application.Exit();

            return;
        }

        try
        {
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

            var trayContext = host.Services.GetRequiredService<TrayApplicationContext>();

            // Registers the hotkeys on this thread, the UI thread, and fills the tray menu
            host.Services.GetRequiredService<TrayPresenter>().Start();

            // Window zones: the drags of other programs' windows come through this thread's message loop
            host.Services.GetRequiredService<ZoneDragPresenter>().Start();
            host.Services.GetRequiredService<WindowDragWatcher>().Start();

            // Shows its setting; when that's on, looks for a newer version a while later. It never throws
            _ = host.Services.GetRequiredService<UpdatesPresenter>().Start();

            // On this thread, as from a hotkey: the tray's menu made it the UI thread, and scripts need it. It never
            // throws, and its waits leave the tray and the hotkeys working
            if (args.Contains(StartWithWindowsService.StartupArgument, StringComparer.OrdinalIgnoreCase))
            {
                _ = host.Services.GetRequiredService<StartupActionsService>().RunAsync();
            }

            Application.Run(trayContext);
        }
        catch (Exception ex)
        {
            HandleUnhandledException(ex);
        }
    }

    private static void HandleUnhandledException(Exception? ex)
    {
        MessageBox.Show(ex?.ToString() ?? "Unknown error.", "Application error");
    }
}
