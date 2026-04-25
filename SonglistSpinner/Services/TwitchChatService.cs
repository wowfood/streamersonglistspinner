using System.Text;
using System.Text.Json;

namespace SonglistSpinner.Services;

public class TwitchChatService
{
    private readonly TwitchAuthService _auth;
    private readonly HttpClient _http;

    public TwitchChatService(HttpClient http, TwitchAuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task SendCommandAsync(string command)
    {
        if (!_auth.IsAuthenticated) return;
        var token = await _auth.GetValidAccessTokenAsync();
        if (string.IsNullOrEmpty(token)) return;

        var userId = _auth.TwitchUserId!;
        var body = JsonSerializer.Serialize(new
        {
            broadcaster_id = userId,
            sender_id = userId,
            message = command
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.twitch.tv/helix/chat/messages");
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("Client-Id", TwitchAuthService.ClientId);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        try
        {
            await _http.SendAsync(request);
        }
        catch
        {
            /* chat commands are best-effort */
        }
    }
}