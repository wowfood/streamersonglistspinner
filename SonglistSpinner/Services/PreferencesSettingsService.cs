using System.Text.Json;
using ServerSpinner.Core.Data;
using ServerSpinner.Core.Models;

namespace SonglistSpinner.Services;

public class PreferencesSettingsService : ILocalSettingsService
{
    private const string SettingsKey = "local_settings";
    private const string ApiUrlKey = "api_base_url";
    private const string DefaultApiUrl = "http://localhost:7071";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };
    private SpinnerConfig? _cachedConfig;

    public SpinnerConfig CurrentConfig => _cachedConfig ??= ToSpinnerConfig(LoadSettings());

    public SettingsDto LoadSettings()
    {
        var json = Preferences.Get(SettingsKey, null);
        if (string.IsNullOrEmpty(json)) return new SettingsDto();
        try
        {
            return JsonSerializer.Deserialize<SettingsDto>(json, JsonOpts) ?? new SettingsDto();
        }
        catch
        {
            return new SettingsDto();
        }
    }

    public void SaveSettings(SettingsDto dto)
    {
        Preferences.Set(SettingsKey, JsonSerializer.Serialize(dto, JsonOpts));
    }

    public string GetApiBaseUrl()
    {
        return Preferences.Get(ApiUrlKey, DefaultApiUrl);
    }

    public void SetApiBaseUrl(string url)
    {
        Preferences.Set(ApiUrlKey, url.Trim());
    }

    public SpinnerConfig ToSpinnerConfig(SettingsDto dto)
    {
        string[] wheelColors;
        try
        {
            wheelColors = JsonSerializer.Deserialize<string[]>(dto.WheelColors, JsonOpts) ??
                          SpinnerConfig.DefaultWheelColors;
        }
        catch
        {
            wheelColors = SpinnerConfig.DefaultWheelColors;
        }

        string[] fields;
        try
        {
            fields = JsonSerializer.Deserialize<string[]>(dto.SongListFields, JsonOpts) ?? ["artist", "title"];
        }
        catch
        {
            fields = ["artist", "title"];
        }

        return new SpinnerConfig
        {
            Debug = dto.DebugMode,
            WheelColors = wheelColors,
            Background = new SpinnerBackground
            {
                Mode = dto.BackgroundMode,
                Color = dto.BackgroundColor,
                Image = dto.BackgroundImage
            },
            Streamer = new SpinnerStreamerConfig
            {
                DefaultName = dto.DefaultStreamerName,
                HideChangeOptionWhenDefault = dto.HideChangeOptionWhenDefault
            },
            SongList = new SpinnerSongListConfig
            {
                Fields = fields,
                ExcludePlayedSongs = dto.ExcludePlayedSongs,
                PlayedListPosition = dto.PlayedListPosition,
                PlayHistoryPeriod = dto.PlayHistoryPeriod
            },
            PlayedList = new SpinnerPlayedListConfig
            {
                FontFamily = dto.PlayedListFontFamily,
                FontSize = dto.PlayedListFontSize,
                MaxLines = dto.PlayedListMaxLines
            },
            Colors = new SpinnerColors
            {
                Text = dto.ColorText,
                StatusBackground = dto.ColorStatusBackground,
                PlayedListBackground = dto.ColorPlayedListBackground,
                PlayedItemBackground = dto.ColorPlayedItemBackground,
                ResizeHandleBackground = dto.ColorResizeHandleBackground,
                ResizeHandleHoverBackground = dto.ColorResizeHandleHoverBackground,
                ToggleBackground = dto.ColorToggleBackground,
                ButtonBackground = dto.ColorButtonBackground,
                ButtonText = dto.ColorButtonText,
                Pointer = dto.ColorPointer
            }
        };
    }
}