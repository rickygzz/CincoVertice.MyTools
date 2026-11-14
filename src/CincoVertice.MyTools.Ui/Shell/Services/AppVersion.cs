using System.Reflection;

namespace CincoVertice.MyTools.Ui.Shell.Services;

/// <summary>
///     Versions as major.minor.patch, from a release's tag ("v0.0.6") or from the app itself ("0.0.6+3f2a1c", where
///     the build adds the commit after a +), and the repository the app's releases are in.
/// </summary>
public static class AppVersion
{
    /// <summary>
    ///     The version in the text, always with three parts: Version counts a missing part as lower, so 0.0.6 would
    ///     otherwise be older than 0.0.6.0. Null when there's none.
    /// </summary>
    public static Version? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        string version = text.Trim();

        if (version.StartsWith('v') || version.StartsWith('V'))
        {
            version = version[1..];
        }

        // What comes after is the build's: the commit after a +, or a pre-release after a -
        int end = version.IndexOfAny(['+', '-', ' ']);

        if (end >= 0)
        {
            version = version[..end];
        }

        return Version.TryParse(version, out Version? parsed)
            ? new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0))
            : null;
    }

    /// <summary>The assembly's version: its informational version, else its version, else 0.0.0.</summary>
    public static Version Of(Assembly assembly)
    {
        string? informational =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return Parse(informational) ?? Parse(assembly.GetName().Version?.ToString()) ?? new Version(0, 0, 0);
    }

    /// <summary>
    ///     The repository the assembly was built from: its RepositoryUrl, which the build writes from the csproj's
    ///     &lt;RepositoryUrl&gt;. Null when there's none.
    /// </summary>
    public static string? RepositoryUrlOf(Assembly assembly) => assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(metadata => metadata.Key == "RepositoryUrl")
        ?.Value is { Length: > 0 } url
            ? url
            : null;

    /// <summary>As people read it: 0.0.6.</summary>
    public static string Format(Version version) => version.ToString(3);
}
