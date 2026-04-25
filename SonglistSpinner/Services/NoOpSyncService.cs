using ServerSpinner.Core.Contracts;

namespace SonglistSpinner.Services;

public class NoOpSyncService : ISpinnerSyncService
{
#pragma warning disable CS0067
    public event Func<string, string, Task>? MessageReceived;
#pragma warning restore CS0067

    public Task InitAsync(string apiBaseUrl, string? streamerId)
    {
        return Task.CompletedTask;
    }

    public Task SendAsync(string messageType, object payload)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}