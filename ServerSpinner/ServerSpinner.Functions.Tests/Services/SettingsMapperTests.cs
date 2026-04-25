using ServerSpinner.Core.Data;
using ServerSpinner.Functions.Entities;
using ServerSpinner.Functions.Services;
using Xunit;

namespace ServerSpinner.Functions.Tests.Services;

public class SettingsMapperTests
{
    private readonly SettingsMapper _mapper = new();

    private static StreamerSettings CreateFullSettings()
    {
        return new StreamerSettings
        {
            WheelColors = """["#ff0000","#00ff00"]""",
            BackgroundMode = "image",
            BackgroundColor = "#333333",
            BackgroundImage = "https://example.com/bg.png",
            DefaultStreamerName = "TestStreamer",
            HideChangeOptionWhenDefault = false,
            SongListFields = """["artist","requester"]""",
            ExcludePlayedSongs = true,
            PlayedListPosition = "left",
            PlayHistoryPeriod = "day",
            AutoPlay = true,
            DebugMode = true,
            ColorText = "#ffffff",
            ColorStatusBackground = "#000000",
            ColorPlayedListBackground = "#111111",
            ColorPlayedItemBackground = "#222222",
            ColorResizeHandleBackground = "#333333",
            ColorResizeHandleHoverBackground = "#444444",
            ColorToggleBackground = "#555555",
            ColorButtonBackground = "#666666",
            ColorButtonText = "#777777",
            ColorPointer = "red",
            PlayedListFontFamily = "monospace",
            PlayedListFontSize = "1rem",
            PlayedListMaxLines = 5
        };
    }

    // ── ToDto ────────────────────────────────────────────────────────────────

    [Fact]
    public void Given_PopulatedSettings_When_ToDto_Then_MapsAllPropertiesCorrectly()
    {
        var settings = CreateFullSettings();

        var dto = _mapper.ToDto(settings);

        Assert.Equal(settings.WheelColors, dto.WheelColors);
        Assert.Equal(settings.BackgroundMode, dto.BackgroundMode);
        Assert.Equal(settings.BackgroundColor, dto.BackgroundColor);
        Assert.Equal(settings.BackgroundImage, dto.BackgroundImage);
        Assert.Equal(settings.DefaultStreamerName, dto.DefaultStreamerName);
        Assert.Equal(settings.HideChangeOptionWhenDefault, dto.HideChangeOptionWhenDefault);
        Assert.Equal(settings.SongListFields, dto.SongListFields);
        Assert.Equal(settings.ExcludePlayedSongs, dto.ExcludePlayedSongs);
        Assert.Equal(settings.PlayedListPosition, dto.PlayedListPosition);
        Assert.Equal(settings.PlayHistoryPeriod, dto.PlayHistoryPeriod);
        Assert.Equal(settings.AutoPlay, dto.AutoPlay);
        Assert.Equal(settings.DebugMode, dto.DebugMode);
        Assert.Equal(settings.ColorText, dto.ColorText);
        Assert.Equal(settings.ColorStatusBackground, dto.ColorStatusBackground);
        Assert.Equal(settings.ColorPlayedListBackground, dto.ColorPlayedListBackground);
        Assert.Equal(settings.ColorPlayedItemBackground, dto.ColorPlayedItemBackground);
        Assert.Equal(settings.ColorResizeHandleBackground, dto.ColorResizeHandleBackground);
        Assert.Equal(settings.ColorResizeHandleHoverBackground, dto.ColorResizeHandleHoverBackground);
        Assert.Equal(settings.ColorToggleBackground, dto.ColorToggleBackground);
        Assert.Equal(settings.ColorButtonBackground, dto.ColorButtonBackground);
        Assert.Equal(settings.ColorButtonText, dto.ColorButtonText);
        Assert.Equal(settings.ColorPointer, dto.ColorPointer);
        Assert.Equal(settings.PlayedListFontFamily, dto.PlayedListFontFamily);
        Assert.Equal(settings.PlayedListFontSize, dto.PlayedListFontSize);
        Assert.Equal(settings.PlayedListMaxLines, dto.PlayedListMaxLines);
    }

    [Fact]
    public void Given_DefaultSettings_When_ToDto_Then_ProducesNonNullDto()
    {
        var settings = new StreamerSettings();

        var dto = _mapper.ToDto(settings);

        Assert.NotNull(dto);
    }

    // ── Apply ────────────────────────────────────────────────────────────────

    [Fact]
    public void Given_PopulatedDto_When_Apply_Then_MapsAllPropertiesCorrectly()
    {
        var dto = new SettingsDto
        {
            WheelColors = """["#ff0000","#00ff00"]""",
            BackgroundMode = "image",
            BackgroundColor = "#333333",
            BackgroundImage = "https://example.com/bg.png",
            DefaultStreamerName = "TestStreamer",
            HideChangeOptionWhenDefault = false,
            SongListFields = """["artist","requester"]""",
            ExcludePlayedSongs = true,
            PlayedListPosition = "left",
            PlayHistoryPeriod = "day",
            AutoPlay = true,
            DebugMode = true,
            ColorText = "#ffffff",
            ColorStatusBackground = "#000000",
            ColorPlayedListBackground = "#111111",
            ColorPlayedItemBackground = "#222222",
            ColorResizeHandleBackground = "#333333",
            ColorResizeHandleHoverBackground = "#444444",
            ColorToggleBackground = "#555555",
            ColorButtonBackground = "#666666",
            ColorButtonText = "#777777",
            ColorPointer = "red",
            PlayedListFontFamily = "monospace",
            PlayedListFontSize = "1rem",
            PlayedListMaxLines = 5
        };
        var settings = new StreamerSettings();

        _mapper.Apply(dto, settings);

        Assert.Equal(dto.WheelColors, settings.WheelColors);
        Assert.Equal(dto.BackgroundMode, settings.BackgroundMode);
        Assert.Equal(dto.BackgroundColor, settings.BackgroundColor);
        Assert.Equal(dto.BackgroundImage, settings.BackgroundImage);
        Assert.Equal(dto.DefaultStreamerName, settings.DefaultStreamerName);
        Assert.Equal(dto.HideChangeOptionWhenDefault, settings.HideChangeOptionWhenDefault);
        Assert.Equal(dto.SongListFields, settings.SongListFields);
        Assert.Equal(dto.ExcludePlayedSongs, settings.ExcludePlayedSongs);
        Assert.Equal(dto.PlayedListPosition, settings.PlayedListPosition);
        Assert.Equal(dto.PlayHistoryPeriod, settings.PlayHistoryPeriod);
        Assert.Equal(dto.AutoPlay, settings.AutoPlay);
        Assert.Equal(dto.DebugMode, settings.DebugMode);
        Assert.Equal(dto.ColorText, settings.ColorText);
        Assert.Equal(dto.ColorStatusBackground, settings.ColorStatusBackground);
        Assert.Equal(dto.ColorPlayedListBackground, settings.ColorPlayedListBackground);
        Assert.Equal(dto.ColorPlayedItemBackground, settings.ColorPlayedItemBackground);
        Assert.Equal(dto.ColorResizeHandleBackground, settings.ColorResizeHandleBackground);
        Assert.Equal(dto.ColorResizeHandleHoverBackground, settings.ColorResizeHandleHoverBackground);
        Assert.Equal(dto.ColorToggleBackground, settings.ColorToggleBackground);
        Assert.Equal(dto.ColorButtonBackground, settings.ColorButtonBackground);
        Assert.Equal(dto.ColorButtonText, settings.ColorButtonText);
        Assert.Equal(dto.ColorPointer, settings.ColorPointer);
        Assert.Equal(dto.PlayedListFontFamily, settings.PlayedListFontFamily);
        Assert.Equal(dto.PlayedListFontSize, settings.PlayedListFontSize);
        Assert.Equal(dto.PlayedListMaxLines, settings.PlayedListMaxLines);
    }

    [Fact]
    public void Given_Apply_When_DtoHasDifferentValues_Then_OverwritesExistingSettingsValues()
    {
        var settings = CreateFullSettings();
        var dto = new SettingsDto { DebugMode = false, AutoPlay = false, PlayedListMaxLines = 1 };

        _mapper.Apply(dto, settings);

        Assert.False(settings.DebugMode);
        Assert.False(settings.AutoPlay);
        Assert.Equal(1, settings.PlayedListMaxLines);
    }

    // ── Round-trip ───────────────────────────────────────────────────────────

    [Fact]
    public void Given_Settings_When_ToDto_Then_Apply_RoundTrip_ProducesIdenticalValues()
    {
        var source = CreateFullSettings();
        var target = new StreamerSettings();

        _mapper.Apply(_mapper.ToDto(source), target);

        Assert.Equal(source.WheelColors, target.WheelColors);
        Assert.Equal(source.BackgroundMode, target.BackgroundMode);
        Assert.Equal(source.ExcludePlayedSongs, target.ExcludePlayedSongs);
        Assert.Equal(source.AutoPlay, target.AutoPlay);
        Assert.Equal(source.DebugMode, target.DebugMode);
        Assert.Equal(source.ColorPointer, target.ColorPointer);
        Assert.Equal(source.PlayedListMaxLines, target.PlayedListMaxLines);
    }
}