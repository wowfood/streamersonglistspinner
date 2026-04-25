using System.Text.Json.Serialization;

namespace ServerSpinner.Core.Models;

public class SpinnerSong
{
    [JsonPropertyName("id")] public int? Id { get; set; }

    [JsonPropertyName("artist")] public string Artist { get; set; } = "";

    [JsonPropertyName("title")] public string Title { get; set; } = "";
}