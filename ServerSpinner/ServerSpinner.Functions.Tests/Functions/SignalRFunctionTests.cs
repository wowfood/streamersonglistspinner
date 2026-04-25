using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ServerSpinner.Functions.Contracts;
using ServerSpinner.Functions.Functions;
using ServerSpinner.Functions.Tests.Helpers;
using Xunit;

namespace ServerSpinner.Functions.Tests.Functions;

public class SignalRFunctionTests
{
    private static readonly Guid StreamerId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    // ServiceManager is a concrete class; passing null is safe for tests that return before
    // reaching the SignalR send path. For valid-message tests the send is wrapped in try-catch
    // so a NullReferenceException on _signalR is caught and logged without failing the request.
    private static SignalRFunction CreateFunction(
        ISpinnerStateService? stateService = null,
        IAutoPlayService? autoPlayService = null)
    {
        return new SignalRFunction(NullLogger<SignalRFunction>.Instance,
            null!,
            stateService ?? Mock.Of<ISpinnerStateService>(),
            autoPlayService ?? Mock.Of<IAutoPlayService>());
    }

    // ── Negotiate ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_ValidConnectionInfo_When_Negotiate_Then_Returns200WithUrlAndToken()
    {
        var connectionInfo = new SignalRConnectionInfo
        {
            Url = "https://signalr.example.com/client/?hub=spinner",
            AccessToken = "test-access-token"
        };
        var (req, _) = MockHttpRequestFactory.Create();
        var function = CreateFunction();

        var response = await function.Negotiate(req, connectionInfo);
        var body = ((FakeHttpResponseData)response).GetBodyAsString();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("signalr.example.com", body);
        Assert.Contains("test-access-token", body);
    }

    // ── SendMessage ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_InvalidJsonBody_When_SendMessage_Then_Returns400()
    {
        var (req, _) = MockHttpRequestFactory.Create("not-valid-json{{{");
        var function = CreateFunction();

        var response = await function.SendMessage(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Given_EmptyBody_When_SendMessage_Then_Returns400()
    {
        var (req, _) = MockHttpRequestFactory.Create(null);
        var function = CreateFunction();

        var response = await function.SendMessage(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Given_MissingStreamerId_When_SendMessage_Then_Returns400()
    {
        var body = JsonSerializer.Serialize(new { streamerId = "", messageType = "spin_command", payloadJson = "{}" });
        var (req, _) = MockHttpRequestFactory.Create(body);
        var function = CreateFunction();

        var response = await function.SendMessage(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Given_InvalidGuidStreamerId_When_SendMessage_Then_Returns400()
    {
        var body = JsonSerializer.Serialize(new
            { streamerId = "not-a-guid", messageType = "spin_command", payloadJson = "{}" });
        var (req, _) = MockHttpRequestFactory.Create(body);
        var function = CreateFunction();

        var response = await function.SendMessage(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Given_ValidMessage_When_SendMessage_Then_Returns200()
    {
        var body = JsonSerializer.Serialize(new
        {
            streamerId = StreamerId.ToString(),
            messageType = "spin_command",
            payloadJson = "{}"
        });
        var (req, _) = MockHttpRequestFactory.Create(body);
        var function = CreateFunction();

        var response = await function.SendMessage(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Given_StateServiceMessage_When_SendMessage_Then_CallsSpinnerStateService()
    {
        var stateService = new Mock<ISpinnerStateService>();
        var body = JsonSerializer.Serialize(new
        {
            streamerId = StreamerId.ToString(),
            messageType = "spin_command",
            payloadJson = "{}"
        });
        var (req, _) = MockHttpRequestFactory.Create(body);
        var function = CreateFunction(stateService.Object);

        await function.SendMessage(req);

        stateService.Verify(s => s.UpdateAsync(StreamerId, "spin_command", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Given_AutoPlayMessage_When_SendMessage_Then_CallsAutoPlayService()
    {
        var autoPlayService = new Mock<IAutoPlayService>();
        var body = JsonSerializer.Serialize(new
        {
            streamerId = StreamerId.ToString(),
            messageType = "close_winner_modal",
            payloadJson = "{}"
        });
        var (req, _) = MockHttpRequestFactory.Create(body);
        var function = CreateFunction(autoPlayService: autoPlayService.Object);

        await function.SendMessage(req);

        autoPlayService.Verify(s => s.HandleAsync(StreamerId, "close_winner_modal", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Given_UnrelatedMessageType_When_SendMessage_Then_DoesNotCallStateOrAutoPlayService()
    {
        var stateService = new Mock<ISpinnerStateService>();
        var autoPlayService = new Mock<IAutoPlayService>();
        var body = JsonSerializer.Serialize(new
        {
            streamerId = StreamerId.ToString(),
            messageType = "unknown_type",
            payloadJson = "{}"
        });
        var (req, _) = MockHttpRequestFactory.Create(body);
        var function = CreateFunction(stateService.Object, autoPlayService.Object);

        await function.SendMessage(req);

        stateService.Verify(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        autoPlayService.Verify(s => s.HandleAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Given_SpinCommand_When_SendMessage_Then_CallsBothStateAndAutoPlayService()
    {
        var stateService = new Mock<ISpinnerStateService>();
        var autoPlayService = new Mock<IAutoPlayService>();
        var body = JsonSerializer.Serialize(new
        {
            streamerId = StreamerId.ToString(),
            messageType = "spin_command",
            payloadJson = "{}"
        });
        var (req, _) = MockHttpRequestFactory.Create(body);
        var function = CreateFunction(stateService.Object, autoPlayService.Object);

        await function.SendMessage(req);

        stateService.Verify(s => s.UpdateAsync(StreamerId, "spin_command", It.IsAny<string>()), Times.Once);
        autoPlayService.Verify(s => s.HandleAsync(StreamerId, "spin_command", It.IsAny<string>()), Times.Once);
    }

    [Theory]
    [InlineData("set_streamer")]
    [InlineData("client_state_push")]
    [InlineData("reset_played")]
    public async Task Given_StateOnlyMessageType_When_SendMessage_Then_CallsStateServiceButNotAutoPlay(
        string messageType)
    {
        var stateService = new Mock<ISpinnerStateService>();
        var autoPlayService = new Mock<IAutoPlayService>();
        var body = JsonSerializer.Serialize(new
        {
            streamerId = StreamerId.ToString(),
            messageType,
            payloadJson = "{}"
        });
        var (req, _) = MockHttpRequestFactory.Create(body);
        var function = CreateFunction(stateService.Object, autoPlayService.Object);

        await function.SendMessage(req);

        stateService.Verify(s => s.UpdateAsync(StreamerId, messageType, It.IsAny<string>()), Times.Once);
        autoPlayService.Verify(s => s.HandleAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // ── AddToGroup ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_NullBody_When_AddToGroup_Then_Returns400()
    {
        var (req, _) = MockHttpRequestFactory.Create("null");
        var function = CreateFunction();

        var response = await function.AddToGroup(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Given_MissingConnectionId_When_AddToGroup_Then_Returns400()
    {
        var body = JsonSerializer.Serialize(new { connectionId = "", streamerId = StreamerId.ToString() });
        var (req, _) = MockHttpRequestFactory.Create(body);
        var function = CreateFunction();

        var response = await function.AddToGroup(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Given_MissingStreamerId_When_AddToGroup_Then_Returns400()
    {
        var body = JsonSerializer.Serialize(new { connectionId = "conn-123", streamerId = "" });
        var (req, _) = MockHttpRequestFactory.Create(body);
        var function = CreateFunction();

        var response = await function.AddToGroup(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Given_ValidRequest_When_AddToGroup_Then_Returns200()
    {
        var body = JsonSerializer.Serialize(new
        {
            connectionId = "conn-123",
            streamerId = StreamerId.ToString()
        });
        var (req, _) = MockHttpRequestFactory.Create(body);
        var function = CreateFunction();

        var response = await function.AddToGroup(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}