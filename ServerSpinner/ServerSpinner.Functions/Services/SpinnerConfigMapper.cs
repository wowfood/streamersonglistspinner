using System.Text.Json;
using Microsoft.Extensions.Logging;
using ServerSpinner.Core.Data;
using ServerSpinner.Functions.Contracts;
using ServerSpinner.Functions.Entities;

namespace ServerSpinner.Functions.Services;

public class SpinnerConfigMapper : ISpinnerConfigMapper
{
    private static readonly string[] DefaultWheelColors =
        ["#ff6b6b", "#4ecdc4", "#45b7d1", "#f9ca24", "#6c5ce7", "#a29bfe", "#fd79a8", "#fdcb6e"];

    private static readonly string[] DefaultFields = ["artist", "title"];

    private readonly ILogger<SpinnerConfigMapper> _logger;

    public SpinnerConfigMapper(ILogger<SpinnerConfigMapper> logger)
    {
        _logger = logger;
    }

    public SpinnerConfigResponse ToConfigResponse(StreamerSettings settings)
    {
        var wheelColors = TryDeserialize(settings.WheelColors, DefaultWheelColors, nameof(settings.WheelColors));
        var fields = TryDeserialize(settings.SongListFields, DefaultFields, nameof(settings.SongListFields));

        return new SpinnerConfigResponse(
            settings.DebugMode,
            wheelColors,
            new SpinnerConfigBackground(settings.BackgroundMode, settings.BackgroundColor, settings.BackgroundImage),
            new SpinnerConfigStreamer(settings.DefaultStreamerName, settings.HideChangeOptionWhenDefault),
            new SpinnerConfigSongList(fields, settings.ExcludePlayedSongs, settings.PlayedListPosition,
                settings.PlayHistoryPeriod),
            new SpinnerConfigPlayedList(settings.PlayedListFontFamily, settings.PlayedListFontSize,
                settings.PlayedListMaxLines),
            new SpinnerConfigColors(
                settings.ColorText,
                settings.ColorStatusBackground,
                settings.ColorPlayedListBackground,
                settings.ColorPlayedItemBackground,
                settings.ColorResizeHandleBackground,
                settings.ColorResizeHandleHoverBackground,
                settings.ColorToggleBackground,
                settings.ColorButtonBackground,
                settings.ColorButtonText,
                settings.ColorPointer));
    }

    private string[] TryDeserialize(string json, string[] fallback, string fieldName)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? fallback;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize {FieldName}, using defaults", fieldName);
            return fallback;
        }
    }
}