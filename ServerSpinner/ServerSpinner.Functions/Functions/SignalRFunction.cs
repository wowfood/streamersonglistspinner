using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;
using ServerSpinner.Core.Data;
using ServerSpinner.Functions.Contracts;

namespace ServerSpinner.Functions.Functions;

public class SignalRFunction
{
    private static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };

    private readonly IAutoPlayService _autoPlayService;
    private readonly ILogger<SignalRFunction> _logger;
    private readonly ServiceManager _signalR;
    private readonly ISpinnerStateService _spinnerStateService;

    public SignalRFunction(ILogger<SignalRFunction> logger, ServiceManager signalR,
        ISpinnerStateService spinnerStateService, IAutoPlayService autoPlayService)
    {
        _logger = logger;
        _signalR = signalR;
        _spinnerStateService = spinnerStateService;
        _autoPlayService = autoPlayService;
    }

    [Function("Negotiate")]
    public async Task<HttpResponseData> Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "negotiate")]
        HttpRequestData req,
        [SignalRConnectionInfoInput(HubName = "spinner")]
        SignalRConnectionInfo connectionInfo)
    {
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new
        {
            url = connectionInfo.Url,
            accessToken = connectionInfo.AccessToken
        }));
        return response;
    }

    [Function("SendMessage")]
    public async Task<HttpResponseData> SendMessage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "messages")]
        HttpRequestData req)
    {
        MessageRequest? msg;
        try
        {
            using var reader = new StreamReader(req.Body);
            var bodyStr = await reader.ReadToEndAsync();
            msg = JsonSerializer.Deserialize<MessageRequest>(bodyStr,
                CaseInsensitive);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize message request body");
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        if (msg is null || string.IsNullOrEmpty(msg.StreamerId))
            return req.CreateResponse(HttpStatusCode.BadRequest);
        if (!Guid.TryParse(msg.StreamerId, out var streamerId))
            return req.CreateResponse(HttpStatusCode.BadRequest);

        await DispatchMessageAsync(streamerId, msg.StreamerId, msg.MessageType, msg.PayloadJson);

        return req.CreateResponse(HttpStatusCode.OK);
    }

    private async Task DispatchMessageAsync(Guid streamerId, string streamerIdStr, string messageType,
        string payloadJson)
    {
        _logger.LogInformation("[SignalR] Dispatching message type={MessageType} to group={Group}", messageType,
            streamerIdStr);

        switch (messageType)
        {
            case "set_streamer":
            case "client_state_push":
            case "spin_command":
            case "reset_played":
                await _spinnerStateService.UpdateAsync(streamerId, messageType, payloadJson);
                break;
        }

        switch (messageType)
        {
            case "spin_command":
            case "close_winner_modal":
                await _autoPlayService.HandleAsync(streamerId, messageType, payloadJson);
                break;
        }

        await SendToGroupDirectAsync(streamerIdStr, messageType, payloadJson);
    }

    [Function("AddToGroup")]
    public async Task<HttpResponseData> AddToGroup(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "join-group")]
        HttpRequestData req)
    {
        using var reader = new StreamReader(req.Body);
        var bodyStr = await reader.ReadToEndAsync();
        var body = JsonSerializer.Deserialize<JoinGroupRequest>(bodyStr,
            CaseInsensitive);

        if (body is null || string.IsNullOrEmpty(body.ConnectionId) || string.IsNullOrEmpty(body.StreamerId))
            return req.CreateResponse(HttpStatusCode.BadRequest);

        await AddConnectionToGroupDirectAsync(body.ConnectionId, body.StreamerId);
        return req.CreateResponse(HttpStatusCode.OK);
    }

    private async Task SendToGroupDirectAsync(string groupName, string messageType, string payloadJson)
    {
        try
        {
            await using var hub = await _signalR.CreateHubContextAsync("spinner", CancellationToken.None);
            await hub.Clients.Group(groupName).SendAsync("ReceiveMessage", messageType, payloadJson);
            _logger.LogInformation("[SignalR] Group send OK: group={Group} type={Type}", groupName, messageType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SignalR] SendToGroupDirect failed for group={Group} type={Type}", groupName,
                messageType);
        }
    }

    private async Task AddConnectionToGroupDirectAsync(string connectionId, string groupName)
    {
        try
        {
            await using var hub = await _signalR.CreateHubContextAsync("spinner", CancellationToken.None);
            await hub.Groups.AddToGroupAsync(connectionId, groupName);
            _logger.LogInformation("[SignalR] AddToGroup OK: connection={ConnectionId} group={Group}", connectionId,
                groupName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SignalR] AddToGroup failed: connection={ConnectionId} group={Group}", connectionId,
                groupName);
        }
    }
}