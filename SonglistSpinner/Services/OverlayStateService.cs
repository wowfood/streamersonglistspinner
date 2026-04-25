using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using ServerSpinner.Core.Models;
using ServerSpinner.Core.Services;

namespace SonglistSpinner.Services;

public class OverlayStateService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ConcurrentDictionary<Guid, Channel<string>> _clients = new();
    private List<SpinnerQueueItem> _availableSongs = [];

    private SpinnerConfig _config = new();
    private string _currentStreamer = "";
    private PlayHistoryItem[] _playedSongs = [];
    public int Port { get; } = 5150;
    public string OverlayUrl => $"http://localhost:{Port}/overlay";

    public Task UpdateStateAsync(SpinnerConfig config, List<SpinnerQueueItem> available,
        PlayHistoryItem[] played, string streamer)
    {
        _config = config;
        _availableSongs = available;
        _playedSongs = played;
        _currentStreamer = streamer;

        var wheelItems = BuildWheelItems(available);
        var playedTexts = played.Select(s => SpinnerDataService.CreatePlayedSongText(s, config)).ToList();

        return BroadcastAsync("update_songs", new
        {
            streamer,
            wheelItems,
            playedTexts,
            playedCount = played.Length,
            availableCount = available.Count
        });
    }

    public Task BroadcastSpinCommandAsync(int winnerIndex, int duration, string mainLine, string details)
    {
        return BroadcastAsync("spin_command", new { winnerIndex, duration, mainLine, details });
    }

    public Task BroadcastCloseWinnerAsync()
    {
        return BroadcastAsync("close_winner", new { });
    }

    public Task BroadcastAsync(string eventName, object payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOpts);
        var message = $"event: {eventName}\ndata: {json}\n\n";
        foreach (var (_, channel) in _clients)
            channel.Writer.TryWrite(message);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<string> SubscribeAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<string>();
        var key = Guid.NewGuid();
        _clients[key] = channel;

        try
        {
            yield return BuildInitStateEvent();

            await foreach (var msg in channel.Reader.ReadAllAsync(ct))
                yield return msg;
        }
        finally
        {
            _clients.TryRemove(key, out _);
            channel.Writer.TryComplete();
        }
    }

    private string BuildInitStateEvent()
    {
        var wheelItems = BuildWheelItems(_availableSongs);
        var playedTexts = _playedSongs.Select(s => SpinnerDataService.CreatePlayedSongText(s, _config)).ToList();

        var payload = new
        {
            config = _config,
            streamer = _currentStreamer,
            wheelItems,
            playedTexts,
            playedCount = _playedSongs.Length,
            availableCount = _availableSongs.Count
        };

        var json = JsonSerializer.Serialize(payload, JsonOpts);
        return $"event: init_state\ndata: {json}\n\n";
    }

    private static object[] BuildWheelItems(List<SpinnerQueueItem> songs)
    {
        return songs.Count > 0
            ? songs.Select(s => (object)new { label = SpinnerDataService.BuildWheelLabel(s) }).ToArray()
            : [new { label = "Waiting for Dashboard..." }];
    }
}