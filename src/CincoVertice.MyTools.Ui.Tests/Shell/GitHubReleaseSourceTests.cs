using System.Net;
using System.Text;
using CincoVertice.MyTools.Ui.Shell.Models;
using CincoVertice.MyTools.Ui.Shell.Services;
using Xunit;

namespace CincoVertice.MyTools.Ui.Tests.Shell;

public class GitHubReleaseSourceTests
{
    private readonly FakeHandler _handler = new();
    private readonly GitHubReleaseSource _source;

    public GitHubReleaseSourceTests()
    {
        _source = new GitHubReleaseSource(
            new HttpClient(_handler),
            "https://github.com/owner/repo",
            new Version(0, 0, 6));
    }

    [Theory]
    [InlineData("https://github.com/owner/repo")]
    [InlineData("https://github.com/owner/repo/")]
    [InlineData("https://github.com/owner/repo.git")]
    [InlineData("https://GitHub.com/owner/repo")]
    public async Task RepositoryUrl_AsGitWritesIt_AsksThatRepository(string repositoryUrl)
    {
        // Arrange
        var source = new GitHubReleaseSource(new HttpClient(_handler), repositoryUrl, new Version(0, 0, 6));
        _handler.Answer(HttpStatusCode.NotFound, "{}");

        // Act
        await source.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            "https://api.github.com/repos/owner/repo/releases/latest",
            Assert.Single(_handler.Requests).RequestUri!.ToString());
        Assert.Equal("https://github.com/owner/repo/releases", source.ReleasesPageUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("github.com/owner/repo")]
    [InlineData("http://github.com/owner/repo")]
    [InlineData("https://gitlab.com/owner/repo")]
    [InlineData("https://github.com/owner")]
    [InlineData("https://github.com/owner/repo/releases")]
    public void RepositoryUrl_ThatIsntAGitHubRepository_IsRefused(string repositoryUrl)
    {
        // Act & Assert
        var error = Assert.Throws<ArgumentException>(
            () => new GitHubReleaseSource(new HttpClient(_handler), repositoryUrl, new Version(0, 0, 6)));
        Assert.Contains("https://github.com/owner/name", error.Message);
    }

    [Fact]
    public async Task GetLatest_ReadsTheTagAndThePage()
    {
        // Arrange: as GitHub answers, with much more in it
        _handler.Answer(
            HttpStatusCode.OK,
            """
            {
                "tag_name": "v0.0.7",
                "name": "MyTools v0.0.7",
                "html_url": "https://github.com/owner/repo/releases/tag/v0.0.7",
                "draft": false
            }
            """);

        // Act
        AppReleaseModel? release = await _source.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            new AppReleaseModel(new Version(0, 0, 7), "https://github.com/owner/repo/releases/tag/v0.0.7"),
            release);
    }

    [Fact]
    public async Task GetLatest_AsksTheRepositorysLatestRelease_SayingWhoAsks()
    {
        // Arrange
        _handler.Answer(HttpStatusCode.OK, """{ "tag_name": "v0.0.7", "html_url": "https://x" }""");

        // Act
        await _source.GetLatestAsync(TestContext.Current.CancellationToken);

        // Assert: GitHub refuses a request without a User-Agent
        HttpRequestMessage request = Assert.Single(_handler.Requests);
        Assert.Equal("https://api.github.com/repos/owner/repo/releases/latest", request.RequestUri!.ToString());
        Assert.Equal("MyTools/0.0.6", request.Headers.UserAgent.ToString());
        Assert.Equal("https://github.com/owner/repo/releases", _source.ReleasesPageUrl);
    }

    [Fact]
    public async Task GetLatest_WithNoReleaseYet_GivesNothing()
    {
        // Arrange
        _handler.Answer(HttpStatusCode.NotFound, """{ "message": "Not Found" }""");

        // Act & Assert
        Assert.Null(await _source.GetLatestAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, """{ "message": "API rate limit exceeded" }""", "403")]
    [InlineData(HttpStatusCode.OK, "not json", "can't be read")]
    [InlineData(HttpStatusCode.OK, """{ "tag_name": "latest", "html_url": "https://x" }""", "latest")]
    [InlineData(HttpStatusCode.OK, """{ "html_url": "https://x" }""", "none")]
    [InlineData(HttpStatusCode.OK, """[]""", "none")]
    public async Task GetLatest_WhatCantBeUsed_SaysWhy(HttpStatusCode status, string body, string reason)
    {
        // Arrange
        _handler.Answer(status, body);

        // Act
        var error = await Assert.ThrowsAsync<UpdateCheckException>(
            () => _source.GetLatestAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(reason, error.Message);
    }

    [Fact]
    public async Task GetLatest_WithoutTheNetwork_SaysSo()
    {
        // Arrange
        _handler.Error = new HttpRequestException("No such host is known.");

        // Act
        var error = await Assert.ThrowsAsync<UpdateCheckException>(
            () => _source.GetLatestAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains("No such host is known.", error.Message);
    }

    [Fact]
    public async Task GetLatest_WhenItTakesTooLong_SaysSo()
    {
        // Arrange: what HttpClient throws when its Timeout passes
        _handler.Error = new TaskCanceledException("The request was canceled due to the configured timeout.");

        // Act
        var error = await Assert.ThrowsAsync<UpdateCheckException>(
            () => _source.GetLatestAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains("in time", error.Message);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        private HttpStatusCode _status = HttpStatusCode.OK;
        private string _body = "{}";

        public List<HttpRequestMessage> Requests { get; } = [];

        public Exception? Error { get; set; }

        public void Answer(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);

            if (Error is not null)
            {
                throw Error;
            }

            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
