using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using ServerSpinner.Core.Contracts;

namespace ServerSpinner.Services;

public class SpinnerSyncService(HttpClient http) : ISpinnerSyncService
{
    private string? _apiBaseUrl;
    private HubConnection? _connection;
    private string? _streamerId;

    public event Func<string, string, Task>? MessageReceived;

    public async Task InitAsync(string apiBaseUrl, string? streamerId)
    {
        if (_connection?.State is HubConnectionState.Connected or HubConnectionState.Connecting) return;

        _apiBaseUrl = apiBaseUrl;
        _streamerId = streamerId;


        using var negotiateResponse = await http.PostAsync($"{apiBaseUrl}/api/negotiate", null);
        if (!negotiateResponse.IsSuccessStatusCode) return;

        var info = await negotiateResponse.Content.ReadFromJsonAsync<SignalRNegotiateInfo>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (info is null)
        {
            await Console.Error.WriteLineAsync("[SyncService] Negotiate response could not be deserialised.");
            return;
        }

        var accessToken = info.AccessToken;
        _connection = new HubConnectionBuilder()
            .WithUrl(info.Url,
                options => { options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken); })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<string, string>("ReceiveMessage", async (messageType, payloadJson) =>
        {
            if (MessageReceived is not null)
                await MessageReceived(messageType, payloadJson);
        });

        try
        {
            await _connection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SyncService] StartAsync failed: {ex.Message}");
            return;
        }

        if (!string.IsNullOrEmpty(streamerId) && _connection.ConnectionId is not null)
        {
            // Small delay to let Azure SignalR fully register the connection before joining group
            await Task.Delay(500);
            await http.PostAsJsonAsync($"{apiBaseUrl}/api/join-group", new
            {
                connectionId = _connection.ConnectionId,
                streamerId
            });
        }
        else
        {
            Console.WriteLine(
                $"[SyncService] Skipped join-group: streamerId='{streamerId}', ConnectionId={_connection.ConnectionId}");
        }
    }

    public async Task SendAsync(string messageType, object payload)
    {
        if (string.IsNullOrEmpty(_streamerId) || string.IsNullOrEmpty(_apiBaseUrl)) return;
        await http.PostAsJsonAsync($"{_apiBaseUrl}/api/messages", new
        {
            streamerId = _streamerId,
            messageType,
            payloadJson = JsonSerializer.Serialize(payload)
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }

    private sealed record SignalRNegotiateInfo(string Url, string AccessToken);
}