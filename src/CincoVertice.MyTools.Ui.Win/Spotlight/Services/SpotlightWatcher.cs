using System.ComponentModel;
using CincoVertice.Common.WinApi.Helpers;
using CincoVertice.MyTools.Ui.Spotlight.Presenters;
using CincoVertice.MyTools.Ui.Spotlight.Services;

namespace CincoVertice.MyTools.Ui.Win.Spotlight.Services;

/// <summary>
///     Shows the mouse spotlight when the user taps Left Ctrl twice, in any program, and the app spotlight when they
///     tap Right Ctrl twice, and hides them at their next click or key. Start it on the UI thread: the keys and clicks
///     come through its messages.
/// </summary>
public sealed class SpotlightWatcher : IDisposable
{
    private readonly SpotlightPresenter _presenter;
    private readonly AppSpotlightPresenter _appPresenter;
    private readonly DoubleTapDetector _detector = new();
    private readonly DoubleTapDetector _appDetector = new();

    private InputHook? _input;
    private SynchronizationContext? _uiThread;

    public SpotlightWatcher(SpotlightPresenter presenter, AppSpotlightPresenter appPresenter)
    {
        _presenter = presenter;
        _appPresenter = appPresenter;
    }

    public void Start()
    {
        _uiThread = SynchronizationContext.Current;

        try
        {
            _input = new InputHook();
            _input.KeyDown += OnKeyDown;
            _input.KeyUp += OnKeyUp;
            _input.MouseDown += OnMouseDown;
        }
        catch (Win32Exception)
        {
            // Without the hook only the actions show them, and they hide by themselves
        }
    }

    public void Dispose()
    {
        _input?.Dispose();
        _input = null;
    }

    private static TimeSpan Now => TimeSpan.FromMilliseconds(Environment.TickCount64);

    private void OnKeyDown(object? sender, Keys key)
    {
        HideSoon();
        _detector.KeyDown(key == Keys.LControlKey, Now);
        _appDetector.KeyDown(key == Keys.RControlKey, Now);
    }

    private void OnKeyUp(object? sender, Keys key)
    {
        if (_detector.KeyUp(key == Keys.LControlKey, Now))
        {
            // After the hook's call: Windows waits for it before the key goes on
            Post(_presenter.Show);
        }

        if (_appDetector.KeyUp(key == Keys.RControlKey, Now))
        {
            Post(_appPresenter.Show);
        }
    }

    private void OnMouseDown(object? sender, (Point Point, MouseButtons Button) e)
    {
        HideSoon();
        _detector.Interrupt();
        _appDetector.Interrupt();
    }

    private void HideSoon()
    {
        if (_presenter.IsShown)
        {
            Post(_presenter.Hide);
        }

        if (_appPresenter.IsShown)
        {
            Post(_appPresenter.Hide);
        }
    }

    private void Post(Action action)
    {
        if (_uiThread is null)
        {
            action();
        }
        else
        {
            _uiThread.Post(_ => action(), null);
        }
    }
}
