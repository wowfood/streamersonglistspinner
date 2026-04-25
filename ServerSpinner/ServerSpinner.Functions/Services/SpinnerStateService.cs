using System.Text.Json;
using Microsoft.Extensions.Logging;
using ServerSpinner.Functions.Contracts;
using ServerSpinner.Functions.Entities;
using ServerSpinner.Functions.Data;

namespace ServerSpinner.Functions.Services;

public class SpinnerStateService : ISpinnerStateService
{
    private readonly AppDbContext _db;
    private readonly ILogger<SpinnerStateService> _logger;

    public SpinnerStateService(AppDbContext db, ILogger<SpinnerStateService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task UpdateAsync(Guid streamerId, string messageType, string payloadJson)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            var state = await _db.SpinnerStates.FindAsync(streamerId);

            switch (messageType)
            {
                case "set_streamer":
                case "client_state_push":
                    await UpdateCurrentStreamerAsync(streamerId, payload, state);
                    break;

                case "spin_command":
                    await AddPlayedSongAsync(streamerId, payload, state);
                    break;

                case "reset_played":
                    await ResetPlayedSongsAsync(state);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update spinner state for {StreamerId}", streamerId);
        }
    }

    private async Task UpdateCurrentStreamerAsync(Guid streamerId, JsonElement payload, SpinnerState? state)
    {
        var streamer = payload.TryGetProperty("streamer", out var s) ? s.GetString() ?? "" : "";

        if (state == null)
        {
            _db.SpinnerStates.Add(new SpinnerState { StreamerId = streamerId, CurrentStreamer = streamer });
        }
        else
        {
            state.CurrentStreamer = streamer;
            state.LastUpdated = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    private async Task AddPlayedSongAsync(Guid streamerId, JsonElement payload, SpinnerState? state)
    {
        if (!payload.TryGetProperty("songId", out var idProp)) return;
        var songId = idProp.ToString();

        if (state == null)
        {
            state = new SpinnerState { StreamerId = streamerId };
            _db.SpinnerStates.Add(state);
        }

        var ids = DeserializePlayedIds(state.PlayedSongIdsJson, streamerId);
        if (!ids.Contains(songId)) ids.Add(songId);
        state.PlayedSongIdsJson = JsonSerializer.Serialize(ids);
        state.LastUpdated = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task ResetPlayedSongsAsync(SpinnerState? state)
    {
        if (state == null) return;
        state.PlayedSongIdsJson = "[]";
        state.LastUpdated = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private List<string> DeserializePlayedIds(string json, Guid streamerId)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize PlayedSongIdsJson for {StreamerId}, resetting to empty",
                streamerId);
            return [];
        }
    }
}