using ServerSpinner.Core.Data;
using ServerSpinner.Functions.Contracts;
using ServerSpinner.Functions.Entities;

namespace ServerSpinner.Functions.Services;

public class SettingsMapper : ISettingsMapper
{
    public SettingsDto ToDto(StreamerSettings settings)
    {
        return new SettingsDto
        {
            WheelColors = settings.WheelColors,
            BackgroundMode = settings.BackgroundMode,
            BackgroundColor = settings.BackgroundColor,
            BackgroundImage = settings.BackgroundImage,
            DefaultStreamerName = settings.DefaultStreamerName,
            HideChangeOptionWhenDefault = settings.HideChangeOptionWhenDefault,
            SongListFields = settings.SongListFields,
            ExcludePlayedSongs = settings.ExcludePlayedSongs,
            PlayedListPosition = settings.PlayedListPosition,
            PlayHistoryPeriod = settings.PlayHistoryPeriod,
            AutoPlay = settings.AutoPlay,
            DebugMode = settings.DebugMode,
            ColorText = settings.ColorText,
            ColorStatusBackground = settings.ColorStatusBackground,
            ColorPlayedListBackground = settings.ColorPlayedListBackground,
            ColorPlayedItemBackground = settings.ColorPlayedItemBackground,
            ColorResizeHandleBackground = settings.ColorResizeHandleBackground,
            ColorResizeHandleHoverBackground = settings.ColorResizeHandleHoverBackground,
            ColorToggleBackground = settings.ColorToggleBackground,
            ColorButtonBackground = settings.ColorButtonBackground,
            ColorButtonText = settings.ColorButtonText,
            ColorPointer = settings.ColorPointer,
            PlayedListFontFamily = settings.PlayedListFontFamily,
            PlayedListFontSize = settings.PlayedListFontSize,
            PlayedListMaxLines = settings.PlayedListMaxLines
        };
    }

    public void Apply(SettingsDto dto, StreamerSettings settings)
    {
        settings.WheelColors = dto.WheelColors;
        settings.BackgroundMode = dto.BackgroundMode;
        settings.BackgroundColor = dto.BackgroundColor;
        settings.BackgroundImage = dto.BackgroundImage;
        settings.DefaultStreamerName = dto.DefaultStreamerName;
        settings.HideChangeOptionWhenDefault = dto.HideChangeOptionWhenDefault;
        settings.SongListFields = dto.SongListFields;
        settings.ExcludePlayedSongs = dto.ExcludePlayedSongs;
        settings.PlayedListPosition = dto.PlayedListPosition;
        settings.PlayHistoryPeriod = dto.PlayHistoryPeriod;
        settings.AutoPlay = dto.AutoPlay;
        settings.DebugMode = dto.DebugMode;
        settings.ColorText = dto.ColorText;
        settings.ColorStatusBackground = dto.ColorStatusBackground;
        settings.ColorPlayedListBackground = dto.ColorPlayedListBackground;
        settings.ColorPlayedItemBackground = dto.ColorPlayedItemBackground;
        settings.ColorResizeHandleBackground = dto.ColorResizeHandleBackground;
        settings.ColorResizeHandleHoverBackground = dto.ColorResizeHandleHoverBackground;
        settings.ColorToggleBackground = dto.ColorToggleBackground;
        settings.ColorButtonBackground = dto.ColorButtonBackground;
        settings.ColorButtonText = dto.ColorButtonText;
        settings.ColorPointer = dto.ColorPointer;
        settings.PlayedListFontFamily = dto.PlayedListFontFamily;
        settings.PlayedListFontSize = dto.PlayedListFontSize;
        settings.PlayedListMaxLines = dto.PlayedListMaxLines;
    }
}