using CincoVertice.Common.WinApi.Hotkey;
using CincoVertice.Common.WinApi.Hotkey.Window;
using CincoVertice.Common.WinApi.Libs.Enums;
using CincoVertice.MyTools.Ui.Win.Calculator.Actions;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     Registers the application-wide hotkeys and runs their actions.
///     Must be started on the UI thread, since the hotkey window receives its messages there.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private readonly ShowCalculatorAction _showCalculatorAction;
    private readonly Hotkey _hotkeys = new();

    public GlobalHotkeyService(ShowCalculatorAction showCalculatorAction)
    {
        _showCalculatorAction = showCalculatorAction;
    }

    public void Start()
    {
        // Ctrl + Alt + A: calculator
        _hotkeys.RegisterHotKey(new HotkeyData(
            FSModifiers.MOD_CONTROL | FSModifiers.MOD_ALT,
            KeyCode.A,
            Calculator_HotKeyPressed));
    }

    private async void Calculator_HotKeyPressed(object? sender, HotkeyKeyPressedEventArgs key)
    {
        await _showCalculatorAction.ExecuteAsync();
    }

    public void Dispose()
    {
        _hotkeys.Dispose();
    }
}
