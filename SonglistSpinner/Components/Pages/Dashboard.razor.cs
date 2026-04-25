using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ServerSpinner.Core.Models;
using ServerSpinner.Core.Services;

namespace SonglistSpinner.Components.Pages;

// Injected properties (Config, ApiService, SyncService, JS) are generated from @inject in Dashboard.razor.
public partial class Dashboard
{
    private SpinnerQueueItem[] _allSongs = [];
    private string _apiBaseUrl = "";
    private CancellationTokenSource? _autoRefreshCts;
    private List<SpinnerQueueItem> _availableSongs = [];

    private SpinnerConfig _config = new();
    private string _currentStreamer = "";

    private DotNetObjectReference<Dashboard>? _dotNetRef;
    private bool _isLockedDefault;
    private bool _isSpinning;
    private bool _jsInitialized;
    private DateTime _lastSpinTime = DateTime.MinValue;
    private bool _loading = true;
    private bool _playedListCollapsed;
    private CancellationTokenSource? _playedRefreshCts;
    private PlayHistoryItem[] _playedSongs = [];
    private bool _showStreamerInput = true;
    private string _spinButtonText = "SPIN";

    private bool _spinDisabled;

    private string _status = "";
    private bool _statusVisible;

    private string _streamerInput = "";
    private CancellationTokenSource _wheelCts = new();

    private bool _wheelVisible = true;
    private string _winnerDetails = "";
    private string _winnerMainLine = "";
    private bool _winnerVisible;

    public async ValueTask DisposeAsync()
    {
        SyncService.MessageReceived -= OnMessageReceived;
        _autoRefreshCts?.Cancel();
        _autoRefreshCts?.Dispose();
        _playedRefreshCts?.Cancel();
        _playedRefreshCts?.Dispose();
        _wheelCts.Cancel();
        _wheelCts.Dispose();
        _dotNetRef?.Dispose();
        await SyncService.DisposeAsync();
        try
        {
            await JS.InvokeVoidAsync("document.body.classList.remove", "spinner-page");
        }
        catch (Exception ex) { _ = ex; }

        try
        {
            await JS.InvokeVoidAsync("SpinnerInterop.resetBackground");
        }
        catch (Exception ex) { _ = ex; }

        GC.SuppressFinalize(this);
    }

    protected override Task OnInitializedAsync()
    {
        _apiBaseUrl = LocalSettings.GetApiBaseUrl();
        Config["ApiBaseUrl"] = _apiBaseUrl;
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("document.body.classList.add", "spinner-page");
            SyncService.MessageReceived += OnMessageReceived;

            var configUrl = $"{_apiBaseUrl}/api/spinner-config";
            var localConfig = LocalSettings.ToSpinnerConfig(LocalSettings.LoadSettings());
            _config = await ApiService.FetchConfigAsync(configUrl) ?? localConfig;
            _isLockedDefault = _config.Streamer.HideChangeOptionWhenDefault
                               && !string.IsNullOrWhiteSpace(_config.Streamer.DefaultName);

            _loading = false;
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (_jsInitialized) return;
        _jsInitialized = true;

        await JS.InvokeVoidAsync("SpinnerInterop.applyTheme", _config.Colors, _config.PlayedList);
        await JS.InvokeVoidAsync("SpinnerInterop.applyBackground", _config.Background);
        await JS.InvokeVoidAsync("SpinnerInterop.applyPlayedListPosition",
            _config.SongList.PlayedListPosition);

        await JS.InvokeVoidAsync("SpinnerInterop.createWheel",
            new[] { new { label = "Enter streamer name above" } },
            _config.WheelColors);

        await JS.InvokeVoidAsync("SpinnerInterop.setupResizeObserver");
        _dotNetRef = DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("SpinnerInterop.setupResizeHandlers", _dotNetRef);

        var defaultName = _config.Streamer.DefaultName.Trim();
        if (!string.IsNullOrEmpty(defaultName))
        {
            _streamerInput = defaultName;
            await LoadStreamer();
        }

        _autoRefreshCts = new CancellationTokenSource();
        _ = RunAutoRefreshAsync(_autoRefreshCts.Token);
    }

    private async Task LoadStreamer()
    {
        var name = _streamerInput.Trim();
        if (string.IsNullOrEmpty(name))
        {
            SetStatus("Please enter a streamer name");
            return;
        }

        _currentStreamer = name;
        _showStreamerInput = false;
        SetStatus("Loading songs...");
        StateHasChanged();

        try
        {
            var (all, played) = await FetchQueueAndHistory(name);
            _allSongs = all;
            _playedSongs = played;
            _availableSongs = SpinnerDataService.FilterAvailableSongs(all, played, _config);

            await RebuildWheel(_wheelCts.Token);
            SetStatus($"Loaded {_availableSongs.Count} songs. Press SPIN!");
            _ = OverlayService.UpdateStateAsync(_config, _availableSongs, _playedSongs, _currentStreamer);
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }

        StateHasChanged();
    }

    private async Task Spin()
    {
        if (string.IsNullOrEmpty(_currentStreamer))
        {
            SetStatus("Please enter a streamer name first");
            return;
        }

        if ((DateTime.UtcNow - _lastSpinTime).TotalMilliseconds < 1000)
        {
            SetStatus("Cooldown active");
            return;
        }

        _lastSpinTime = DateTime.UtcNow;
        _spinDisabled = true;
        _isSpinning = true;
        _wheelCts.Cancel();
        _wheelCts.Dispose();
        _wheelCts = new CancellationTokenSource();
        SetStatus("Fetching queue...");
        StateHasChanged();

        try
        {
            var (all, played) = await FetchQueueAndHistory(_currentStreamer);
            _allSongs = all;
            _playedSongs = played;
            _availableSongs = SpinnerDataService.FilterAvailableSongs(all, played, _config);

            if (_availableSongs.Count == 0)
            {
                SetStatus("No songs left to spin!");
                _spinDisabled = false;
                _isSpinning = false;
                await InvokeAsync(StateHasChanged);
                return;
            }

            await RebuildWheel(_wheelCts.Token);
            var winnerIndex = Random.Shared.Next(_availableSongs.Count);
            SetStatus("Spinning...");
            await InvokeAsync(StateHasChanged);

            var spinWinner = _availableSongs[winnerIndex];
            var spinMainLine = $"{spinWinner.Song.Artist} - {spinWinner.Song.Title}";
            var spinDetails =
                SpinnerDataService.CreateSongTextForFields(spinWinner, SpinnerDataService.GetWinnerFields(_config));
            _ = OverlayService.BroadcastSpinCommandAsync(winnerIndex, 5000, spinMainLine, spinDetails);

            await JS.InvokeVoidAsync("SpinnerInterop.spinToItem", winnerIndex, 5000);

            var winner = _availableSongs[winnerIndex];
            await Task.Delay(5100);
            ShowWinnerModal(winner);
            SetStatus($"Winner: {SpinnerDataService.BuildWheelLabel(winner)}");
            if (LocalSettings.LoadSettings().AutoPlay)
                _ = TwitchChat.SendCommandAsync($"!setSong {winner.Position} to 1");
            StateHasChanged();

            for (var i = 1; i >= 0; i--)
            {
                await Task.Delay(1000);
                _spinButtonText = i > 0 ? $"{i}" : "SPIN";
                if (i == 0) _spinDisabled = false;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            _spinDisabled = false;
            _isSpinning = false;
            StateHasChanged();
        }
    }

    private async Task ResetPlayed()
    {
        _playedSongs = [];
        _availableSongs = SpinnerDataService.FilterAvailableSongs(_allSongs, _playedSongs, _config);
        SetStatus($"Played songs reset for {_currentStreamer}");
        await RebuildWheel(_wheelCts.Token);
        _ = OverlayService.UpdateStateAsync(_config, _availableSongs, _playedSongs, _currentStreamer);
        StateHasChanged();
    }

    private void ChangeStreamer()
    {
        _showStreamerInput = true;
        _currentStreamer = "";
        _streamerInput = "";
        StateHasChanged();
    }

    private async Task OnWheelToggle(ChangeEventArgs e)
    {
        _wheelVisible = (bool)(e.Value ?? true);
        await JS.InvokeVoidAsync("SpinnerInterop.setWheelVisible", _wheelVisible);
        _ = OverlayService.BroadcastAsync("set_wheel_visible", new { visible = _wheelVisible });
    }

    private async Task ToggleCollapse()
    {
        _playedListCollapsed = !_playedListCollapsed;
        await JS.InvokeVoidAsync("SpinnerInterop.setPlayedListCollapsed",
            _playedListCollapsed, _config.SongList.PlayedListPosition);
        _ = OverlayService.BroadcastAsync("set_collapse", new { collapsed = _playedListCollapsed });
    }

    private void ShowWinnerModal(SpinnerQueueItem song)
    {
        _winnerMainLine = $"{song.Song.Artist} - {song.Song.Title}";
        _winnerDetails = SpinnerDataService.CreateSongTextForFields(
            song, SpinnerDataService.GetWinnerFields(_config));
        _winnerVisible = true;
        _ = JS.InvokeVoidAsync("SpinnerInterop.runConfetti", (object)_config.WheelColors);
    }

    private async Task CloseWinnerModal()
    {
        _winnerVisible = false;
        _isSpinning = false;
        _ = OverlayService.BroadcastCloseWinnerAsync();
        if (LocalSettings.LoadSettings().AutoPlay)
            _ = TwitchChat.SendCommandAsync("!setPlayed");
        _playedRefreshCts?.Cancel();
        _playedRefreshCts = new CancellationTokenSource();
        _ = RefreshAfterWinnerAsync(_playedRefreshCts.Token);
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnResizeEnd(string width, string minWidth)
    {
        _ = OverlayService.BroadcastAsync("set_played_list_width", new { width, minWidth });
        await Task.CompletedTask;
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
                    case "close_winner_modal":
                        _winnerVisible = false;
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
                /* ignore parse errors */
            }

            StateHasChanged();
        });
    }

    private async Task AutoRefresh()
    {
        if (string.IsNullOrEmpty(_currentStreamer) || _isSpinning) return;
        await InvokeAsync(async () =>
        {
            try
            {
                var (all, played) = await FetchQueueAndHistory(_currentStreamer);
                var newAvailable = SpinnerDataService.FilterAvailableSongs(all, played, _config);

                var listChanged = newAvailable.Count != _availableSongs.Count ||
                                  newAvailable.Zip(_availableSongs).Any(p => p.First.Song.Id != p.Second.Song.Id);

                _allSongs = all;
                _playedSongs = played;
                _availableSongs = newAvailable;

                if (listChanged)
                {
                    await RebuildWheel(_wheelCts.Token);
                    _ = OverlayService.UpdateStateAsync(_config, _availableSongs, _playedSongs, _currentStreamer);
                }

                StateHasChanged();
            }
            catch
            {
                /* swallow auto-refresh errors */
            }
        });
    }

    private async Task<(SpinnerQueueItem[] all, PlayHistoryItem[] played)> FetchQueueAndHistory(string streamer)
    {
        var period = _config.SongList.PlayHistoryPeriod;
        var all = await ApiService.FetchQueueAsync(streamer);
        var played = await ApiService.FetchPlayHistoryAsync(streamer, period);
        return (all, played);
    }

    private async Task RebuildWheel(CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested) return;
        var items = _availableSongs.Count > 0
            ? _availableSongs.Select(s => new { label = SpinnerDataService.BuildWheelLabel(s) }).ToArray<object>()
            : new object[] { new { label = "No songs in queue" } };
        await JS.InvokeVoidAsync("SpinnerInterop.createWheel", ct, items, _config.WheelColors);
    }

    private void SetStatus(string message, bool visible = true)
    {
        _status = message;
        _statusVisible = visible && _config.Debug;
    }

    private async Task OnStreamerKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await LoadStreamer();
    }

    private async Task RunAutoRefreshAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
                await AutoRefresh();
        }
        catch (OperationCanceledException) { }
    }

    private async Task RefreshAfterWinnerAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            if (_isSpinning || string.IsNullOrEmpty(_currentStreamer)) return;
            await InvokeAsync(async () =>
            {
                var (all, played) = await FetchQueueAndHistory(_currentStreamer);
                _allSongs = all;
                _playedSongs = played;
                _availableSongs = SpinnerDataService.FilterAvailableSongs(all, played, _config);
                await RebuildWheel(_wheelCts.Token);
                StateHasChanged();
            });
        }
        catch (OperationCanceledException) { }
    }
}