using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using ServerSpinner.Functions.Contracts;
using ServerSpinner.Core.Data;
using ServerSpinner.Functions.Entities;
using ServerSpinner.Functions.Data;
using ServerSpinner.Functions.Functions;
using ServerSpinner.Functions.Services;
using ServerSpinner.Functions.Tests.Helpers;
using Xunit;

namespace ServerSpinner.Functions.Tests.Functions;

public class SettingsFunctionTests
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

    private static SettingsFunction CreateFunction(AppDbContext db, IAuthService authService,
        ISettingsMapper? mapper = null)
    {
        return new SettingsFunction(db, authService, mapper ?? new SettingsMapper());
    }

    // ── Get ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_UnauthenticatedRequest_When_Get_Then_Returns401()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.Get(req);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Given_AuthenticatedWithInvalidGuidClaim_When_Get_Then_Returns401()
    {
        await using var db = CreateDb();
        var principal = CreatePrincipal("not-a-guid");
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.Get(req);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Given_AuthenticatedRequest_And_ExistingSettings_When_Get_Then_Returns200()
    {
        await using var db = CreateDb();
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId, DebugMode = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var principal = CreatePrincipal(StreamerId.ToString());
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.Get(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Given_AuthenticatedRequest_And_NoSettings_When_Get_Then_Returns200WithDefaults()
    {
        await using var db = CreateDb();
        var principal = CreatePrincipal(StreamerId.ToString());
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction(db, authService);

        var response = await function.Get(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_UnauthenticatedRequest_When_Save_Then_Returns401()
    {
        await using var db = CreateDb();
        var authService = Mock.Of<IAuthService>();
        var (req, _) = MockHttpRequestFactory.Create("{}");
        var function = CreateFunction(db, authService);

        var response = await function.Save(req);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Given_AuthenticatedWithInvalidGuidClaim_When_Save_Then_Returns401()
    {
        await using var db = CreateDb();
        var principal = CreatePrincipal("not-a-guid");
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var (req, _) = MockHttpRequestFactory.Create("{}");
        var function = CreateFunction(db, authService);

        var response = await function.Save(req);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Given_InvalidJsonBody_When_Save_Then_Returns400()
    {
        await using var db = CreateDb();
        var principal = CreatePrincipal(StreamerId.ToString());
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var (req, _) = MockHttpRequestFactory.Create("{not valid json}");
        var function = CreateFunction(db, authService);

        var response = await function.Save(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Given_NullJsonBody_When_Save_Then_Returns400()
    {
        await using var db = CreateDb();
        var principal = CreatePrincipal(StreamerId.ToString());
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var (req, _) = MockHttpRequestFactory.Create("null");
        var function = CreateFunction(db, authService);

        var response = await function.Save(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Given_ValidBody_And_NoExistingSettings_When_Save_Then_Returns200AndCreatesSettings()
    {
        await using var db = CreateDb();
        var principal = CreatePrincipal(StreamerId.ToString());
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var dto = new SettingsDto { DebugMode = true, AutoPlay = true };
        var (req, _) = MockHttpRequestFactory.Create(
            JsonSerializer.Serialize(dto,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        var function = CreateFunction(db, authService);

        var response = await function.Save(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await db.StreamerSettings.FirstOrDefaultAsync(s => s.StreamerId == StreamerId,
            TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.True(saved.DebugMode);
    }

    [Fact]
    public async Task Given_ValidBody_And_ExistingSettings_When_Save_Then_Returns200AndUpdatesSettings()
    {
        await using var db = CreateDb();
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId, DebugMode = false });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var principal = CreatePrincipal(StreamerId.ToString());
        var authService = Mock.Of<IAuthService>(s => s.Authenticate(It.IsAny<HttpRequestData>()) == principal);
        var dto = new SettingsDto { DebugMode = true };
        var (req, _) = MockHttpRequestFactory.Create(
            JsonSerializer.Serialize(dto,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        var function = CreateFunction(db, authService);

        var response = await function.Save(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await db.StreamerSettings.FirstOrDefaultAsync(s => s.StreamerId == StreamerId,
            TestContext.Current.CancellationToken);
        Assert.True(saved!.DebugMode);
    }
}