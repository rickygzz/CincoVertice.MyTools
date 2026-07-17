using System.Runtime.Versioning;
using CincoVertice.Common.WinApi.Hotkey;
using CincoVertice.Common.WinApi.Hotkey.Window;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.CodeReview.Presenters;
using CincoVertice.MyTools.Ui.CodeReview.Views;
using CincoVertice.MyTools.Ui.Win.CodeReview;
using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.Ui.Win.Main;

[SupportedOSPlatform("windows10.0")]
public partial class MainForm : Form
{
    private readonly IServiceProvider _serviceProvider;

    private readonly Hotkey _hotkeys = new();

    public MainForm(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _serviceProvider = serviceProvider;
    }

    private void ExitNotifyContextMenu_Click(object sender, EventArgs e)
    {
        Dispose();
        Application.Exit();
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        HotkeyData hkCalculator = new(
            FSModifiers.MOD_CONTROL | FSModifiers.MOD_ALT,
            KeyCode.A,
            HotKeyDispatcher_HotKeyPressed);

        _hotkeys.RegisterHotKey(hkCalculator);
        // CodeReviewMenu_Click(sender, e);
    }

    private void HotKeyDispatcher_HotKeyPressed(object? sender, HotkeyKeyPressedEventArgs key)
    {
        // Check if modifier is CTL + ALT
        if (key.Modifier == (FSModifiers.MOD_CONTROL | FSModifiers.MOD_ALT))
        {
            if (key.Key == KeyCode.A)
            {
                System.Media.SystemSounds.Beep.Play();
            }
        }
    }

    private void CodeReviewMenu_Click(object sender, EventArgs e)
    {
        // Create a scope per window
        var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var form = sp.GetRequiredService<CodeReviewForm>();

        // Build presenter using the same form instance as ICodeReviewView
        _ = ActivatorUtilities.CreateInstance<CodeReviewPresenter>(sp, (ICodeReviewView)form);

        form.FormClosed += (_, _) => scope.Dispose();
        form.Show();
    }
}
