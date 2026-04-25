using System.Net;
using System.Security.Claims;
using System.Text;
using Moq;
using Moq.Protected;
using Xunit;

namespace ServerSpinner.Tests;

public class CookieAuthStateProviderTests
{
    private static HttpClient MakeClient(HttpStatusCode status, string? json = null)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = json is not null
                    ? new StringContent(json, Encoding.UTF8, "application/json")
                    : new StringContent("")
            });
        var client = new HttpClient(handler.Object) { BaseAddress = new Uri("https://localhost/") };
        return client;
    }

    private static HttpClient MakeThrowingClient()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        return new HttpClient(handler.Object) { BaseAddress = new Uri("https://localhost/") };
    }

    // ── GetAuthenticationStateAsync ───────────────────────────────────────────

    [Fact]
    public async Task Given_ValidUserResponse_When_GetAuthenticationStateAsync_Then_ReturnsAuthenticatedState()
    {
        var json = """{"id":"user-123","displayName":"TestUser"}""";
        var provider = new CookieAuthStateProvider(MakeClient(HttpStatusCode.OK, json));

        var state = await provider.GetAuthenticationStateAsync();

        Assert.True(state.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task Given_ValidUserResponse_When_GetAuthenticationStateAsync_Then_ClaimsContainNameIdentifier()
    {
        var json = """{"id":"user-123","displayName":"TestUser"}""";
        var provider = new CookieAuthStateProvider(MakeClient(HttpStatusCode.OK, json));

        var state = await provider.GetAuthenticationStateAsync();

        var idClaim = state.User.FindFirst(ClaimTypes.NameIdentifier);
        Assert.NotNull(idClaim);
        Assert.Equal("user-123", idClaim!.Value);
    }

    [Fact]
    public async Task Given_ValidUserResponse_When_GetAuthenticationStateAsync_Then_ClaimsContainDisplayName()
    {
        var json = """{"id":"user-123","displayName":"TestUser"}""";
        var provider = new CookieAuthStateProvider(MakeClient(HttpStatusCode.OK, json));

        var state = await provider.GetAuthenticationStateAsync();

        var nameClaim = state.User.FindFirst(ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal("TestUser", nameClaim!.Value);
    }

    [Fact]
    public async Task Given_401Response_When_GetAuthenticationStateAsync_Then_ReturnsAnonymousState()
    {
        var provider = new CookieAuthStateProvider(MakeClient(HttpStatusCode.Unauthorized));

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task Given_HttpException_When_GetAuthenticationStateAsync_Then_ReturnsAnonymousState()
    {
        var provider = new CookieAuthStateProvider(MakeThrowingClient());

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task Given_NullBodyResponse_When_GetAuthenticationStateAsync_Then_ReturnsAnonymousState()
    {
        var provider = new CookieAuthStateProvider(MakeClient(HttpStatusCode.OK, "null"));

        var state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task Given_401Response_When_GetAuthenticationStateAsync_Then_UserHasNoClaims()
    {
        var provider = new CookieAuthStateProvider(MakeClient(HttpStatusCode.Unauthorized));

        var state = await provider.GetAuthenticationStateAsync();

        Assert.Empty(state.User.Claims);
    }

    [Fact]
    public async Task Given_ValidUserResponse_When_GetAuthenticationStateAsync_Then_AuthenticationTypeIsCookie()
    {
        var json = """{"id":"user-abc","displayName":"Streamer"}""";
        var provider = new CookieAuthStateProvider(MakeClient(HttpStatusCode.OK, json));

        var state = await provider.GetAuthenticationStateAsync();

        Assert.Equal("cookie", state.User.Identity!.AuthenticationType);
    }
}