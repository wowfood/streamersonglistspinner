using ServerSpinner.Core.Models;

namespace ServerSpinner.Core.Contracts;

public interface ISpinnerApiService
{
    Task<SpinnerQueueItem[]> FetchQueueAsync(string streamer);
    Task<PlayHistoryItem[]> FetchPlayHistoryAsync(string streamer, string period = "week");
    Task<SpinnerConfig?> FetchConfigAsync(string configUrl);
}