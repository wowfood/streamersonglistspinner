using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using ServerSpinner.Functions.Helpers;
using ServerSpinner.Functions.Services;
using Xunit;

namespace ServerSpinner.Functions.Tests.Services;

public class AuthServiceTests
{
    private const string ValidSecret = "this-is-a-test-secret-that-is-long-enough-for-hmac-sha256";

    private static AuthService CreateService(string secret = ValidSecret)
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["JwtSecret"]).Returns(secret);
        return new AuthService(config.Object);
    }

    private static HttpRequestData CreateRequest(string? cookieHeader = null)
    {
        var mockContext = new Mock<FunctionContext>();
        var mockReq = new Mock<HttpRequestData>(mockContext.Object);
        var headers = new HttpHeadersCollection();
        if (cookieHeader is not null)
            headers.Add("Cookie", cookieHeader);
        mockReq.Setup(r => r.Headers).Returns(headers);
        return mockReq.Object;
    }

    private static string CreateToken(string streamerId = "test-streamer-id", string displayName = "TestStreamer")
    {
        return JwtHelper.Create(streamerId, displayName, ValidSecret);
    }

    // ── Authenticate ─────────────────────────────────────────────────────────

    [Fact]
    public void Given_RequestWithValidAuthCookie_When_Authenticate_Then_ReturnsPrincipal()
    {
        var token = CreateToken();
        var req = CreateRequest($"ss_auth={token}");

        var principal = CreateService().Authenticate(req);

        Assert.NotNull(principal);
    }

    [Fact]
    public void Given_RequestWithValidAuthCookie_When_Authenticate_Then_PrincipalHasCorrectStreamerId()
    {
        var streamerId = "550e8400-e29b-41d4-a716-446655440000";
        var token = CreateToken(streamerId);
        var req = CreateRequest($"ss_auth={token}");

        var principal = CreateService().Authenticate(req)!;
        var (id, _) = JwtHelper.GetClaims(principal);

        Assert.Equal(streamerId, id);
    }

    [Fact]
    public void Given_RequestWithNoCookieHeader_When_Authenticate_Then_ReturnsNull()
    {
        var req = CreateRequest();

        var principal = CreateService().Authenticate(req);

        Assert.Null(principal);
    }

    [Fact]
    public void Given_RequestWithCookieContainingInvalidToken_When_Authenticate_Then_ReturnsNull()
    {
        var req = CreateRequest("ss_auth=not-a-valid-jwt");

        var principal = CreateService().Authenticate(req);

        Assert.Null(principal);
    }

    [Fact]
    public void Given_RequestWithMultipleCookies_And_ValidAuthCookie_When_Authenticate_Then_ReturnsPrincipal()
    {
        var token = CreateToken();
        var req = CreateRequest($"session=abc123; ss_auth={token}; theme=dark");

        var principal = CreateService().Authenticate(req);

        Assert.NotNull(principal);
    }

    [Fact]
    public void Given_RequestWithValidToken_And_WrongSecret_When_Authenticate_Then_ReturnsNull()
    {
        var token = CreateToken();
        var req = CreateRequest($"ss_auth={token}");

        var principal = CreateService("a-completely-different-secret-key-for-validation").Authenticate(req);

        Assert.Null(principal);
    }

    [Fact]
    public void Given_RequestWithOtherCookiesButNoAuthCookie_When_Authenticate_Then_ReturnsNull()
    {
        var req = CreateRequest("session=abc; theme=dark");

        var principal = CreateService().Authenticate(req);

        Assert.Null(principal);
    }
}