using System.Text.Json.Serialization;

namespace ServerSpinner.Core.Models;

public class QueueResponse
{
    [JsonPropertyName("list")] public List<SpinnerQueueItem> List { get; set; } = [];
}