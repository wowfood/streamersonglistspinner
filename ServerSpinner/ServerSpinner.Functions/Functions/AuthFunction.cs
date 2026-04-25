using System.Net;
using System.Text.Json;
using System.Web;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerSpinner.Functions.Entities;
using ServerSpinner.Functions.Data;
using ServerSpinner.Functions.Helpers;
using ServerSpinner.Functions.Services;

namespace ServerSpinner.Functions.Functions;

public class AuthFunction
{
    private static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };

    private readonly IAuthService _authService;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AuthFunction> _logger;

    public AuthFunction(AppDbContext db, IHttpClientFactory httpFactory,
        IConfiguration config, ILogger<AuthFunction> logger, IAuthService authService)
    {
        _db = db;
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
        _authService = authService;
    }

    [Function("GetUser")]
    public async Task<HttpResponseData> GetUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/user")]
        HttpRequestData req)
    {
        var principal = _authService.Authenticate(req);
        if (principal is null)
            return req.CreateResponse(HttpStatusCode.Unauthorized);

        var (id, name) = JwtHelper.GetClaims(principal);
        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { id, displayName = name }));
        return response;
    }

    [Function("TwitchLogin")]
    public HttpResponseData Login(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/twitch/login")]
        HttpRequestData req)
    {
        var clientId = _config["Twitch:ClientId"]!;
        var redirectUri = _config["Twitch:RedirectUri"]!;

        var url = "https://id.twitch.tv/oauth2/authorize" +
                  $"?client_id={clientId}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  "&response_type=code" +
                  "&scope=chat:read+user:write:chat";

        var response = req.CreateResponse(HttpStatusCode.Found);
        response.Headers.Add("Location", url);
        return response;
    }

    [Function("TwitchCallback")]
    public async Task<HttpResponseData> Callback(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/twitch/callback")]
        HttpRequestData req)
    {
        var query = HttpUtility.ParseQueryString(req.Url.Query);
        var code = query["code"];
        if (string.IsNullOrEmpty(code))
        {
            var err = req.CreateResponse(HttpStatusCode.BadRequest);
            await err.WriteStringAsync("Missing code");
            return err;
        }

        var clientId = _config["Twitch:ClientId"]!;
        var clientSecret = _config["Twitch:ClientSecret"]!;
        var redirectUri = _config["Twitch:RedirectUri"]!;
        var secret = _config["JwtSecret"]!;

        var client = _httpFactory.CreateClient();

        var tokenResponse = await client.PostAsync(
            "https://id.twitch.tv/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUri
            }));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get token: {Status}", tokenResponse.StatusCode);
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Failed to get token");
            return err;
        }

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        var tokenData = JsonSerializer.Deserialize<TwitchTokenResponse>(tokenJson,
            CaseInsensitive)!;

        var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/users");
        userRequest.Headers.Add("Authorization", $"Bearer {tokenData.AccessToken}");
        userRequest.Headers.Add("Client-Id", clientId);
        var userResponse = await client.SendAsync(userRequest);

        if (!userResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to get user info: {Status}", userResponse.StatusCode);
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Failed to get user info");
            return err;
        }

        var userJson = await userResponse.Content.ReadAsStringAsync();
        var userData = JsonSerializer.Deserialize<TwitchUserResponse>(userJson,
            CaseInsensitive)!;
        var twitchUser = userData.Data.FirstOrDefault();

        if (twitchUser is null)
        {
            _logger.LogError("Twitch returned empty user data");
            var err = req.CreateResponse(HttpStatusCode.InternalServerError);
            await err.WriteStringAsync("Failed to get user info");
            return err;
        }

        var streamer = await _db.Streamers
            .Include(s => s.Settings)
            .FirstOrDefaultAsync(s => s.TwitchUserId == twitchUser.Id);

        if (streamer == null)
        {
            streamer = new Streamer
            {
                Id = Guid.NewGuid(),
                TwitchUserId = twitchUser.Id,
                DisplayName = twitchUser.DisplayName,
                AccessToken = tokenData.AccessToken,
                RefreshToken = tokenData.RefreshToken,
                TokenExpiry = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn),
                Settings = new StreamerSettings()
            };
            _db.Streamers.Add(streamer);
        }
        else
        {
            streamer.DisplayName = twitchUser.DisplayName;
            streamer.AccessToken = tokenData.AccessToken;
            streamer.RefreshToken = tokenData.RefreshToken;
            streamer.TokenExpiry = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn);
        }

        await _db.SaveChangesAsync();

        var jwt = JwtHelper.Create(streamer.Id.ToString(), streamer.DisplayName, secret);
        var webBase = _config["WebBaseUrl"] ?? "/";
        var secure = req.Url.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "; Secure" : "";
        var cookieValue = $"ss_auth={jwt}; HttpOnly{secure}; SameSite=Lax; Path=/; Max-Age=604800";

        var response = req.CreateResponse(HttpStatusCode.Found);
        response.Headers.Add("Set-Cookie", cookieValue);
        response.Headers.Add("Location", webBase);
        return response;
    }

    [Function("TwitchLogout")]
    public HttpResponseData Logout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/twitch/logout")]
        HttpRequestData req)
    {
        var webBase = _config["WebBaseUrl"] ?? "/";
        var secure = req.Url.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "; Secure" : "";
        var response = req.CreateResponse(HttpStatusCode.Found);
        response.Headers.Add("Set-Cookie", $"ss_auth=; HttpOnly{secure}; SameSite=Lax; Path=/; Max-Age=0");
        response.Headers.Add("Location", webBase);
        return response;
    }
}