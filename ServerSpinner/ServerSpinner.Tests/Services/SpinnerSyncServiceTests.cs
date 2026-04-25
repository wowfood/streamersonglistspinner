using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using ServerSpinner.Services;
using Xunit;

namespace ServerSpinner.Tests.Services;

public class SpinnerSyncServiceTests
{
    private static (HttpClient client, Mock<HttpMessageHandler> handler, List<HttpRequestMessage> captured)
        MakeClient(HttpStatusCode status = HttpStatusCode.OK, string? json = null)
    {
        var captured = new List<HttpRequestMessage>();
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured.Add(req))
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = json is not null
                    ? new StringContent(json, Encoding.UTF8, "application/json")
                    : new StringContent("")
            });
        return (new HttpClient(handler.Object), handler, captured);
    }

    // ── InitAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_NegotiateReturnsFailure_When_InitAsync_Then_CompletesWithoutError()
    {
        var (http, _, _) = MakeClient(HttpStatusCode.ServiceUnavailable);
        await using var service = new SpinnerSyncService(http);

        // Should not throw
        await service.InitAsync("https://api.example.com", "streamer-id");
    }

    [Fact]
    public async Task Given_NegotiateReturnsFailure_When_InitAsync_Then_PostsToNegotiateEndpoint()
    {
        var (http, _, captured) = MakeClient(HttpStatusCode.ServiceUnavailable);
        await using var service = new SpinnerSyncService(http);

        await service.InitAsync("https://api.example.com", "streamer-id");

        Assert.Single(captured);
        Assert.Equal("https://api.example.com/api/negotiate", captured[0].RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, captured[0].Method);
    }

    [Fact]
    public async Task Given_NegotiateReturnsNullBody_When_InitAsync_Then_CompletesWithoutError()
    {
        var (http, _, _) = MakeClient(HttpStatusCode.OK, "null");
        await using var service = new SpinnerSyncService(http);

        await service.InitAsync("https://api.example.com", "streamer-id");
    }

    // ── SendAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_NoStreamerId_When_SendAsync_Then_MakesNoHttpCall()
    {
        var (http, _, captured) = MakeClient();
        await using var service = new SpinnerSyncService(http);

        await service.SendAsync("test_type", new { value = 1 });

        Assert.Empty(captured);
    }

    [Fact]
    public async Task Given_EmptyApiBaseUrl_When_SendAsync_Then_MakesNoHttpCall()
    {
        var (http, _, captured) = MakeClient();
        await using var service = new SpinnerSyncService(http);

        // _streamerId and _apiBaseUrl are both empty by default — neither populated
        await service.SendAsync("test_type", new { });

        Assert.Empty(captured);
    }

    // ── DisposeAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_NoConnectionInitialised_When_DisposeAsync_Then_CompletesWithoutError()
    {
        var (http, _, _) = MakeClient();
        var service = new SpinnerSyncService(http);

        // No InitAsync called — _connection is null
        await service.DisposeAsync();
    }

    // ── MessageReceived event ─────────────────────────────────────────────────

    [Fact]
    public async Task Given_NewService_When_MessageReceivedSubscribed_Then_NoException()
    {
        var (http, _, _) = MakeClient();
        await using var service = new SpinnerSyncService(http);

        // Subscribing to the event should not throw
        service.MessageReceived += (_, _) => Task.CompletedTask;
    }
}