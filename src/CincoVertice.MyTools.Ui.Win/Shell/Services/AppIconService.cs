namespace CincoVertice.MyTools.Ui.Win.Shell.Services;

/// <summary>
///     The application icon, shown in the systray and on the main window. It switches between the normal (RG) and
///     the red icon, which shows that activity (the mouse move timer) is on; whoever shows it updates on
///     <see cref="Changed"/>.
/// </summary>
public sealed class AppIconService : IDisposable
{
    // Each Resources getter creates a new Icon, so both are loaded once
    private readonly Icon _normalIcon = Resources.Resources.RGIcon;
    private readonly Icon _redIcon = Resources.Resources.RGRedIcon;

    public AppIconService()
    {
        Current = _normalIcon;
    }

    public event EventHandler? Changed;

    public Icon Current { get; private set; }

    /// <summary>
    ///     Shows the red icon when active, the normal one otherwise.
    /// </summary>
    public void ShowActive(bool active)
    {
        Icon icon = active ? _redIcon : _normalIcon;

        if (icon == Current)
        {
            return;
        }

        Current = icon;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _normalIcon.Dispose();
        _redIcon.Dispose();
    }
}
