using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SonglistSpinner.Services;

public class TwitchAuthService
{
    public const string ClientId = "drqs6lplcj63ky4r40fh7yznyv44wd";
    private const string ClientSecret = "21le7g5c0f2bffiyz50dpihxk25ytc";
    private const string RedirectUri = "http://localhost:3000/auth/callback";
    private const string Scope = "chat:read user:write:chat";

    private const string AccessTokenKey = "twitch_access_token";
    private const string RefreshTokenKey = "twitch_refresh_token";
    private const string UserIdKey = "twitch_user_id";
    private const string DisplayNameKey = "twitch_display_name";
    private const string TokenExpiryKey = "twitch_token_expiry";

    private readonly HttpClient _http;
    private string? _pendingState;

    public TwitchAuthService(HttpClient http)
    {
        _http = http;
    }

    public string? TwitchUserId { get; private set; }
    public string? DisplayName { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(TwitchUserId);

    public event Action? AuthStateChanged;

    public async Task LoadAsync()
    {
        var userId = Preferences.Get(UserIdKey, null);
        var token = await SecureStorage.GetAsync(AccessTokenKey);
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(token))
        {
            TwitchUserId = userId;
            DisplayName = Preferences.Get(DisplayNameKey, null);
        }
    }

    public string GetAuthUrl()
    {
        _pendingState = Guid.NewGuid().ToString("N");
        return "https://id.twitch.tv/oauth2/authorize" +
               $"?client_id={ClientId}" +
               $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
               "&response_type=code" +
               $"&scope={Uri.EscapeDataString(Scope)}" +
               $"&state={_pendingState}";
    }

    public async Task<bool> CompleteOAuthAsync(string code, string state)
    {
        if (string.IsNullOrEmpty(_pendingState) || state != _pendingState)
            return false;
        _pendingState = null;

        var tokenResp = await _http.PostAsync("https://id.twitch.tv/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = RedirectUri
            }));

        if (!tokenResp.IsSuccessStatusCode) return false;

        var tokenJson = await tokenResp.Content.ReadAsStringAsync();
        var tokens = JsonSerializer.Deserialize<TwitchTokenResponse>(tokenJson);
        if (tokens == null) return false;

        var userReq = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/users");
        userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        userReq.Headers.Add("Client-Id", ClientId);
        var userResp = await _http.SendAsync(userReq);
        if (!userResp.IsSuccessStatusCode) return false;

        var userJson = await userResp.Content.ReadAsStringAsync();
        var usersData = JsonSerializer.Deserialize<TwitchUsersResponse>(userJson);
        var user = usersData?.Data.FirstOrDefault();
        if (user == null) return false;

        await PersistAsync(tokens.AccessToken, tokens.RefreshToken ?? "", tokens.ExpiresIn,
            user.Id, user.DisplayName);
        return true;
    }

    public async Task<string?> GetValidAccessTokenAsync()
    {
        var expiryStr = Preferences.Get(TokenExpiryKey, null);
        if (!string.IsNullOrEmpty(expiryStr) && long.TryParse(expiryStr, out var expiryBinary))
        {
            var expiry = DateTime.FromBinary(expiryBinary);
            if (expiry > DateTime.UtcNow.AddMinutes(5))
                return await SecureStorage.GetAsync(AccessTokenKey);
        }

        return await RefreshAsync();
    }

    private async Task<string?> RefreshAsync()
    {
        var refreshToken = await SecureStorage.GetAsync(RefreshTokenKey);
        if (string.IsNullOrEmpty(refreshToken)) return null;

        var resp = await _http.PostAsync("https://id.twitch.tv/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            }));

        if (!resp.IsSuccessStatusCode)
        {
            await ClearAsync();
            return null;
        }

        var json = await resp.Content.ReadAsStringAsync();
        var tokens = JsonSerializer.Deserialize<TwitchTokenResponse>(json);
        if (tokens == null) return null;

        await PersistAsync(tokens.AccessToken, tokens.RefreshToken ?? refreshToken,
            tokens.ExpiresIn, TwitchUserId!, DisplayName!);
        return tokens.AccessToken;
    }

    public async Task ClearAsync()
    {
        SecureStorage.Remove(AccessTokenKey);
        SecureStorage.Remove(RefreshTokenKey);
        Preferences.Remove(UserIdKey);
        Preferences.Remove(DisplayNameKey);
        Preferences.Remove(TokenExpiryKey);
        TwitchUserId = null;
        DisplayName = null;
        AuthStateChanged?.Invoke();
        await Task.CompletedTask;
    }

    private async Task PersistAsync(string accessToken, string refreshToken, int expiresIn,
        string userId, string displayName)
    {
        await SecureStorage.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.SetAsync(RefreshTokenKey, refreshToken);
        Preferences.Set(TokenExpiryKey, DateTime.UtcNow.AddSeconds(expiresIn).ToBinary().ToString());
        Preferences.Set(UserIdKey, userId);
        Preferences.Set(DisplayNameKey, displayName);
        TwitchUserId = userId;
        DisplayName = displayName;
        AuthStateChanged?.Invoke();
    }
}

file class TwitchTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
}

file class TwitchUsersResponse
{
    [JsonPropertyName("data")] public List<TwitchUser> Data { get; set; } = [];
}

file class TwitchUser
{
    [JsonPropertyName("id")] public string Id { get; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; } = "";
}