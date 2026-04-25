using ServerSpinner.Core.Models;
using Xunit;

namespace ServerSpinner.Tests.Models;

// ── SpinnerConfig ─────────────────────────────────────────────────────────────

public class SpinnerConfigTests
{
    [Fact]
    public void Given_DefaultWheelColors_When_CountChecked_Then_HasEightEntries()
    {
        Assert.Equal(8, SpinnerConfig.DefaultWheelColors.Length);
    }

    [Fact]
    public void Given_DefaultWheelColors_When_ContentsChecked_Then_HasAllExpectedColors()
    {
        Assert.Contains("#ff6b6b", SpinnerConfig.DefaultWheelColors);
        Assert.Contains("#4ecdc4", SpinnerConfig.DefaultWheelColors);
        Assert.Contains("#45b7d1", SpinnerConfig.DefaultWheelColors);
        Assert.Contains("#f9ca24", SpinnerConfig.DefaultWheelColors);
        Assert.Contains("#6c5ce7", SpinnerConfig.DefaultWheelColors);
        Assert.Contains("#a29bfe", SpinnerConfig.DefaultWheelColors);
        Assert.Contains("#fd79a8", SpinnerConfig.DefaultWheelColors);
        Assert.Contains("#fdcb6e", SpinnerConfig.DefaultWheelColors);
    }

    [Fact]
    public void Given_NewSpinnerConfig_When_DebugRead_Then_IsFalse()
    {
        Assert.False(new SpinnerConfig().Debug);
    }

    [Fact]
    public void Given_NewSpinnerConfig_When_WheelColorsRead_Then_EqualsDefaultWheelColors()
    {
        Assert.Equal(SpinnerConfig.DefaultWheelColors, new SpinnerConfig().WheelColors);
    }

    [Fact]
    public void Given_NewSpinnerConfig_When_BackgroundRead_Then_IsNotNull()
    {
        Assert.NotNull(new SpinnerConfig().Background);
    }

    [Fact]
    public void Given_NewSpinnerConfig_When_StreamerRead_Then_IsNotNull()
    {
        Assert.NotNull(new SpinnerConfig().Streamer);
    }

    [Fact]
    public void Given_NewSpinnerConfig_When_SongListRead_Then_IsNotNull()
    {
        Assert.NotNull(new SpinnerConfig().SongList);
    }

    [Fact]
    public void Given_NewSpinnerConfig_When_PlayedListRead_Then_IsNotNull()
    {
        Assert.NotNull(new SpinnerConfig().PlayedList);
    }

    [Fact]
    public void Given_NewSpinnerConfig_When_ColorsRead_Then_IsNotNull()
    {
        Assert.NotNull(new SpinnerConfig().Colors);
    }
}

// ── SpinnerBackground ─────────────────────────────────────────────────────────

public class SpinnerBackgroundTests
{
    [Fact]
    public void Given_NewSpinnerBackground_When_ModeRead_Then_IsColor()
    {
        Assert.Equal("color", new SpinnerBackground().Mode);
    }

    [Fact]
    public void Given_NewSpinnerBackground_When_ColorRead_Then_IsHashHex111111()
    {
        Assert.Equal("#111111", new SpinnerBackground().Color);
    }

    [Fact]
    public void Given_NewSpinnerBackground_When_ImageRead_Then_IsEmptyString()
    {
        Assert.Equal("", new SpinnerBackground().Image);
    }

    [Fact]
    public void Given_SpinnerBackground_When_ModeSet_Then_ReturnedCorrectly()
    {
        var bg = new SpinnerBackground { Mode = "image" };
        Assert.Equal("image", bg.Mode);
    }

    [Fact]
    public void Given_SpinnerBackground_When_ColorSet_Then_ReturnedCorrectly()
    {
        var bg = new SpinnerBackground { Color = "#aabbcc" };
        Assert.Equal("#aabbcc", bg.Color);
    }

    [Fact]
    public void Given_SpinnerBackground_When_ImageSet_Then_ReturnedCorrectly()
    {
        var bg = new SpinnerBackground { Image = "https://example.com/bg.png" };
        Assert.Equal("https://example.com/bg.png", bg.Image);
    }
}

// ── SpinnerColors ─────────────────────────────────────────────────────────────

public class SpinnerColorsTests
{
    [Fact]
    public void Given_NewSpinnerColors_When_TextRead_Then_IsWhite()
    {
        Assert.Equal("#ffffff", new SpinnerColors().Text);
    }

    [Fact]
    public void Given_NewSpinnerColors_When_StatusBackgroundRead_Then_IsDefaultRgba()
    {
        Assert.Equal("rgba(0, 0, 0, 0.7)", new SpinnerColors().StatusBackground);
    }

    [Fact]
    public void Given_NewSpinnerColors_When_PlayedListBackgroundRead_Then_IsDefaultRgba()
    {
        Assert.Equal("rgba(0, 0, 0, 0.7)", new SpinnerColors().PlayedListBackground);
    }

    [Fact]
    public void Given_NewSpinnerColors_When_PlayedItemBackgroundRead_Then_IsDefault()
    {
        Assert.Equal("#222222", new SpinnerColors().PlayedItemBackground);
    }

    [Fact]
    public void Given_NewSpinnerColors_When_ResizeHandleBackgroundRead_Then_IsDefault()
    {
        Assert.Equal("#333333", new SpinnerColors().ResizeHandleBackground);
    }

    [Fact]
    public void Given_NewSpinnerColors_When_ResizeHandleHoverBackgroundRead_Then_IsDefault()
    {
        Assert.Equal("#555555", new SpinnerColors().ResizeHandleHoverBackground);
    }

    [Fact]
    public void Given_NewSpinnerColors_When_ToggleBackgroundRead_Then_IsDefault()
    {
        Assert.Equal("#222222", new SpinnerColors().ToggleBackground);
    }

    [Fact]
    public void Given_NewSpinnerColors_When_ButtonBackgroundRead_Then_IsDefault()
    {
        Assert.Equal("#555555", new SpinnerColors().ButtonBackground);
    }

    [Fact]
    public void Given_NewSpinnerColors_When_ButtonTextRead_Then_IsDefault()
    {
        Assert.Equal("#CCCCCC", new SpinnerColors().ButtonText);
    }

    [Fact]
    public void Given_NewSpinnerColors_When_PointerRead_Then_IsWheat()
    {
        Assert.Equal("wheat", new SpinnerColors().Pointer);
    }
}

// ── SpinnerStreamerConfig ─────────────────────────────────────────────────────

public class SpinnerStreamerConfigTests
{
    [Fact]
    public void Given_NewSpinnerStreamerConfig_When_DefaultNameRead_Then_IsEmptyString()
    {
        Assert.Equal("", new SpinnerStreamerConfig().DefaultName);
    }

    [Fact]
    public void Given_NewSpinnerStreamerConfig_When_HideChangeOptionWhenDefaultRead_Then_IsTrue()
    {
        Assert.True(new SpinnerStreamerConfig().HideChangeOptionWhenDefault);
    }

    [Fact]
    public void Given_SpinnerStreamerConfig_When_DefaultNameSet_Then_ReturnedCorrectly()
    {
        var config = new SpinnerStreamerConfig { DefaultName = "mystreamer" };
        Assert.Equal("mystreamer", config.DefaultName);
    }

    [Fact]
    public void Given_SpinnerStreamerConfig_When_HideChangeOptionSetToFalse_Then_ReturnedCorrectly()
    {
        var config = new SpinnerStreamerConfig { HideChangeOptionWhenDefault = false };
        Assert.False(config.HideChangeOptionWhenDefault);
    }
}

// ── SpinnerSongListConfig ─────────────────────────────────────────────────────

public class SpinnerSongListConfigTests
{
    [Fact]
    public void Given_NewSpinnerSongListConfig_When_FieldsRead_Then_ContainsArtistAndTitle()
    {
        var config = new SpinnerSongListConfig();
        Assert.Contains("artist", config.Fields);
        Assert.Contains("title", config.Fields);
    }

    [Fact]
    public void Given_NewSpinnerSongListConfig_When_FieldsRead_Then_HasTwoEntries()
    {
        Assert.Equal(2, new SpinnerSongListConfig().Fields.Length);
    }

    [Fact]
    public void Given_NewSpinnerSongListConfig_When_ExcludePlayedSongsRead_Then_IsFalse()
    {
        Assert.False(new SpinnerSongListConfig().ExcludePlayedSongs);
    }

    [Fact]
    public void Given_NewSpinnerSongListConfig_When_PlayedListPositionRead_Then_IsRight()
    {
        Assert.Equal("right", new SpinnerSongListConfig().PlayedListPosition);
    }

    [Fact]
    public void Given_NewSpinnerSongListConfig_When_PlayHistoryPeriodRead_Then_IsWeek()
    {
        Assert.Equal("week", new SpinnerSongListConfig().PlayHistoryPeriod);
    }
}

// ── SpinnerPlayedListConfig ───────────────────────────────────────────────────

public class SpinnerPlayedListConfigTests
{
    [Fact]
    public void Given_NewSpinnerPlayedListConfig_When_FontFamilyRead_Then_IsSansSerif()
    {
        Assert.Equal("sans-serif", new SpinnerPlayedListConfig().FontFamily);
    }

    [Fact]
    public void Given_NewSpinnerPlayedListConfig_When_FontSizeRead_Then_Is0875rem()
    {
        Assert.Equal("0.875rem", new SpinnerPlayedListConfig().FontSize);
    }

    [Fact]
    public void Given_NewSpinnerPlayedListConfig_When_MaxLinesRead_Then_IsTwo()
    {
        Assert.Equal(2, new SpinnerPlayedListConfig().MaxLines);
    }
}

// ── SpinnerSong ───────────────────────────────────────────────────────────────

public class SpinnerSongTests
{
    [Fact]
    public void Given_NewSpinnerSong_When_IdRead_Then_IsNull()
    {
        Assert.Null(new SpinnerSong().Id);
    }

    [Fact]
    public void Given_NewSpinnerSong_When_ArtistRead_Then_IsEmptyString()
    {
        Assert.Equal("", new SpinnerSong().Artist);
    }

    [Fact]
    public void Given_NewSpinnerSong_When_TitleRead_Then_IsEmptyString()
    {
        Assert.Equal("", new SpinnerSong().Title);
    }

    [Fact]
    public void Given_SpinnerSong_When_AllPropertiesSet_Then_ReturnedCorrectly()
    {
        var song = new SpinnerSong { Id = 42, Artist = "Band", Title = "Track" };
        Assert.Equal(42, song.Id);
        Assert.Equal("Band", song.Artist);
        Assert.Equal("Track", song.Title);
    }
}

// ── SpinnerRequest ────────────────────────────────────────────────────────────

public class SpinnerRequestTests
{
    [Fact]
    public void Given_NewSpinnerRequest_When_NameRead_Then_IsEmptyString()
    {
        Assert.Equal("", new SpinnerRequest().Name);
    }

    [Fact]
    public void Given_NewSpinnerRequest_When_DonationAmountRead_Then_IsNull()
    {
        Assert.Null(new SpinnerRequest().DonationAmount);
    }

    [Fact]
    public void Given_NewSpinnerRequest_When_DonationRead_Then_IsNull()
    {
        Assert.Null(new SpinnerRequest().Donation);
    }

    [Fact]
    public void Given_NewSpinnerRequest_When_AmountRead_Then_IsNull()
    {
        Assert.Null(new SpinnerRequest().Amount);
    }

    [Fact]
    public void Given_NewSpinnerRequest_When_PriceRead_Then_IsNull()
    {
        Assert.Null(new SpinnerRequest().Price);
    }
}

// ── SpinnerQueueItem ──────────────────────────────────────────────────────────

public class SpinnerQueueItemTests
{
    [Fact]
    public void Given_NewSpinnerQueueItem_When_SongRead_Then_IsNotNull()
    {
        Assert.NotNull(new SpinnerQueueItem().Song);
    }

    [Fact]
    public void Given_NewSpinnerQueueItem_When_RequestsRead_Then_IsEmptyList()
    {
        Assert.Empty(new SpinnerQueueItem().Requests);
    }

    [Fact]
    public void Given_NewSpinnerQueueItem_When_PositionRead_Then_IsZero()
    {
        Assert.Equal(0, new SpinnerQueueItem().Position);
    }
}

// ── PlayHistoryItem ───────────────────────────────────────────────────────────

public class PlayHistoryItemTests
{
    [Fact]
    public void Given_NewPlayHistoryItem_When_SongRead_Then_IsNull()
    {
        Assert.Null(new PlayHistoryItem().Song);
    }

    [Fact]
    public void Given_NewPlayHistoryItem_When_RequestsRead_Then_IsEmptyList()
    {
        Assert.Empty(new PlayHistoryItem().Requests);
    }
}

// ── PlayHistoryResponse ───────────────────────────────────────────────────────

public class PlayHistoryResponseTests
{
    [Fact]
    public void Given_NewPlayHistoryResponse_When_ItemsRead_Then_IsEmptyList()
    {
        Assert.Empty(new PlayHistoryResponse().Items);
    }

    [Fact]
    public void Given_PlayHistoryResponse_When_ItemsSet_Then_ReturnedCorrectly()
    {
        var response = new PlayHistoryResponse
        {
            Items = [new PlayHistoryItem { Song = new SpinnerSong { Artist = "A" } }]
        };
        Assert.Single(response.Items);
        Assert.Equal("A", response.Items[0].Song!.Artist);
    }
}

// ── QueueResponse ─────────────────────────────────────────────────────────────

public class QueueResponseTests
{
    [Fact]
    public void Given_NewQueueResponse_When_ListRead_Then_IsEmptyList()
    {
        Assert.Empty(new QueueResponse().List);
    }

    [Fact]
    public void Given_QueueResponse_When_ListSet_Then_ReturnedCorrectly()
    {
        var response = new QueueResponse
        {
            List = [new SpinnerQueueItem { Position = 1 }]
        };
        Assert.Single(response.List);
        Assert.Equal(1, response.List[0].Position);
    }
}