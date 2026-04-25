namespace ServerSpinner.Core.Contracts;

public interface ISpinnerSyncService : IAsyncDisposable
{
    event Func<string, string, Task>? MessageReceived;
    Task InitAsync(string apiBaseUrl, string? streamerId);
    Task SendAsync(string messageType, object payload);
}