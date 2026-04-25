using System.Text.Json.Serialization;

namespace ServerSpinner.Core.Models;

public class SpinnerRequest
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    [JsonPropertyName("donationAmount")] public decimal? DonationAmount { get; set; }

    [JsonPropertyName("donation")] public decimal? Donation { get; set; }

    [JsonPropertyName("amount")] public decimal? Amount { get; set; }

    [JsonPropertyName("price")] public decimal? Price { get; set; }
}