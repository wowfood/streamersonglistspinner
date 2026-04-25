using Microsoft.Extensions.Logging.Abstractions;
using ServerSpinner.Functions.Entities;
using ServerSpinner.Functions.Services;
using Xunit;

namespace ServerSpinner.Functions.Tests.Services;

public class SpinnerConfigMapperTests
{
    private static readonly string[] DefaultWheelColors =
        ["#ff6b6b", "#4ecdc4", "#45b7d1", "#f9ca24", "#6c5ce7", "#a29bfe", "#fd79a8", "#fdcb6e"];

    private static readonly string[] DefaultFields = ["artist", "title"];

    private static SpinnerConfigMapper CreateMapper()
    {
        return new SpinnerConfigMapper(NullLogger<SpinnerConfigMapper>.Instance);
    }

    // ── WheelColors parsing ───────────────────────────────────────────────────

    [Fact]
    public void Given_ValidWheelColorsJson_When_ToConfigResponse_Then_ParsesWheelColorsCorrectly()
    {
        var settings = new StreamerSettings { WheelColors = """["#ff0000","#00ff00","#0000ff"]""" };

        var result = CreateMapper().ToConfigResponse(settings);

        Assert.Equal(["#ff0000", "#00ff00", "#0000ff"], result.WheelColors);
    }

    [Fact]
    public void Given_InvalidWheelColorsJson_When_ToConfigResponse_Then_UsesDefaultWheelColors()
    {
        var settings = new StreamerSettings { WheelColors = "not-valid-json" };

        var result = CreateMapper().ToConfigResponse(settings);

        Assert.Equal(DefaultWheelColors, result.WheelColors);
    }

    [Fact]
    public void Given_EmptyWheelColorsJson_When_ToConfigResponse_Then_UsesDefaultWheelColors()
    {
        var settings = new StreamerSettings { WheelColors = "null" };

        var result = CreateMapper().ToConfigResponse(settings);

        Assert.Equal(DefaultWheelColors, result.WheelColors);
    }

    // ── SongListFields parsing ────────────────────────────────────────────────

    [Fact]
    public void Given_ValidSongListFieldsJson_When_ToConfigResponse_Then_ParsesFieldsCorrectly()
    {
        var settings = new StreamerSettings { SongListFields = """["artist","requester","donation"]""" };

        var result = CreateMapper().ToConfigResponse(settings);

        Assert.Equal(["artist", "requester", "donation"], result.SongList.Fields);
    }

    [Fact]
    public void Given_InvalidSongListFieldsJson_When_ToConfigResponse_Then_UsesDefaultFields()
    {
        var settings = new StreamerSettings { SongListFields = "{invalid}" };

        var result = CreateMapper().ToConfigResponse(settings);

        Assert.Equal(DefaultFields, result.SongList.Fields);
    }

    // ── Debug ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Given_DebugModeTrue_When_ToConfigResponse_Then_DebugIsTrue()
    {
        var settings = new StreamerSettings { DebugMode = true };

        var result = CreateMapper().ToConfigResponse(settings);

        Assert.True(result.Debug);
    }

    [Fact]
    public void Given_DebugModeFalse_When_ToConfigResponse_Then_DebugIsFalse()
    {
        var settings = new StreamerSettings { DebugMode = false };

        var result = CreateMapper().ToConfigResponse(settings);

        Assert.False(result.Debug);
    }

    // ── Background ────────────────────────────────────────────────────────────

    [Fact]
    public void Given_CustomBackgroundSettings_When_ToConfigResponse_Then_BackgroundIsMapped()
    {
        var settings = new StreamerSettings
        {
            BackgroundMode = "image",
            BackgroundColor = "#aabbcc",
            BackgroundImage = "https://example.com/bg.jpg"
        };

        var result = CreateMapper().ToConfigResponse(settings);

        Assert.Equal("image", result.Background.Mode);
        Assert.Equal("#aabbcc", result.Background.Color);
        Assert.Equal("https://example.com/bg.jpg", result.Background.Image);
    }

    // ── Streamer ──────────────────────────────────────────────────────────────

    [Fact]
    public void Given_CustomStreamerSettings_When_ToConfigResponse_Then_StreamerIsMapped()
    {
        var settings = new StreamerSettings
        {
            DefaultStreamerName = "CoolStreamer",
            HideChangeOptionWhenDefault = false
        };

        var result = CreateMapper().ToConfigResponse(settings);

        Assert.Equal("CoolStreamer", result.Streamer.DefaultName);
        Assert.False(result.Streamer.HideChangeOptionWhenDefault);
    }

    // ── SongList ──────────────────────────────────────────────────────────────

    [Fact]
    public void Given_CustomSongListSettings_When_ToConfigResponse_Then_SongListIsMapped()
    {
        var settings = new StreamerSettings
        {
            ExcludePlayedSongs = true,
            PlayedListPosition = "left",
            PlayHistoryPeriod = "day"
        };

        var result = CreateMapper().ToConfigResponse(settings);

        Assert.True(result.SongList.ExcludePlayedSongs);
        Assert.Equal("left", result.SongList.PlayedListPosition);
        Assert.Equal("day", result.SongList.PlayHistoryPeriod);
    }

    // ── PlayedList ────────────────────────────────────────────────────────────

    [Fact]
    public void Given_CustomPlayedListSettings_When_ToConfigResponse_Then_PlayedListIsMapped()
    {
        var settings = new StreamerSettings
        {
            PlayedListFontFamily = "monospace",
            PlayedListFontSize = "1.25rem",
            PlayedListMaxLines = 4
        };

        var result = CreateMapper().ToConfigResponse(settings);

        Assert.Equal("monospace", result.PlayedList.FontFamily);
        Assert.Equal("1.25rem", result.PlayedList.FontSize);
        Assert.Equal(4, result.PlayedList.MaxLines);
    }

    // ── Colors ────────────────────────────────────────────────────────────────

    [Fact]
    public void Given_CustomColorSettings_When_ToConfigResponse_Then_AllColorsAreMapped()
    {
        var settings = new StreamerSettings
        {
            ColorText = "#111111",
            ColorStatusBackground = "#222222",
            ColorPlayedListBackground = "#333333",
            ColorPlayedItemBackground = "#444444",
            ColorResizeHandleBackground = "#555555",
            ColorResizeHandleHoverBackground = "#666666",
            ColorToggleBackground = "#777777",
            ColorButtonBackground = "#888888",
            ColorButtonText = "#999999",
            ColorPointer = "blue"
        };

        var result = CreateMapper().ToConfigResponse(settings);

        Assert.Equal("#111111", result.Colors.Text);
        Assert.Equal("#222222", result.Colors.StatusBackground);
        Assert.Equal("#333333", result.Colors.PlayedListBackground);
        Assert.Equal("#444444", result.Colors.PlayedItemBackground);
        Assert.Equal("#555555", result.Colors.ResizeHandleBackground);
        Assert.Equal("#666666", result.Colors.ResizeHandleHoverBackground);
        Assert.Equal("#777777", result.Colors.ToggleBackground);
        Assert.Equal("#888888", result.Colors.ButtonBackground);
        Assert.Equal("#999999", result.Colors.ButtonText);
        Assert.Equal("blue", result.Colors.Pointer);
    }
}