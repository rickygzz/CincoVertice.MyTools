using CincoVertice.MyTools.Ui.Shell.Presenters;

namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     The application icon, shown in the systray and on the main window, and the systray's tooltip. The icon switches
///     between the normal (RG) and the red one, which shows that the mouse mover is on; the tooltip also says when the
///     computer is kept awake. Whoever shows them updates on <see cref="Changed"/>.
/// </summary>
public sealed class AppIconService : IDisposable
{
    public const string MouseMoverText = "Mouse mover on";
    public const string KeepingAwakeText = "Keeping awake";

    // Each Resources getter creates a new Icon, so both are loaded once
    private readonly Icon _normalIcon = Resources.Resources.RGIcon;
    private readonly Icon _redIcon = Resources.Resources.RGRedIcon;

    private bool _isActive;
    private bool _isKeepingAwake;

    public AppIconService()
    {
        Current = _normalIcon;
    }

    public event EventHandler? Changed;

    public Icon Current { get; private set; }

    /// <summary>The systray's tooltip: the app's name, then what's on, e.g. "My Tools\nKeeping awake".</summary>
    public string ToolTipText
    {
        get
        {
            IEnumerable<string> lines = new[]
            {
                TrayPresenter.AppTitle,
                _isActive ? MouseMoverText : null,
                _isKeepingAwake ? KeepingAwakeText : null,
            }.OfType<string>();

            return string.Join("\n", lines);
        }
    }

    /// <summary>
    ///     Shows the red icon when the mouse mover is on, the normal one otherwise.
    /// </summary>
    public void ShowActive(bool active)
    {
        if (active == _isActive)
        {
            return;
        }

        _isActive = active;
        Current = active ? _redIcon : _normalIcon;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Says in the tooltip whether the computer is kept awake.</summary>
    public void ShowKeepingAwake(bool keepingAwake)
    {
        if (keepingAwake == _isKeepingAwake)
        {
            return;
        }

        _isKeepingAwake = keepingAwake;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _normalIcon.Dispose();
        _redIcon.Dispose();
    }
}
