using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerSpinner.Functions.Contracts;
using ServerSpinner.Functions.Entities;
using ServerSpinner.Functions.Data;

namespace ServerSpinner.Functions.Services;

public class AutoPlayService : IAutoPlayService
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AutoPlayService> _logger;

    public AutoPlayService(AppDbContext db, IConfiguration config, IHttpClientFactory httpFactory,
        ILogger<AutoPlayService> logger)
    {
        _db = db;
        _config = config;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task HandleAsync(Guid streamerId, string messageType, string payloadJson)
    {
        try
        {
            var settings = await _db.StreamerSettings.FirstOrDefaultAsync(s => s.StreamerId == streamerId);
            if (settings == null || !settings.AutoPlay) return;

            var streamer = await _db.Streamers.FirstOrDefaultAsync(s => s.Id == streamerId);
            if (streamer == null) return;

            var accessToken = await GetValidAccessTokenAsync(streamer);
            if (accessToken == null) return;

            if (messageType == "spin_command")
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
                if (!payload.TryGetProperty("queuePosition", out var pos)) return;
                var position = pos.GetInt32();
                await SendTwitchChatCommandAsync(streamer.TwitchUserId, accessToken, $"!setSong {position} to 1");
            }
            else if (messageType == "close_winner_modal")
            {
                await SendTwitchChatCommandAsync(streamer.TwitchUserId, accessToken, "!setPlayed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AutoPlay failed for {StreamerId}", streamerId);
        }
    }

    private async Task<string?> GetValidAccessTokenAsync(Streamer streamer)
    {
        if (streamer.TokenExpiry > DateTime.UtcNow.AddMinutes(5))
            return streamer.AccessToken;

        var client = _httpFactory.CreateClient();
        var refreshResponse = await client.PostAsync("https://id.twitch.tv/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = streamer.RefreshToken,
                ["client_id"] = _config["Twitch:ClientId"]!,
                ["client_secret"] = _config["Twitch:ClientSecret"]!
            }));

        if (!refreshResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Token refresh failed for {StreamerId}", streamer.Id);
            return null;
        }

        var json = await refreshResponse.Content.ReadAsStringAsync();
        var tokens = JsonSerializer.Deserialize<TwitchTokenRefresh>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (tokens == null) return null;

        streamer.AccessToken = tokens.AccessToken;
        streamer.RefreshToken = tokens.RefreshToken;
        streamer.TokenExpiry = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn);
        await _db.SaveChangesAsync();

        return tokens.AccessToken;
    }

    private async Task SendTwitchChatCommandAsync(string twitchUserId, string accessToken, string command)
    {
        var client = _httpFactory.CreateClient();
        var clientId = _config["Twitch:ClientId"]!;
        var body = JsonSerializer.Serialize(new
        {
            broadcaster_id = twitchUserId,
            sender_id = twitchUserId,
            message = command
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.twitch.tv/helix/chat/messages");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("Client-Id", clientId);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Failed to send chat command '{Command}': {Status}", command, response.StatusCode);
    }
}