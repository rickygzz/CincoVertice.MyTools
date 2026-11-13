using CincoVertice.Common.Application.Calculator.Setup;
using CincoVertice.Common.Application.Scripting;
using CincoVertice.Common.Application.Scripting.Services;
using CincoVertice.Common.Application.Setup;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Calculator.Views;
using CincoVertice.MyTools.Ui.CodeReview.Views;
using CincoVertice.MyTools.Ui.ConfigEditor.Interfaces;
using CincoVertice.MyTools.Ui.ConfigEditor.Services;
using CincoVertice.MyTools.Ui.Diagramming.Interfaces;
using CincoVertice.MyTools.Ui.Diagramming.Services;
using CincoVertice.MyTools.Ui.Help.Interfaces;
using CincoVertice.MyTools.Ui.Setup;
using CincoVertice.MyTools.Ui.Shell.Interfaces;
using CincoVertice.MyTools.Ui.Shell.Presenters;
using CincoVertice.MyTools.Ui.Shell.Services;
using CincoVertice.MyTools.Ui.Shell.Views;
using CincoVertice.MyTools.Ui.Snipping.Interfaces;
using CincoVertice.MyTools.Ui.Snipping.Services;
using CincoVertice.MyTools.Ui.Win.Calculator;
using CincoVertice.MyTools.Ui.Win.Calculator.Actions;
using CincoVertice.MyTools.Ui.Win.CodeReview;
using CincoVertice.MyTools.Ui.Win.CodeReview.Actions;
using CincoVertice.MyTools.Ui.Win.CodeReview.Services;
using CincoVertice.MyTools.Ui.Win.ConfigEditor.Services;
using CincoVertice.MyTools.Ui.Win.Diagramming.Actions;
using CincoVertice.MyTools.Ui.Win.Diagramming.Services;
using CincoVertice.MyTools.Ui.Win.Help.Actions;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using CincoVertice.MyTools.Ui.Win.Scripting.Actions;
using CincoVertice.MyTools.Ui.Win.Scripting.Services;
using CincoVertice.MyTools.Ui.Win.Shell;
using CincoVertice.MyTools.Ui.Win.Shell.Actions;
using CincoVertice.MyTools.Ui.Win.Shell.Services;
using CincoVertice.MyTools.Ui.Win.Shell.Services.Config;
using CincoVertice.MyTools.Ui.Win.Snipping.Actions;
using CincoVertice.MyTools.Ui.Win.Snipping.Services;
using CincoVertice.MyTools.Ui.Win.Zones;
using CincoVertice.MyTools.Ui.Win.Zones.Actions;
using CincoVertice.MyTools.Ui.Win.Zones.Services;
using CincoVertice.MyTools.Ui.Zones.Interfaces;
using CincoVertice.MyTools.Ui.Zones.Presenters;
using CincoVertice.MyTools.Ui.Zones.Services;
using CincoVertice.MyTools.Ui.Zones.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.Ui.Win.Setup;

public static class UiWinSetupExtensions
{
    // At sign-in, the network may not be up yet
    private static readonly TimeSpan UpdateCheckStartDelay = TimeSpan.FromMinutes(1);

    private static string UpdateSettingsFilePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "config", "updates.json");

    /// <summary>
    ///     Where the app's releases are: the csproj's &lt;RepositoryUrl&gt;, which the build writes in it.
    /// </summary>
    /// <exception cref="InvalidOperationException">The csproj has none.</exception>
    public static string RepositoryUrl =>
        AppVersion.RepositoryUrlOf(typeof(TrayApplicationContext).Assembly)
        ?? throw new InvalidOperationException("The app was built without a <RepositoryUrl> in its csproj.");

    public static IServiceCollection AddMyToolsUiWinLayer(this IServiceCollection services)
    {
        services.AddSingleton<TrayApplicationContext>();
        services.AddSingleton<ITrayView>(sp => sp.GetRequiredService<TrayApplicationContext>());

        services.AddSingleton<MainForm>();
        services.AddSingleton<CalculatorForm>();
        services.AddSingleton<ICalculatorView>(sp => sp.GetRequiredService<CalculatorForm>());

        services.AddSingleton<GlobalHotkeyService>();
        services.AddSingleton<IHotkeyRegistrar>(sp => sp.GetRequiredService<GlobalHotkeyService>());
        services.AddSingleton<IStartWithWindows>(_ => StartWithWindowsService.ForCurrentUser());

        // Releases are on GitHub, in the csproj's <RepositoryUrl>; looking at start is a setting, off until turned on
        services.AddSingleton<IUpdatesView>(sp => sp.GetRequiredService<TrayApplicationContext>());
        services.AddSingleton<IReleaseSource>(_ => new GitHubReleaseSource(
            new HttpClient { Timeout = TimeSpan.FromSeconds(15) },
            RepositoryUrl,
            AppVersion.Of(typeof(TrayApplicationContext).Assembly)));
        services.AddSingleton<IUpdateSettingsStore>(_ => new JsonFileUpdateSettingsStore(UpdateSettingsFilePath));
        services.AddSingleton(sp => new UpdatesPresenter(
            sp.GetRequiredService<IUpdatesView>(),
            sp.GetRequiredService<IReleaseSource>(),
            sp.GetRequiredService<IUpdateSettingsStore>(),
            AppVersion.Of(typeof(TrayApplicationContext).Assembly),
            UpdateCheckStartDelay));
        services.AddSingleton<AppIconService>();
        services.AddSingleton<MouseMoveSimulator>();
        services.AddSingleton<AwakeKeeper>();
        services.AddSingleton<CodeReviewWindowService>();
        services.AddSingleton<CheckFolderAction>();
        services.AddSingleton<FixFolderAction>();
        services.AddSingleton<ShowCalculatorAction>();
        services.AddSingleton<ToggleActivityAction>();
        services.AddSingleton<EditHotkeysAction>();
        services.AddSingleton<EditActivitySettingsAction>();
        services.AddSingleton<OpenScriptsFolderAction>();
        services.AddSingleton<EditScriptsAction>();

        // The actions are read when they're needed, because some of them open this editor
        services.AddSingleton<IEditableFileStore, EditableFileStore>();
        services.AddSingleton<IEditableFileCatalog>(sp => new EditableFileCatalog(
            sp.GetRequiredService<HotkeyConfigService>(),
            sp.GetRequiredService<ActivitySettingsService>(),
            sp.GetRequiredService<StartupConfigService>(),
            sp.GetRequiredService<ZonesConfigService>(),
            sp.GetRequiredService<RecordingSettingsService>(),
            () => sp.GetRequiredService<AppActionCatalog>().GetActions().ToList(),
            ScriptCatalog.FolderPath));
        services.AddSingleton<IEditableFileLanguage>(sp => new EditableFileLanguage(
            () => sp.GetRequiredService<AppActionCatalog>().GetActions().Select(action => action.Name).ToList()));
        services.AddSingleton<ConfigEditorWindowService>();
        services.AddSingleton<ScreenSnipper>();
        services.AddSingleton<ITextRecognizer, WindowsTextRecognizer>();
        services.AddSingleton<ISnipEditorSettingsStore>(_ =>
            new JsonFileSnipEditorSettingsStore(SnipAndEditAction.SettingsFilePath));
        services.AddSingleton<SnipEditorWindowService>();
        services.AddSingleton<SnipRegionAction>();
        services.AddSingleton<SnipAndEditAction>();
        services.AddSingleton<SnipTextAction>();
        services.AddSingleton(_ => new RecordingSettingsService(RecordingSettingsService.DefaultFilePath));
        services.AddSingleton<RecordScreenAction>();
        services.AddSingleton<IDiagramFileStore, SvgDiagramFileStore>();
        services.AddSingleton<IDiagramEditorSettingsStore>(_ =>
            new JsonFileDiagramEditorSettingsStore(OpenDiagramEditorAction.SettingsFilePath));
        services.AddSingleton(_ => RobotoFontInstaller.ForCurrentUser());
        services.AddSingleton<OpenDiagramEditorAction>();

        // Window zones: the drag watcher is started on the UI thread by Program
        services.AddSingleton(sp => new ZonesConfigService(
            ZonesConfigService.DefaultFilePath,
            sp.GetRequiredService<TrayNotificationService>()));
        services.AddSingleton<IZoneLayoutSource>(sp => sp.GetRequiredService<ZonesConfigService>());
        services.AddSingleton<IZoneLayoutStore>(sp => sp.GetRequiredService<ZonesConfigService>());
        services.AddSingleton<ZoneEditorWindowService>();
        services.AddSingleton<IZoneDesktop, DesktopZones>();
        services.AddSingleton<WindowDragWatcher>();
        services.AddSingleton<IWindowDragSource>(sp => sp.GetRequiredService<WindowDragWatcher>());
        services.AddSingleton<ZoneOverlayForm>();
        services.AddSingleton<IZoneOverlayView>(sp => sp.GetRequiredService<ZoneOverlayForm>());
        services.AddSingleton<ZoneDragPresenter>();
        services.AddSingleton<ZoneMover>();
        services.AddSingleton<MoveToZoneAction>();
        services.AddSingleton<MoveToNextZoneAction>();
        services.AddSingleton<MoveToPreviousZoneAction>();
        services.AddSingleton<EditZonesAction>();

        // The user guide, docs\user-guide next to the exe
        services.AddSingleton<IHelpPageStore>(_ => new HelpPageStore(HelpPageStore.DefaultFolder));
        services.AddSingleton<HelpWindowService>();
        services.AddSingleton<ShowHelpAction>();

        // Every action can be bound to a shortcut in hotkeys.cfg; same instances as above
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<ShowCalculatorAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<ToggleActivityAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<CheckFolderAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<FixFolderAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<EditHotkeysAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<EditActivitySettingsAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<OpenScriptsFolderAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<EditScriptsAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<SnipRegionAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<SnipAndEditAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<SnipTextAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<RecordScreenAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<MoveToZoneAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<MoveToNextZoneAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<MoveToPreviousZoneAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<EditZonesAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<OpenDiagramEditorAction>());
        services.AddSingleton<IAppAction>(sp => sp.GetRequiredService<ShowHelpAction>());

        // The built-in actions above, plus one per script in config\scripts, read again on every hotkey reload
        services.AddSingleton(_ => new ScriptEngine(
            new JsonFileVariableStore(ScriptCatalog.GlobalVariablesFilePath)));
        services.AddSingleton<TrayNotificationService>();
        services.AddSingleton(sp => new AppActionCatalog(
            sp.GetServices<IAppAction>(),
            sp.GetRequiredService<ScriptEngine>(),
            sp.GetRequiredService<TrayNotificationService>(),
            sp.GetRequiredService<ZoneMover>(),
            ScriptCatalog.FolderPath));

        services.AddSingleton(_ => new HotkeyConfigService(HotkeyConfigService.DefaultFilePath));
        services.AddSingleton(_ => new ActivitySettingsService(ActivitySettingsService.DefaultFilePath));
        services.AddSingleton(_ => new StartupConfigService(StartupConfigService.DefaultFilePath));
        services.AddSingleton(sp => new StartupActionsService(
            sp.GetRequiredService<StartupConfigService>(),
            () => sp.GetRequiredService<AppActionCatalog>().GetActions().ToList(),
            sp.GetRequiredService<TrayNotificationService>()));
        services.AddSingleton(_ => new HotkeyFilesWatcher(
            HotkeyConfigService.DefaultFilePath,
            ScriptCatalog.FolderPath));

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
