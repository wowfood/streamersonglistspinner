using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ServerSpinner.Core.Contracts;
using ServerSpinner.Core.Models;

namespace SonglistSpinner.Services;

public class HttpApiService(HttpClient http, ITokenStore tokenStore) : ISpinnerApiService
{
    private const string SslBase = "https://api.streamersonglist.com/v1/streamers";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<SpinnerQueueItem[]> FetchQueueAsync(string streamer)
    {
        var encoded = Uri.EscapeDataString(streamer.Trim().ToLower());
        try
        {
            var response = await http.GetFromJsonAsync<QueueResponse>(
                $"{SslBase}/{encoded}/queue", JsonOpts);
            return response?.List.ToArray() ?? [];
        }
        catch
        {
            return Array.Empty<SpinnerQueueItem>();
        }
    }

    public async Task<PlayHistoryItem[]> FetchPlayHistoryAsync(string streamer, string period = "week")
    {
        var encoded = Uri.EscapeDataString(streamer.Trim().ToLower());
        var url = $"{SslBase}/{encoded}/playHistory" +
                  $"?size=200&current=0&period={Uri.EscapeDataString(period)}&type=playedAt&order=desc";
        try
        {
            var response = await http.GetFromJsonAsync<PlayHistoryResponse>(url, JsonOpts);
            return response?.Items.ToArray() ?? [];
        }
        catch
        {
            return Array.Empty<PlayHistoryItem>();
        }
    }

    public async Task<SpinnerConfig?> FetchConfigAsync(string configUrl)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, configUrl);
            var token = await tokenStore.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SpinnerConfig>(JsonOpts);
        }
        catch
        {
            return null;
        }
    }
}