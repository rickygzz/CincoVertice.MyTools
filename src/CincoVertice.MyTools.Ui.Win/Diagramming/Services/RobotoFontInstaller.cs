using System.Security;
using CincoVertice.Common.Utils.Helpers;
using CincoVertice.Common.WinApi.Libs;
using CincoVertice.Common.WinApi.Libs.Enums;
using Microsoft.Win32;

namespace CincoVertice.MyTools.Ui.Win.Diagramming.Services;

/// <summary>
///     Installs the Roboto font inside the app for the current user, which needs no administrator rights, so other
///     programs show it too: a browser opening a saved diagram, and the box text is typed in. The editor draws with
///     its own copy (<see cref="DiagramFonts"/>), so failing here only means those fall back to another font.
/// </summary>
public sealed class RobotoFontInstaller
{
    private const string FileName = "Roboto.ttf";
    private const string RegistryValueName = "Roboto (TrueType)";
    private const string RegistryKey = @"Software\Microsoft\Windows NT\CurrentVersion\Fonts";

    private readonly string _fontsFolder;
    private readonly Func<bool> _isInstalled;
    private readonly Func<Stream> _openFont;
    private readonly Action<string, string> _register;
    private readonly Action<string> _loadNow;

    /// <param name="fontsFolder">Where the font file goes.</param>
    /// <param name="isInstalled">Tells whether the system has the font already.</param>
    /// <param name="openFont">Gives the font file's content.</param>
    /// <param name="register">Keeps the font from the next sign-in on, with this name and path.</param>
    /// <param name="loadNow">Makes the font at this path available right away.</param>
    public RobotoFontInstaller(
        string fontsFolder,
        Func<bool> isInstalled,
        Func<Stream> openFont,
        Action<string, string> register,
        Action<string> loadNow)
    {
        _fontsFolder = fontsFolder;
        _isInstalled = isInstalled;
        _openFont = openFont;
        _register = register;
        _loadNow = loadNow;
    }

    /// <summary>The installer for the current user of this computer.</summary>
    public static RobotoFontInstaller ForCurrentUser() => new(
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft",
            "Windows",
            "Fonts"),
        () => DiagramFonts.IsInstalled("Roboto"),
        DiagramFonts.OpenBundledFont,
        Register,
        LoadNow);

    /// <returns>True when the font is installed now; false when that failed.</returns>
    public bool EnsureInstalled()
    {
        if (_isInstalled())
        {
            return true;
        }

        try
        {
            string path = Path.Combine(_fontsFolder, FileName);

            // Installed earlier in this run, or by a run that couldn't finish: it may be in use, so left as it is
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(_fontsFolder);

                // A partial font at the real path would count as installed from then on
                AtomicFile.Write(path, CopyFont, overwrite: false);
            }

            _register(RegistryValueName, path);
            _loadNow(path);

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            return false;
        }
    }

    private void CopyFont(string path)
    {
        using Stream font = _openFont();
        using FileStream file = File.Create(path);

        font.CopyTo(file);
    }

    private static void Register(string name, string path)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKey);

        key.SetValue(name, path);
    }

    private static void LoadNow(string path)
    {
        GDI32.AddFontResource(path);

        // So running programs read the list of fonts again; without waiting for them
        User32.SendNotifyMessage(User32.HWND_BROADCAST, (uint)WM.WM_FONTCHANGE, 0, 0);
    }
}
