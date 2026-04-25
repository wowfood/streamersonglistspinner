using System.Net;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ServerSpinner.Functions.Contracts;
using ServerSpinner.Functions.Entities;
using ServerSpinner.Functions.Data;
using ServerSpinner.Functions.Functions;
using ServerSpinner.Functions.Services;
using ServerSpinner.Functions.Tests.Helpers;
using Xunit;

namespace ServerSpinner.Functions.Tests.Functions;

public class SpinnerConfigFunctionTests
{
    private static readonly Guid StreamerId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    private static AppDbContext CreateDb()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static ClaimsPrincipal CreatePrincipal(string streamerId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", streamerId)], "test"));
    }

    private static SpinnerConfigFunction CreateFunction(AppDbContext db, IAuthService authService,
        ISpinnerConfigMapper? mapper = null)
    {
        return new SpinnerConfigFunction(db, authService,
            mapper ?? new SpinnerConfigMapper(NullLogger<SpinnerConfigMapper>.Instance));
    }

    // ── GetMyConfig ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_UnauthenticatedRequest_When_GetMyConfig_Then_Returns401()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.GetMyConfig(req);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Given_AuthenticatedWithInvalidGuidClaim_When_GetMyConfig_Then_Returns401()
    {
        await using var db = CreateDb();
        var principal = CreatePrincipal("not-a-guid");
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.GetMyConfig(req);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Given_AuthenticatedRequest_And_ExistingSettings_When_GetMyConfig_Then_Returns200()
    {
        await using var db = CreateDb();
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var principal = CreatePrincipal(StreamerId.ToString());
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.GetMyConfig(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Given_AuthenticatedRequest_And_NoSettings_When_GetMyConfig_Then_Returns200WithDefaults()
    {
        await using var db = CreateDb();
        var principal = CreatePrincipal(StreamerId.ToString());
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.GetMyConfig(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── GetConfigForOverlay ──────────────────────────────────────────────────

    [Fact]
    public async Task Given_ExistingStreamerId_When_GetConfigForOverlay_Then_Returns200()
    {
        await using var db = CreateDb();
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.GetConfigForOverlay(req, StreamerId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Given_UnknownStreamerId_When_GetConfigForOverlay_Then_Returns200WithDefaults()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.GetConfigForOverlay(req, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Given_NoAuthCookie_When_GetConfigForOverlay_Then_StillReturns200()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.GetConfigForOverlay(req, StreamerId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}