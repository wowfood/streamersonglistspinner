namespace ServerSpinner.Functions.Data;

public class TwitchTokenRefresh
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public int ExpiresIn { get; set; }
}