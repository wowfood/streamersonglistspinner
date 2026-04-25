using System.Net.Http.Json;
using System.Text.Json;
using ServerSpinner.Core.Contracts;
using ServerSpinner.Core.Models;

namespace ServerSpinner.Services;

// authHttp = the app's credentialed HttpClient (for our Azure Functions endpoints)
// externalHttp = a plain HttpClient without credentials (for public external APIs)
public class SpinnerApiService(HttpClient authHttp, HttpClient externalHttp) : ISpinnerApiService
{
    private const string SslBase = "https://api.streamersonglist.com/v1/streamers";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<SpinnerConfig?> FetchConfigAsync(string configUrl)
    {
        try
        {
            return await authHttp.GetFromJsonAsync<SpinnerConfig>(configUrl, JsonOpts);
        }
        catch
        {
            await Console.Error.WriteAsync("Failed to fetch config file");
            return null;
        }
    }

    public async Task<SpinnerQueueItem[]> FetchQueueAsync(string streamer)
    {
        var encoded = Uri.EscapeDataString(streamer.Trim().ToLower());
        try
        {
            var response = await externalHttp.GetFromJsonAsync<QueueResponse>(
                $"{SslBase}/{encoded}/queue", JsonOpts);
            return (response?.List ?? []).ToArray();
        }
        catch
        {
            return [];
        }
    }

    public async Task<PlayHistoryItem[]> FetchPlayHistoryAsync(string streamer, string period = "week")
    {
        var encoded = Uri.EscapeDataString(streamer.Trim().ToLower());
        var url = $"{SslBase}/{encoded}/playHistory" +
                  $"?size=200&current=0&period={Uri.EscapeDataString(period)}&type=playedAt&order=desc";
        try
        {
            var response = await externalHttp.GetFromJsonAsync<PlayHistoryResponse>(url, JsonOpts);
            return (response?.Items ?? []).ToArray();
        }
        catch
        {
            return [];
        }
    }
}