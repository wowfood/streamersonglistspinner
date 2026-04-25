using ServerSpinner.Core.Models;
using ServerSpinner.Core.Services;
using Xunit;

namespace ServerSpinner.Tests.Services;

public class SpinnerDataServiceEdgeCaseTests
{
    private static SpinnerQueueItem Q(
        int? id = null,
        string artist = "Artist A",
        string title = "Song One",
        string requester = "User1",
        decimal? donation = null)
    {
        return new SpinnerQueueItem
        {
            Song = new SpinnerSong { Id = id, Artist = artist, Title = title },
            Requests = [new SpinnerRequest { Name = requester, DonationAmount = donation }]
        };
    }

    private static PlayHistoryItem H(
        int? id = null,
        string artist = "Artist A",
        string title = "Song One",
        string requester = "",
        decimal? donationAmount = null)
    {
        return new PlayHistoryItem
        {
            Song = new SpinnerSong { Id = id, Artist = artist, Title = title },
            Requests = requester.Length > 0
                ? [new SpinnerRequest { Name = requester, DonationAmount = donationAmount }]
                : []
        };
    }

    // ── GetSongFieldValue ────────────────────────────────────────────────────

    [Fact]
    public void Given_EmptyTitleValue_When_GetSongFieldValue_With_TitleField_Then_ReturnsUnknown()
    {
        Assert.Equal("Unknown", SpinnerDataService.GetSongFieldValue(Q(title: ""), "title"));
    }

    // ── FormatDonation – priority ordering ───────────────────────────────────

    [Fact]
    public void Given_DonationAmountAndDonationBothSet_When_FormatDonation_Then_DonationAmountTakesPriority()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong(),
            Requests = [new SpinnerRequest { DonationAmount = 5m, Donation = 3m }]
        };
        Assert.Equal("5", SpinnerDataService.FormatDonation(q));
    }

    [Fact]
    public void Given_DonationAndAmountBothSet_When_FormatDonation_Then_DonationTakesPriority()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong(),
            Requests = [new SpinnerRequest { Donation = 3m, Amount = 2m }]
        };
        Assert.Equal("3", SpinnerDataService.FormatDonation(q));
    }

    [Fact]
    public void Given_AmountAndPriceBothSet_When_FormatDonation_Then_AmountTakesPriority()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong(),
            Requests = [new SpinnerRequest { Amount = 2m, Price = 7.99m }]
        };
        Assert.Equal("2", SpinnerDataService.FormatDonation(q));
    }

    // ── CreatePlayedSongText (PlayHistoryItem overload) ──────────────────────

    [Fact]
    public void Given_HistoryItemWithNoRequests_When_CreatePlayedSongText_With_RequesterField_Then_ShowsUnknown()
    {
        var h = new PlayHistoryItem
        {
            Song = new SpinnerSong { Artist = "Artist A", Title = "Song One" },
            Requests = []
        };
        var cfg = new SpinnerConfig { SongList = new SpinnerSongListConfig { Fields = ["requester"] } };
        Assert.Equal("Requester: Unknown", SpinnerDataService.CreatePlayedSongText(h, cfg));
    }

    [Fact]
    public void Given_HistoryItemWithDonationAndRequesterFields_When_RequesterHasDonation_Then_BothShown()
    {
        var h = H(requester: "Fan", donationAmount: 10m);
        var cfg = new SpinnerConfig
        {
            SongList = new SpinnerSongListConfig { Fields = ["requester", "donation"] }
        };
        Assert.Equal("Requester: Fan | Donation: 10", SpinnerDataService.CreatePlayedSongText(h, cfg));
    }

    // ── SongMatchesPlayed – null item handling ────────────────────────────────

    [Fact]
    public void Given_NullQueueItem_When_SongMatchesPlayed_Then_ReturnsFalse()
    {
        Assert.False(SpinnerDataService.SongMatchesPlayed(null!, H()));
    }

    [Fact]
    public void Given_NullPlayedItem_When_SongMatchesPlayed_Then_ReturnsFalse()
    {
        Assert.False(SpinnerDataService.SongMatchesPlayed(Q(), null!));
    }

    // ── BuildWheelLabel – multiple requesters ────────────────────────────────

    [Fact]
    public void Given_MultipleRequesters_When_BuildWheelLabel_Then_UsesFirstRequester()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong { Artist = "Artist", Title = "Song" },
            Requests =
            [
                new SpinnerRequest { Name = "Primary" },
                new SpinnerRequest { Name = "Secondary" }
            ]
        };
        Assert.Equal("Artist - Song (Primary)", SpinnerDataService.BuildWheelLabel(q));
    }
}