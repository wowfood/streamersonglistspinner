using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using ServerSpinner.Functions.Entities;
using ServerSpinner.Functions.Data;
using ServerSpinner.Functions.Services;
using Xunit;

namespace ServerSpinner.Functions.Tests.Services;

public class AutoPlayServiceTests
{
    private const string TwitchUserId = "twitch-user-123";
    private const string ValidAccessToken = "valid-access-token";
    private const string RefreshToken = "refresh-token";
    private static readonly Guid StreamerId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    private static AppDbContext CreateContext()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static IConfiguration CreateConfig()
    {
        return Mock.Of<IConfiguration>(c =>
            c["Twitch:ClientId"] == "test-client-id" &&
            c["Twitch:ClientSecret"] == "test-client-secret");
    }

    private static (IHttpClientFactory Factory, Mock<HttpMessageHandler> Handler) CreateMockHttpFactory(
        params (string UrlFragment, HttpResponseMessage Response)[] responses)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        foreach (var (urlFragment, response) in responses)
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsoluteUri.Contains(urlFragment)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler.Object));
        return (factory.Object, handler);
    }

    private static Streamer CreateStreamer(DateTime? tokenExpiry = null)
    {
        return new Streamer
        {
            Id = StreamerId,
            TwitchUserId = TwitchUserId,
            AccessToken = ValidAccessToken,
            RefreshToken = RefreshToken,
            TokenExpiry = tokenExpiry ?? DateTime.UtcNow.AddHours(1),
            Settings = new StreamerSettings()
        };
    }

    private static string SpinCommandPayload(int queuePosition = 3)
    {
        return JsonSerializer.Serialize(new { songId = "song-1", queuePosition });
    }

    private static string CloseWinnerPayload()
    {
        return "{}";
    }

    private static AutoPlayService CreateService(AppDbContext db, IHttpClientFactory factory,
        IConfiguration? config = null)
    {
        return new AutoPlayService(db, config ?? CreateConfig(), factory, NullLogger<AutoPlayService>.Instance);
    }

    // ── AutoPlay disabled / missing data ──────────────────────────────────────

    [Fact]
    public async Task Given_NoSettings_When_HandleAsync_Then_NoHttpCallIsMade()
    {
        await using var db = CreateContext();
        var (factory, handler) = CreateMockHttpFactory();
        var service = CreateService(db, factory);

        await service.HandleAsync(StreamerId, "spin_command", SpinCommandPayload());

        handler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task Given_AutoPlayDisabled_When_HandleAsync_Then_NoHttpCallIsMade()
    {
        await using var db = CreateContext();
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId, AutoPlay = false });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (factory, handler) = CreateMockHttpFactory();
        var service = CreateService(db, factory);

        await service.HandleAsync(StreamerId, "spin_command", SpinCommandPayload());

        handler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task Given_AutoPlayEnabled_And_NoStreamer_When_HandleAsync_Then_NoHttpCallIsMade()
    {
        await using var db = CreateContext();
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId, AutoPlay = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var (factory, handler) = CreateMockHttpFactory();
        var service = CreateService(db, factory);

        await service.HandleAsync(StreamerId, "spin_command", SpinCommandPayload());

        handler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    // ── spin_command ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_AutoPlayEnabled_And_SpinCommand_When_HandleAsync_Then_SendsSetSongChatCommand()
    {
        await using var db = CreateContext();
        var streamer = CreateStreamer();
        db.Streamers.Add(streamer);
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId, AutoPlay = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (factory, handler) = CreateMockHttpFactory(
            ("chat/messages", new HttpResponseMessage(HttpStatusCode.OK)));
        var service = CreateService(db, factory);

        await service.HandleAsync(StreamerId, "spin_command", SpinCommandPayload(3));

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsoluteUri.Contains("chat/messages")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task
        Given_AutoPlayEnabled_And_SpinCommandWithQueuePosition_When_HandleAsync_Then_SendsCorrectPositionInCommand()
    {
        await using var db = CreateContext();
        db.Streamers.Add(CreateStreamer());
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId, AutoPlay = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        string? capturedBody = null;
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
                capturedBody = await req.Content!.ReadAsStringAsync(_))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var factory =
            Mock.Of<IHttpClientFactory>(f => f.CreateClient(It.IsAny<string>()) == new HttpClient(handler.Object));
        var service = CreateService(db, factory);

        await service.HandleAsync(StreamerId, "spin_command", SpinCommandPayload(5));

        Assert.NotNull(capturedBody);
        Assert.Contains("!setSong 5 to 1", capturedBody);
    }

    // ── close_winner_modal ────────────────────────────────────────────────────

    [Fact]
    public async Task Given_AutoPlayEnabled_And_CloseWinnerModal_When_HandleAsync_Then_SendsSetPlayedCommand()
    {
        await using var db = CreateContext();
        db.Streamers.Add(CreateStreamer());
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId, AutoPlay = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        string? capturedBody = null;
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
                capturedBody = await req.Content!.ReadAsStringAsync(_))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var factory =
            Mock.Of<IHttpClientFactory>(f => f.CreateClient(It.IsAny<string>()) == new HttpClient(handler.Object));
        var service = CreateService(db, factory);

        await service.HandleAsync(StreamerId, "close_winner_modal", CloseWinnerPayload());

        Assert.NotNull(capturedBody);
        Assert.Contains("!setPlayed", capturedBody);
    }

    // ── Token refresh ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_AutoPlayEnabled_And_TokenExpired_When_HandleAsync_Then_RefreshesTokenBeforeSendingCommand()
    {
        await using var db = CreateContext();
        var streamer = CreateStreamer(DateTime.UtcNow.AddMinutes(-1));
        db.Streamers.Add(streamer);
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId, AutoPlay = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var refreshJson = JsonSerializer.Serialize(new
        {
            access_token = "new-token",
            refresh_token = "new-refresh",
            expires_in = 3600
        });

        var (factory, handler) = CreateMockHttpFactory(
            ("oauth2/token", new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(refreshJson) }),
            ("chat/messages", new HttpResponseMessage(HttpStatusCode.OK)));
        var service = CreateService(db, factory);

        await service.HandleAsync(StreamerId, "spin_command", SpinCommandPayload());

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsoluteUri.Contains("oauth2/token")),
            ItExpr.IsAny<CancellationToken>());
        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsoluteUri.Contains("chat/messages")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task Given_AutoPlayEnabled_And_TokenRefreshFails_When_HandleAsync_Then_NoChatCommandIsSent()
    {
        await using var db = CreateContext();
        var streamer = CreateStreamer(DateTime.UtcNow.AddMinutes(-1));
        db.Streamers.Add(streamer);
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId, AutoPlay = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (factory, handler) = CreateMockHttpFactory(
            ("oauth2/token", new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        var service = CreateService(db, factory);

        await service.HandleAsync(StreamerId, "spin_command", SpinCommandPayload());

        handler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsoluteUri.Contains("chat/messages")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task Given_AutoPlayEnabled_And_ValidToken_When_HandleAsync_Then_DoesNotRefreshToken()
    {
        await using var db = CreateContext();
        db.Streamers.Add(CreateStreamer(DateTime.UtcNow.AddHours(1)));
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId, AutoPlay = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (factory, handler) = CreateMockHttpFactory(
            ("chat/messages", new HttpResponseMessage(HttpStatusCode.OK)));
        var service = CreateService(db, factory);

        await service.HandleAsync(StreamerId, "spin_command", SpinCommandPayload());

        handler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsoluteUri.Contains("oauth2/token")),
            ItExpr.IsAny<CancellationToken>());
    }

    // ── Unrelated message types ───────────────────────────────────────────────

    [Fact]
    public async Task Given_AutoPlayEnabled_And_UnrelatedMessageType_When_HandleAsync_Then_NoHttpCallIsMade()
    {
        await using var db = CreateContext();
        db.Streamers.Add(CreateStreamer());
        db.StreamerSettings.Add(new StreamerSettings { StreamerId = StreamerId, AutoPlay = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (factory, handler) = CreateMockHttpFactory();
        var service = CreateService(db, factory);

        await service.HandleAsync(StreamerId, "set_streamer", "{}");

        handler.Protected().Verify("SendAsync", Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}