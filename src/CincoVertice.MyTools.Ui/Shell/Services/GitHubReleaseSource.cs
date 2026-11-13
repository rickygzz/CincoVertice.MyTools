using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CincoVertice.MyTools.Ui.Shell.Interfaces;
using CincoVertice.MyTools.Ui.Shell.Models;

namespace CincoVertice.MyTools.Ui.Shell.Services;

/// <summary>
///     The releases of a public GitHub repository, from its REST API, which needs no sign-in for them. Its latest
///     release is the newest one that isn't a draft or a pre-release, and its tag is the version (v0.0.6).
/// </summary>
public sealed class GitHubReleaseSource : IReleaseSource
{
    private readonly HttpClient _http;
    private readonly Uri _latestUrl;

    /// <param name="http">Sends the request; GitHub refuses one without a User-Agent, so this adds one.</param>
    /// <param name="repositoryUrl">
    ///     The repository's page, e.g. https://github.com/rickygzz/CincoVertice.MyTools.
    /// </param>
    /// <param name="appVersion">The version asking, for the User-Agent.</param>
    /// <exception cref="ArgumentException">It isn't a GitHub repository's page.</exception>
    public GitHubReleaseSource(HttpClient http, string repositoryUrl, Version appVersion)
    {
        (string owner, string repository) = ParseRepository(repositoryUrl);

        _http = http;
        _latestUrl = new Uri($"https://api.github.com/repos/{owner}/{repository}/releases/latest");
        ReleasesPageUrl = $"https://github.com/{owner}/{repository}/releases";

        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MyTools", AppVersion.Format(appVersion)));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public string ReleasesPageUrl { get; }

    public async Task<AppReleaseModel?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await _http.GetAsync(_latestUrl, cancellationToken);

            // What it answers when there's no release yet
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new UpdateCheckException($"GitHub answered {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            await using Stream body = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument json = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);

            return Read(json.RootElement);
        }
        catch (HttpRequestException ex)
        {
            throw new UpdateCheckException($"GitHub can't be reached: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new UpdateCheckException("GitHub didn't answer in time.", ex);
        }
        catch (JsonException ex)
        {
            throw new UpdateCheckException("GitHub's answer can't be read.", ex);
        }
    }

    /// <summary>
    ///     The owner and the name in https://github.com/owner/name, with or without a / or .git at the end.
    /// </summary>
    private static (string Owner, string Repository) ParseRepository(string repositoryUrl)
    {
        if (Uri.TryCreate(repositoryUrl, UriKind.Absolute, out Uri? url)
            && url.Scheme == Uri.UriSchemeHttps
            && url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
            && url.AbsolutePath.Trim('/').Split('/') is [{ Length: > 0 } owner, { Length: > 0 } name])
        {
            return (owner, name.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name);
        }

        throw new ArgumentException(
            $"Not a GitHub repository's page (https://github.com/owner/name): {repositoryUrl}",
            nameof(repositoryUrl));
    }

    private static AppReleaseModel Read(JsonElement release)
    {
        string? tag = ReadText(release, "tag_name");
        string? page = ReadText(release, "html_url");

        if (AppVersion.Parse(tag) is not { } version || string.IsNullOrEmpty(page))
        {
            throw new UpdateCheckException($"The latest release has no version in its tag ({tag ?? "none"}).");
        }

        return new AppReleaseModel(version, page);
    }

    /// <summary>The text in a property of an object; null when it isn't one, or the property isn't text.</summary>
    private static string? ReadText(JsonElement json, string name) =>
        json.ValueKind == JsonValueKind.Object
        && json.TryGetProperty(name, out JsonElement value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
