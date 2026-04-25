using System.Text.Json.Serialization;

namespace ServerSpinner.Core.Models;

public class PlayHistoryItem
{
    [JsonPropertyName("song")] public SpinnerSong? Song { get; init; }

    [JsonPropertyName("requests")] public List<SpinnerRequest> Requests { get; init; } = [];
}