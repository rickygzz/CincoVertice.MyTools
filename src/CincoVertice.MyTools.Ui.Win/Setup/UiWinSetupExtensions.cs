using CincoVertice.Common.Application.Calculator.Setup;
using CincoVertice.Common.Application.Scripting;
using CincoVertice.Common.Application.Setup;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Calculator.Views;
using CincoVertice.MyTools.Ui.CodeReview.Views;
using CincoVertice.MyTools.Ui.Setup;
using CincoVertice.MyTools.Ui.Win.Calculator;
using CincoVertice.MyTools.Ui.Win.Calculator.Actions;
using CincoVertice.MyTools.Ui.Win.CodeReview;
using CincoVertice.MyTools.Ui.Win.CodeReview.Actions;
using CincoVertice.MyTools.Ui.Win.CodeReview.Services;
using CincoVertice.MyTools.Ui.Win.Scripting.Actions;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Shell;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
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
        services.AddSingleton<AppIconService>();
        services.AddSingleton<MouseMoveSimulator>();
        services.AddSingleton<CodeReviewWindowService>();
        services.AddSingleton<CheckFolderAction>();
        services.AddSingleton<FixFolderAction>();
        services.AddSingleton<ShowCalculatorAction>();
        services.AddSingleton<ToggleActivityAction>();
        services.AddSingleton<EditHotkeysAction>();

        // Every action can be bound to a shortcut in hotkeys.cfg; same instances as above
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<ShowCalculatorAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<ToggleActivityAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<CheckFolderAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<FixFolderAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<EditHotkeysAction>());

        // One action per script in config\scripts, after the built-in actions, which win a hotkeys.cfg name clash
        services.AddSingleton<ScriptEngine>();
        services.AddSingleton<TrayNotificationService>();

        foreach (string scriptFile in ScriptCatalog.GetScriptFiles())
        {
            if (ScriptCatalog.GetActionName(scriptFile).Length > 0)
            {
                services.AddSingleton<IAppAction>(sp => new ScriptAction(
                    scriptFile,
                    sp.GetRequiredService<ScriptEngine>(),
                    sp.GetRequiredService<TrayNotificationService>()));
            }
        }

        services.AddSingleton(_ => new HotkeyConfigService(HotkeyConfigService.DefaultFilePath));

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
