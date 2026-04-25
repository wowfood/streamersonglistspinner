using System.Net;
using Moq;
using Moq.Protected;
using Xunit;

namespace ServerSpinner.Tests;

public class CookieCredentialHandlerTests
{
    private static (HttpClient client, Mock<HttpMessageHandler> inner, List<HttpRequestMessage> captured)
        MakeClient(HttpStatusCode status = HttpStatusCode.OK)
    {
        var captured = new List<HttpRequestMessage>();
        var inner = new Mock<HttpMessageHandler>();
        inner.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured.Add(req))
            .ReturnsAsync(new HttpResponseMessage(status));

        var handler = new CookieCredentialHandler { InnerHandler = inner.Object };
        return (new HttpClient(handler), inner, captured);
    }

    // ── SendAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_Request_When_SendAsync_Then_DelegatesToInnerHandler()
    {
        var (client, inner, _) = MakeClient();

        await client.GetAsync("https://example.com/test", TestContext.Current.CancellationToken);

        inner.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task Given_Request_When_SendAsync_Then_ReturnsResponseFromInnerHandler()
    {
        var (client, _, _) = MakeClient();

        var response = await client.GetAsync("https://example.com/test", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Given_InnerHandlerReturnsNotFound_When_SendAsync_Then_PropagatesStatus()
    {
        var (client, _, _) = MakeClient(HttpStatusCode.NotFound);

        var response = await client.GetAsync("https://example.com/test", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Given_MultipleRequests_When_SendAsync_Then_EachRequestDelegatesToInner()
    {
        var (client, inner, _) = MakeClient();

        await client.GetAsync("https://example.com/a", TestContext.Current.CancellationToken);
        await client.GetAsync("https://example.com/b", TestContext.Current.CancellationToken);

        inner.Protected().Verify("SendAsync", Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task Given_PostRequest_When_SendAsync_Then_DelegatesToInnerHandler()
    {
        var (client, inner, _) = MakeClient();

        await client.PostAsync("https://example.com/test", new StringContent("{}"),
            TestContext.Current.CancellationToken);

        inner.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }
}