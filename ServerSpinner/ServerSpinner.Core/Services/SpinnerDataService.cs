using ServerSpinner.Core.Models;

namespace ServerSpinner.Core.Services;

public static class SpinnerDataService
{
    public static bool SongMatchesPlayed(SpinnerQueueItem queueItem, PlayHistoryItem playedItem)
    {
        var q = queueItem.Song;
        var p = playedItem.Song;
        if (p is null) return false;
        if (q.Id.HasValue && p.Id.HasValue) return q.Id == p.Id;
        return string.Equals(q.Artist, p.Artist, StringComparison.OrdinalIgnoreCase)
               && string.Equals(q.Title, p.Title, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetPrimaryRequester(SpinnerQueueItem song)
    {
        return song.Requests.FirstOrDefault()?.Name is { Length: > 0 } n ? n : "Unknown";
    }

    public static string BuildWheelLabel(SpinnerQueueItem song)
    {
        var artist = song.Song.Artist is { Length: > 0 } a ? a : "Unknown";
        var title = song.Song.Title is { Length: > 0 } t ? t : "Unknown";
        return $"{artist} - {title} ({GetPrimaryRequester(song)})";
    }

    public static string FormatDonation(SpinnerQueueItem song)
    {
        return FormatDonationFromRequest(song.Requests.FirstOrDefault(), "None");
    }

    public static string GetSongFieldValue(SpinnerQueueItem song, string field)
    {
        return field.ToLowerInvariant() switch
        {
            "artist" => song.Song.Artist is { Length: > 0 } a ? a : "Unknown",
            "title" => song.Song.Title is { Length: > 0 } t ? t : "Unknown",
            "requester" => GetPrimaryRequester(song),
            "donation" => FormatDonation(song),
            _ => ""
        };
    }

    public static string CreateSongTextForFields(SpinnerQueueItem song, IEnumerable<string> fields)
    {
        var parts = fields
            .Select(f => (field: f, value: GetSongFieldValue(song, f)))
            .Where(x => x.field.Length > 0 && !string.IsNullOrEmpty(x.value))
            .Select(x => $"{char.ToUpperInvariant(x.field[0])}{x.field[1..]}: {x.value}");
        return string.Join(" | ", parts);
    }

    public static string CreatePlayedSongText(SpinnerQueueItem song, SpinnerConfig config)
    {
        var fields = config.SongList.Fields is { Length: > 0 } f ? f : ["artist", "title"];
        return CreateSongTextForFields(song, fields);
    }

    public static string[] GetWinnerFields(SpinnerConfig config)
    {
        var fields = (config.SongList.Fields is { Length: > 0 } f ? f : (string[])["artist", "title"])
            .Select(x => x.ToLowerInvariant())
            .ToList();
        if (!fields.Contains("requester")) fields.Add("requester");
        return [.. fields];
    }

    public static string CreatePlayedSongText(PlayHistoryItem item, SpinnerConfig config)
    {
        var fields = config.SongList.Fields is { Length: > 0 } f ? f : (string[])["artist", "title"];
        var parts = fields
            .Where(fieldName => fieldName.Length > 0)
            .Select(field => (field, value: GetHistoryFieldValue(item, field)))
            .Where(x => !string.IsNullOrEmpty(x.value))
            .Select(x => $"{char.ToUpperInvariant(x.field[0])}{x.field[1..]}: {x.value}");
        return string.Join(" | ", parts);
    }

    private static string GetHistoryFieldValue(PlayHistoryItem item, string field)
    {
        return field.ToLowerInvariant() switch
        {
            "artist" => item.Song?.Artist is { Length: > 0 } a ? a : "Unknown",
            "title" => item.Song?.Title is { Length: > 0 } t ? t : "Unknown",
            "requester" => item.Requests.FirstOrDefault()?.Name is { Length: > 0 } n ? n : "Unknown",
            "donation" => FormatDonationFromRequest(item.Requests.FirstOrDefault(), ""),
            _ => ""
        };
    }

    // Single source of truth for donation formatting. fallback differs by context:
    // queue items show "None", history items omit the field entirely (empty string).
    private static string FormatDonationFromRequest(SpinnerRequest? request, string fallback)
    {
        if (request is null) return fallback;
        var amount = request.DonationAmount ?? request.Donation ?? request.Amount ?? request.Price;
        return amount.HasValue ? $"{amount.Value}" : fallback;
    }

    // O(n×m) — acceptable for typical queue sizes (< 200 songs, < 100 played).
    // If scale demands it, replace the inner Any() with a HashSet lookup.
    public static List<SpinnerQueueItem> FilterAvailableSongs(
        IEnumerable<SpinnerQueueItem> all,
        IEnumerable<PlayHistoryItem> played,
        SpinnerConfig config)
    {
        if (!config.SongList.ExcludePlayedSongs) return all.ToList();
        var playedList = played.ToList();
        return all.Where(song => !playedList.Any(p => SongMatchesPlayed(song, p))).ToList();
    }
}