using System.Text.Json.Serialization;

namespace ServerSpinner.Functions.Data;

public class TwitchTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")] public string TokenType { get; set; } = string.Empty;
}

public class TwitchUserResponse
{
    [JsonPropertyName("data")] public List<TwitchUser> Data { get; set; } = new();
}

public class TwitchUser
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("login")] public string Login { get; set; } = string.Empty;

    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = string.Empty;
}