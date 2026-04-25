using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using ServerSpinner.Functions.Entities;
using ServerSpinner.Functions.Data;
using ServerSpinner.Functions.Functions;
using ServerSpinner.Functions.Services;
using ServerSpinner.Functions.Tests.Helpers;
using Xunit;

namespace ServerSpinner.Functions.Tests.Functions;

public class AuthFunctionTests
{
    private const string JwtSecret = "this-is-a-test-secret-that-is-long-enough-for-hmac-sha256";
    private const string ClientId = "test-client-id";
    private const string ClientSecret = "test-client-secret";
    private const string RedirectUri = "https://example.com/callback";
    private const string WebBaseUrl = "https://example.com/";

    private static AppDbContext CreateDb()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static IConfiguration CreateConfig()
    {
        return Mock.Of<IConfiguration>(c =>
            c["Twitch:ClientId"] == ClientId &&
            c["Twitch:ClientSecret"] == ClientSecret &&
            c["Twitch:RedirectUri"] == RedirectUri &&
            c["JwtSecret"] == JwtSecret &&
            c["WebBaseUrl"] == WebBaseUrl);
    }

    private static ClaimsPrincipal CreatePrincipal(
        string streamerId = "550e8400-e29b-41d4-a716-446655440000",
        string displayName = "TestStreamer")
    {
        var claims = new[] { new Claim("sub", streamerId), new Claim("name", displayName) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static AuthFunction CreateFunction(
        AppDbContext db,
        IAuthService authService,
        IHttpClientFactory? httpFactory = null,
        IConfiguration? config = null)
    {
        return new AuthFunction(db, httpFactory ?? Mock.Of<IHttpClientFactory>(), config ?? CreateConfig(),
            NullLogger<AuthFunction>.Instance, authService);
    }

    // ── GetUser ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_UnauthenticatedRequest_When_GetUser_Then_Returns401()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.GetUser(req);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Given_AuthenticatedRequest_When_GetUser_Then_Returns200()
    {
        await using var db = CreateDb();
        var principal = CreatePrincipal();
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.GetUser(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Given_AuthenticatedRequest_When_GetUser_Then_ResponseBodyContainsDisplayName()
    {
        await using var db = CreateDb();
        var principal = CreatePrincipal(displayName: "StreamerName");
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.GetUser(req);
        var body = ((FakeHttpResponseData)response).GetBodyAsString();

        Assert.Contains("StreamerName", body);
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Given_LoginRequest_When_Login_Then_Returns302()
    {
        using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = function.Login(req);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    }

    [Fact]
    public void Given_LoginRequest_When_Login_Then_LocationHeaderContainsTwitchOAuthUrlWithClientId()
    {
        using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = function.Login(req);

        var location = response.Headers.GetValues("Location").First();
        Assert.Contains("id.twitch.tv/oauth2/authorize", location);
        Assert.Contains(ClientId, location);
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    [Fact]
    public void Given_LogoutRequest_When_Logout_Then_Returns302()
    {
        using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = function.Logout(req);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
    }

    [Fact]
    public void Given_LogoutRequest_When_Logout_Then_SetCookieHeaderClearsAuthCookie()
    {
        using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = function.Logout(req);

        var setCookie = response.Headers.GetValues("Set-Cookie").First();
        Assert.Contains("ss_auth=", setCookie);
        Assert.Contains("Max-Age=0", setCookie);
    }

    [Fact]
    public void Given_LogoutRequest_When_Logout_Then_RedirectsToWebBaseUrl()
    {
        using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = function.Logout(req);

        var location = response.Headers.GetValues("Location").First();
        Assert.Equal(WebBaseUrl, location);
    }

    // ── Callback ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_CallbackWithMissingCode_When_Callback_Then_Returns400()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create(
            url: "https://example.com/api/auth/twitch/callback");
        var function = CreateFunction(db, authService);

        var response = await function.Callback(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Given_CallbackWithCodeAndTokenExchangeFails_When_Callback_Then_Returns500()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));
        var (req, _) = MockHttpRequestFactory.Create(
            url: "https://example.com/api/auth/twitch/callback?code=abc123");
        var function = CreateFunction(db, authService, factory.Object);

        var response = await function.Callback(req);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Given_CallbackWithCodeAndUserInfoFails_When_Callback_Then_Returns500()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var tokenJson = JsonSerializer.Serialize(new
        {
            access_token = "at",
            refresh_token = "rt",
            expires_in = 3600
        });

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(tokenJson) });
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));
        var (req, _) = MockHttpRequestFactory.Create(
            url: "https://example.com/api/auth/twitch/callback?code=abc123");
        var function = CreateFunction(db, authService, factory.Object);

        var response = await function.Callback(req);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Given_CallbackWithValidCode_And_NewStreamer_When_Callback_Then_CreatesStreamerAndRedirects()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (factory, _) = CreateSuccessfulTwitchHttpFactory("twitch-123", "StreamerName");
        var (req, _) = MockHttpRequestFactory.Create(
            url: "https://example.com/api/auth/twitch/callback?code=abc123");
        var function = CreateFunction(db, authService, factory);

        var response = await function.Callback(req);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var streamer = await db.Streamers.FirstOrDefaultAsync(s => s.TwitchUserId == "twitch-123",
            TestContext.Current.CancellationToken);
        Assert.NotNull(streamer);
        Assert.Equal("StreamerName", streamer.DisplayName);
    }

    [Fact]
    public async Task Given_CallbackWithValidCode_And_ExistingStreamer_When_Callback_Then_UpdatesStreamer()
    {
        await using var db = CreateDb();
        db.Streamers.Add(new Streamer
        {
            Id = Guid.NewGuid(),
            TwitchUserId = "twitch-123",
            DisplayName = "OldName",
            AccessToken = "old-token",
            RefreshToken = "old-refresh",
            TokenExpiry = DateTime.UtcNow.AddHours(-1),
            Settings = new StreamerSettings()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var authService = Mock.Of<IAuthService>();
        var (factory, _) = CreateSuccessfulTwitchHttpFactory("twitch-123", "NewName", "new-at");
        var (req, _) = MockHttpRequestFactory.Create(
            url: "https://example.com/api/auth/twitch/callback?code=abc123");
        var function = CreateFunction(db, authService, factory);

        var response = await function.Callback(req);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var streamer = await db.Streamers.FirstOrDefaultAsync(s => s.TwitchUserId == "twitch-123",
            TestContext.Current.CancellationToken);
        Assert.Equal("NewName", streamer!.DisplayName);
        Assert.Equal("new-at", streamer.AccessToken);
    }

    [Fact]
    public async Task Given_CallbackWithValidCode_And_NewStreamer_When_Callback_Then_SetsAuthCookie()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (factory, _) = CreateSuccessfulTwitchHttpFactory("twitch-xyz", "CoolStreamer");
        var (req, _) = MockHttpRequestFactory.Create(
            url: "https://example.com/api/auth/twitch/callback?code=abc123");
        var function = CreateFunction(db, authService, factory);

        var response = await function.Callback(req);

        var setCookie = response.Headers.GetValues("Set-Cookie").First();
        Assert.Contains("ss_auth=", setCookie);
        Assert.Contains("HttpOnly", setCookie);
    }

    [Fact]
    public async Task Given_CallbackWithValidCodeAndEmptyUserData_When_Callback_Then_Returns500()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var tokenJson = JsonSerializer.Serialize(new
        {
            access_token = "at",
            refresh_token = "rt",
            expires_in = 3600
        });
        var emptyUserJson = JsonSerializer.Serialize(new { data = Array.Empty<object>() });

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(tokenJson) });
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(emptyUserJson) });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));
        var (req, _) = MockHttpRequestFactory.Create(
            url: "https://example.com/api/auth/twitch/callback?code=abc123");
        var function = CreateFunction(db, authService, factory.Object);

        var response = await function.Callback(req);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Given_CallbackWithValidCode_When_Callback_Then_RedirectsToWebBaseUrl()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (factory, _) = CreateSuccessfulTwitchHttpFactory("twitch-456", "SomeStreamer");
        var (req, _) = MockHttpRequestFactory.Create(
            url: "https://example.com/api/auth/twitch/callback?code=abc123");
        var function = CreateFunction(db, authService, factory);

        var response = await function.Callback(req);

        var location = response.Headers.GetValues("Location").First();
        Assert.Equal(WebBaseUrl, location);
    }

    [Fact]
    public void Given_LogoutRequestViaHttp_When_Logout_Then_CookieDoesNotContainSecureFlag()
    {
        using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create(url: "http://example.com/api/test");
        var function = CreateFunction(db, authService);

        var response = function.Logout(req);

        var setCookie = response.Headers.GetValues("Set-Cookie").First();
        Assert.DoesNotContain("; Secure", setCookie);
    }

    [Fact]
    public async Task Given_CallbackWithValidCodeViaHttp_When_Callback_Then_CookieDoesNotContainSecureFlag()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (factory, _) = CreateSuccessfulTwitchHttpFactory("twitch-789", "HttpStreamer");
        var (req, _) = MockHttpRequestFactory.Create(
            url: "http://example.com/api/auth/twitch/callback?code=abc123");
        var function = CreateFunction(db, authService, factory);

        var response = await function.Callback(req);

        var setCookie = response.Headers.GetValues("Set-Cookie").First();
        Assert.DoesNotContain("; Secure", setCookie);
    }

    private static (IHttpClientFactory Factory, Mock<HttpMessageHandler> Handler)
        CreateSuccessfulTwitchHttpFactory(
            string twitchUserId,
            string displayName,
            string accessToken = "at")
    {
        var tokenJson = JsonSerializer.Serialize(new
        {
            access_token = accessToken,
            refresh_token = "rt",
            expires_in = 3600
        });
        var userJson = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new { id = twitchUserId, login = displayName.ToLower(), display_name = displayName }
            }
        });

        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(tokenJson) });
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(userJson) });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));
        return (factory.Object, handler);
    }
}