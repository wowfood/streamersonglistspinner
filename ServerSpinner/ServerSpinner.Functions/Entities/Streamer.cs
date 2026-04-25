namespace ServerSpinner.Functions.Entities;

public class Streamer
{
    public Guid Id { get; set; }
    public string TwitchUserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime TokenExpiry { get; set; }
    public StreamerSettings Settings { get; set; } = new();
}
