using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ServerSpinner.Core.Models;
using ServerSpinner.Core.Services;

namespace ServerSpinner.Components.Pages;

public partial class Overlay
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private SpinnerQueueItem[] _allSongs = [];

    private string _apiBaseUrl = "";
    private List<SpinnerQueueItem> _availableSongs = [];

    private SpinnerConfig _config = new();
    private string _currentStreamer = "";
    private bool _isSpinning;
    private bool _playedListCollapsed;
    private PlayHistoryItem[] _playedSongs = [];
    private bool _wheelVisible = true;
    private string _winnerDetails = "";
    private string _winnerMainLine = "";
    private bool _winnerVisible;
    [Parameter] public Guid StreamerId { get; set; }

    public async ValueTask DisposeAsync()
    {
        SyncService.MessageReceived -= OnMessageReceived;
        await JS.InvokeVoidAsync("document.body.classList.remove", "overlay-mode");
        await SyncService.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void OnInitialized()
    {
        _apiBaseUrl = Config["ApiBaseUrl"] ?? "";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await JS.InvokeVoidAsync("document.body.classList.add", "overlay-mode");
        SyncService.MessageReceived += OnMessageReceived;

        var configUrl = $"{_apiBaseUrl}/api/spinner-config/{StreamerId}";
        _config = await ApiService.FetchConfigAsync(configUrl) ?? new SpinnerConfig();

        await JS.InvokeVoidAsync("SpinnerInterop.applyTheme", _config.Colors, _config.PlayedList);
        await JS.InvokeVoidAsync("SpinnerInterop.applyBackground", _config.Background);
        await JS.InvokeVoidAsync("SpinnerInterop.applyPlayedListPosition", _config.SongList.PlayedListPosition);

        await JS.InvokeVoidAsync("SpinnerInterop.createWheel",
            new[] { new { label = "Waiting for streamer..." } },
            _config.WheelColors);

        await JS.InvokeVoidAsync("SpinnerInterop.setupResizeObserver");

        var defaultName = _config.Streamer.DefaultName.Trim();
        if (!string.IsNullOrEmpty(defaultName))
        {
            _currentStreamer = defaultName;
            await LoadStreamerSongs();
        }

        await SyncService.InitAsync(_apiBaseUrl, StreamerId.ToString());
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadStreamerSongs()
    {
        if (string.IsNullOrEmpty(_currentStreamer)) return;
        var period = _config.SongList.PlayHistoryPeriod;
        var newAll = await ApiService.FetchQueueAsync(_currentStreamer);
        var newPlayed = await ApiService.FetchPlayHistoryAsync(_currentStreamer, period);
        var newAvailable = SpinnerDataService.FilterAvailableSongs(newAll, newPlayed, _config);

        var listChanged = newAvailable.Count != _availableSongs.Count ||
                          newAvailable.Zip(_availableSongs).Any(p => p.First.Song.Id != p.Second.Song.Id);

        _allSongs = newAll;
        _playedSongs = newPlayed;
        _availableSongs = newAvailable;

        if (listChanged)
            await RebuildWheel();
        StateHasChanged();
    }

    private async Task ToggleCollapse()
    {
        _playedListCollapsed = !_playedListCollapsed;
        await JS.InvokeVoidAsync("SpinnerInterop.setPlayedListCollapsed",
            _playedListCollapsed, _config.SongList.PlayedListPosition);
    }

    private void ShowWinnerModal(SpinnerQueueItem song)
    {
        _winnerMainLine = $"{song.Song.Artist} - {song.Song.Title}";
        _winnerDetails = SpinnerDataService.CreateSongTextForFields(
            song, SpinnerDataService.GetWinnerFields(_config));
        _winnerVisible = true;
        _ = JS.InvokeVoidAsync("SpinnerInterop.runConfetti", (object)_config.WheelColors);
    }

    private void CloseWinnerModal()
    {
        _winnerVisible = false;
        _isSpinning = false;
    }

    private async Task OnMessageReceived(string messageType, string payloadJson)
    {
        await InvokeAsync(async () =>
        {
            try
            {
                var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
                switch (messageType)
                {
                    case "set_streamer":
                        if (payload.TryGetProperty("streamer", out var streamerEl))
                        {
                            var name = streamerEl.GetString() ?? "";
                            if (!string.IsNullOrEmpty(name) && !_isSpinning)
                            {
                                _currentStreamer = name;
                                await LoadStreamerSongs();
                            }
                        }

                        break;

                    case "spin_command":
                        if (payload.TryGetProperty("songData", out var songDataEl))
                        {
                            var winner = JsonSerializer.Deserialize<SpinnerQueueItem>(
                                songDataEl.GetRawText(), JsonOpts);
                            if (winner != null)
                            {
                                _isSpinning = true;
                                var historyItem = new PlayHistoryItem { Song = winner.Song };
                                var winnerIndex = _availableSongs.FindIndex(s =>
                                    SpinnerDataService.SongMatchesPlayed(s, historyItem));
                                if (winnerIndex >= 0)
                                {
                                    await JS.InvokeVoidAsync("SpinnerInterop.spinToItem", winnerIndex, 5000);
                                    await Task.Delay(5100);
                                }

                                ShowWinnerModal(winner);
                                StateHasChanged();
                            }
                        }

                        break;

                    case "reset_played":
                        _playedSongs = [];
                        _availableSongs = SpinnerDataService.FilterAvailableSongs(_allSongs, _playedSongs, _config);
                        await RebuildWheel();
                        break;

                    case "close_winner_modal":
                        _winnerVisible = false;
                        _isSpinning = false;
                        break;

                    case "set_wheel_visible":
                        if (payload.TryGetProperty("visible", out var vis))
                        {
                            _wheelVisible = vis.GetBoolean();
                            await JS.InvokeVoidAsync("SpinnerInterop.setWheelVisible", _wheelVisible);
                        }

                        break;

                    case "set_collapse":
                        if (payload.TryGetProperty("collapsed", out var col))
                        {
                            _playedListCollapsed = col.GetBoolean();
                            await JS.InvokeVoidAsync("SpinnerInterop.setPlayedListCollapsed",
                                _playedListCollapsed, _config.SongList.PlayedListPosition);
                        }

                        break;

                    case "set_played_list_width":
                        if (payload.TryGetProperty("width", out var w) &&
                            payload.TryGetProperty("minWidth", out var mw))
                            await JS.InvokeVoidAsync("SpinnerInterop.setPlayedListWidth",
                                w.GetString(), mw.GetString());
                        break;
                }
            }
            catch
            {
                /* ignore parse errors in incoming messages */
            }

            StateHasChanged();
        });
    }

    private async Task RebuildWheel()
    {
        var items = _availableSongs.Count > 0
            ? _availableSongs.Select(s => new { label = SpinnerDataService.BuildWheelLabel(s) }).ToArray<object>()
            : new object[] { new { label = "No songs in queue" } };
        await JS.InvokeVoidAsync("SpinnerInterop.createWheel", items, _config.WheelColors);
    }
}