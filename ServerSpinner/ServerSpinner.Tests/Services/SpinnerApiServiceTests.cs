using System.Net;
using System.Text;
using System.Text.Json;
using Moq;
using Moq.Protected;
using ServerSpinner.Core.Models;
using ServerSpinner.Services;
using Xunit;

namespace ServerSpinner.Tests.Services;

public class SpinnerApiServiceTests
{
    private const string SslBase = "https://api.streamersonglist.com/v1/streamers";

    private static (HttpClient client, Mock<HttpMessageHandler> handler) MakeClient(
        HttpStatusCode status = HttpStatusCode.OK,
        string? json = null,
        Action<HttpRequestMessage>? captureRequest = null)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captureRequest?.Invoke(req))
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = json is not null
                    ? new StringContent(json, Encoding.UTF8, "application/json")
                    : new StringContent("")
            });
        return (new HttpClient(handler.Object), handler);
    }

    private static SpinnerApiService MakeService(HttpClient? authHttp = null, HttpClient? externalHttp = null)
    {
        return new SpinnerApiService(authHttp ?? new HttpClient(), externalHttp ?? new HttpClient());
    }

    // ── FetchQueueAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task Given_ValidStreamer_When_FetchQueueAsync_Then_ReturnsQueueItems()
    {
        var json = JsonSerializer.Serialize(new QueueResponse
        {
            List =
            [
                new SpinnerQueueItem
                {
                    Song = new SpinnerSong { Id = 1, Artist = "Artist", Title = "Track" },
                    Requests = [new SpinnerRequest { Name = "User" }],
                    Position = 1
                }
            ]
        });
        var (external, _) = MakeClient(json: json);
        var service = MakeService(externalHttp: external);

        var result = await service.FetchQueueAsync("teststreamer");

        Assert.Single(result);
        Assert.Equal("Artist", result[0].Song.Artist);
        Assert.Equal("Track", result[0].Song.Title);
    }

    [Fact]
    public async Task Given_HttpError_When_FetchQueueAsync_Then_ReturnsEmptyList()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        var service = MakeService(externalHttp: new HttpClient(handler.Object));

        var result = await service.FetchQueueAsync("teststreamer");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Given_NullListInResponse_When_FetchQueueAsync_Then_ReturnsEmptyList()
    {
        var (external, _) = MakeClient(json: "{\"list\":null}");
        var service = MakeService(externalHttp: external);

        var result = await service.FetchQueueAsync("teststreamer");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Given_StreamerWithUppercase_When_FetchQueueAsync_Then_UsesLowercaseInUrl()
    {
        HttpRequestMessage? captured = null;
        var (external, _) = MakeClient(json: "{\"list\":[]}", captureRequest: r => captured = r);
        var service = MakeService(externalHttp: external);

        await service.FetchQueueAsync("MyStreamer");

        Assert.NotNull(captured);
        Assert.Contains("/mystreamer/queue", captured!.RequestUri!.ToString());
    }

    [Fact]
    public async Task Given_StreamerWithLeadingSpaces_When_FetchQueueAsync_Then_TrimsAndEncodes()
    {
        HttpRequestMessage? captured = null;
        var (external, _) = MakeClient(json: "{\"list\":[]}", captureRequest: r => captured = r);
        var service = MakeService(externalHttp: external);

        await service.FetchQueueAsync("  streamer  ");

        Assert.NotNull(captured);
        Assert.Contains("/streamer/queue", captured!.RequestUri!.ToString());
    }

    [Fact]
    public async Task Given_ValidStreamer_When_FetchQueueAsync_Then_CallsCorrectEndpoint()
    {
        HttpRequestMessage? captured = null;
        var (external, _) = MakeClient(json: "{\"list\":[]}", captureRequest: r => captured = r);
        var service = MakeService(externalHttp: external);

        await service.FetchQueueAsync("streamer");

        Assert.NotNull(captured);
        Assert.Equal($"{SslBase}/streamer/queue", captured!.RequestUri!.ToString());
    }

    // ── FetchPlayHistoryAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task Given_ValidStreamer_When_FetchPlayHistoryAsync_Then_ReturnsHistoryItems()
    {
        var json = JsonSerializer.Serialize(new PlayHistoryResponse
        {
            Items =
            [
                new PlayHistoryItem { Song = new SpinnerSong { Artist = "Band", Title = "Song" } }
            ]
        });
        var (external, _) = MakeClient(json: json);
        var service = MakeService(externalHttp: external);

        var result = await service.FetchPlayHistoryAsync("streamer");

        Assert.Single(result);
        Assert.Equal("Band", result[0].Song!.Artist);
    }

    [Fact]
    public async Task Given_HttpError_When_FetchPlayHistoryAsync_Then_ReturnsEmptyList()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        var service = MakeService(externalHttp: new HttpClient(handler.Object));

        var result = await service.FetchPlayHistoryAsync("streamer");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Given_NullItemsInResponse_When_FetchPlayHistoryAsync_Then_ReturnsEmptyList()
    {
        var (external, _) = MakeClient(json: "{\"items\":null}");
        var service = MakeService(externalHttp: external);

        var result = await service.FetchPlayHistoryAsync("streamer");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Given_CustomPeriod_When_FetchPlayHistoryAsync_Then_IncludesPeriodInUrl()
    {
        HttpRequestMessage? captured = null;
        var (external, _) = MakeClient(json: "{\"items\":[]}", captureRequest: r => captured = r);
        var service = MakeService(externalHttp: external);

        await service.FetchPlayHistoryAsync("streamer", "month");

        Assert.NotNull(captured);
        Assert.Contains("period=month", captured!.RequestUri!.Query);
    }

    [Fact]
    public async Task Given_DefaultPeriod_When_FetchPlayHistoryAsync_Then_UsesWeekInUrl()
    {
        HttpRequestMessage? captured = null;
        var (external, _) = MakeClient(json: "{\"items\":[]}", captureRequest: r => captured = r);
        var service = MakeService(externalHttp: external);

        await service.FetchPlayHistoryAsync("streamer");

        Assert.NotNull(captured);
        Assert.Contains("period=week", captured!.RequestUri!.Query);
    }

    [Fact]
    public async Task Given_ValidStreamer_When_FetchPlayHistoryAsync_Then_CallsPlayHistoryEndpoint()
    {
        HttpRequestMessage? captured = null;
        var (external, _) = MakeClient(json: "{\"items\":[]}", captureRequest: r => captured = r);
        var service = MakeService(externalHttp: external);

        await service.FetchPlayHistoryAsync("streamer");

        Assert.NotNull(captured);
        Assert.Contains("/streamer/playHistory", captured!.RequestUri!.ToString());
    }

    // ── FetchConfigAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task Given_ValidUrl_When_FetchConfigAsync_Then_ReturnsConfig()
    {
        var json = JsonSerializer.Serialize(new SpinnerConfig { Debug = true });
        var (auth, _) = MakeClient(json: json);
        auth.BaseAddress = new Uri("https://api.example.com/");
        var service = MakeService(auth);

        var result = await service.FetchConfigAsync("https://api.example.com/config");

        Assert.NotNull(result);
        Assert.True(result!.Debug);
    }

    [Fact]
    public async Task Given_HttpError_When_FetchConfigAsync_Then_ReturnsNull()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        var service = MakeService(new HttpClient(handler.Object));

        var result = await service.FetchConfigAsync("https://api.example.com/config");

        Assert.Null(result);
    }

    [Fact]
    public async Task Given_InvalidJson_When_FetchConfigAsync_Then_ReturnsNull()
    {
        var (auth, _) = MakeClient(json: "not-valid-json");
        auth.BaseAddress = new Uri("https://api.example.com/");
        var service = MakeService(auth);

        var result = await service.FetchConfigAsync("https://api.example.com/config");

        Assert.Null(result);
    }

    [Fact]
    public async Task Given_FetchConfigAsync_When_Called_Then_UsesAuthHttpClient()
    {
        var json = JsonSerializer.Serialize(new SpinnerConfig());
        HttpRequestMessage? authCaptured = null;
        HttpRequestMessage? externalCaptured = null;

        var (auth, _) = MakeClient(json: json, captureRequest: r => authCaptured = r);
        auth.BaseAddress = new Uri("https://api.example.com/");
        var (external, _) = MakeClient(captureRequest: r => externalCaptured = r);
        var service = MakeService(auth, external);

        await service.FetchConfigAsync("https://api.example.com/config");

        Assert.NotNull(authCaptured);
        Assert.Null(externalCaptured);
    }
}