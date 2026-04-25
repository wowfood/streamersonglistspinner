namespace SonglistSpinner.Services;

public class SecureStorageTokenStore : ITokenStore
{
    private const string Key = "twitch_access_token";

    public Task<string?> GetTokenAsync()
    {
        return SecureStorage.GetAsync(Key);
    }

    public Task SetTokenAsync(string token)
    {
        return SecureStorage.SetAsync(Key, token);
    }

    public Task ClearTokenAsync()
    {
        SecureStorage.Remove(Key);
        return Task.CompletedTask;
    }
}