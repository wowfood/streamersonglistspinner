using System.Text.Json.Serialization;

namespace ServerSpinner.Core.Models;

public class SpinnerQueueItem
{
    [JsonPropertyName("song")] public SpinnerSong Song { get; set; } = new();

    [JsonPropertyName("requests")] public List<SpinnerRequest> Requests { get; set; } = [];

    [JsonPropertyName("position")] public int Position { get; set; }
}