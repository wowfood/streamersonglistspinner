using System.Text.Json.Serialization;

namespace ServerSpinner.Core.Models;

public class PlayHistoryResponse
{
    [JsonPropertyName("items")] public List<PlayHistoryItem> Items { get; set; } = [];
}