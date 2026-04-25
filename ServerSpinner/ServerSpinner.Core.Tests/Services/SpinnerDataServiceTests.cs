using ServerSpinner.Core.Models;
using ServerSpinner.Core.Services;
using Xunit;

namespace ServerSpinner.Core.Tests.Services;

public class SpinnerDataServiceTests
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

    private static SpinnerConfig Cfg(string[]? fields = null, bool exclude = true)
    {
        return new SpinnerConfig
        {
            SongList = new SpinnerSongListConfig
            {
                Fields = fields ?? ["artist", "title"],
                ExcludePlayedSongs = exclude
            }
        };
    }

    // ── SongMatchesPlayed ────────────────────────────────────────────────────

    [Fact]
    public void Given_QueueAndHistoryWithMatchingIds_When_SongMatchesPlayed_Then_ReturnsTrue()
    {
        Assert.True(SpinnerDataService.SongMatchesPlayed(Q(42), H(42)));
    }

    [Fact]
    public void Given_QueueAndHistoryWithDifferentIds_When_SongMatchesPlayed_Then_ReturnsFalse()
    {
        Assert.False(SpinnerDataService.SongMatchesPlayed(Q(1), H(2)));
    }

    [Fact]
    public void Given_SongsWithNoIds_And_MatchingArtistAndTitle_When_SongMatchesPlayed_Then_ReturnsTrue()
    {
        Assert.True(SpinnerDataService.SongMatchesPlayed(Q(), H()));
    }

    [Fact]
    public void Given_SongsWithNoIds_And_DifferentArtist_When_SongMatchesPlayed_Then_ReturnsFalse()
    {
        Assert.False(SpinnerDataService.SongMatchesPlayed(
            Q(artist: "Artist A"), H(artist: "Artist B")));
    }

    [Fact]
    public void Given_SongsWithNoIds_And_DifferentTitle_When_SongMatchesPlayed_Then_ReturnsFalse()
    {
        Assert.False(SpinnerDataService.SongMatchesPlayed(
            Q(title: "Song One"), H(title: "Song Two")));
    }

    [Fact]
    public void Given_SongsWithNoIds_And_ArtistTitleDifferByCase_When_SongMatchesPlayed_Then_ReturnsTrue()
    {
        Assert.True(SpinnerDataService.SongMatchesPlayed(
            Q(artist: "ARTIST A", title: "SONG ONE"),
            H(artist: "artist a", title: "song one")));
    }

    [Fact]
    public void Given_QueueItemSongIsNull_When_SongMatchesPlayed_Then_ReturnsFalse()
    {
        var q = new SpinnerQueueItem { Song = null! };
        Assert.False(SpinnerDataService.SongMatchesPlayed(q, H()));
    }

    [Fact]
    public void Given_HistoryItemSongIsNull_When_SongMatchesPlayed_Then_ReturnsFalse()
    {
        var h = new PlayHistoryItem { Song = null };
        Assert.False(SpinnerDataService.SongMatchesPlayed(Q(), h));
    }

    [Fact]
    public void Given_QueueHasIdButHistoryDoesNot_When_SongMatchesPlayed_Then_FallsBackToArtistTitle()
    {
        Assert.True(SpinnerDataService.SongMatchesPlayed(
            Q(5, "Artist A", "Song One"),
            H(null, "Artist A", "Song One")));
    }

    [Fact]
    public void Given_HistoryHasIdButQueueDoesNot_When_SongMatchesPlayed_Then_FallsBackToArtistTitle()
    {
        Assert.True(SpinnerDataService.SongMatchesPlayed(
            Q(null, "Artist A", "Song One"),
            H(5, "Artist A", "Song One")));
    }

    // ── GetPrimaryRequester ──────────────────────────────────────────────────

    [Fact]
    public void Given_QueueItemWithRequests_When_GetPrimaryRequester_Then_ReturnsFirstName()
    {
        Assert.Equal("User1", SpinnerDataService.GetPrimaryRequester(Q(requester: "User1")));
    }

    [Fact]
    public void Given_QueueItemWithEmptyRequests_When_GetPrimaryRequester_Then_ReturnsUnknown()
    {
        var q = new SpinnerQueueItem { Song = new SpinnerSong(), Requests = [] };
        Assert.Equal("Unknown", SpinnerDataService.GetPrimaryRequester(q));
    }

    [Fact]
    public void Given_QueueItemWithEmptyRequesterName_When_GetPrimaryRequester_Then_ReturnsUnknown()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong(),
            Requests = [new SpinnerRequest { Name = "" }]
        };
        Assert.Equal("Unknown", SpinnerDataService.GetPrimaryRequester(q));
    }

    [Fact]
    public void Given_QueueItemWithMultipleRequesters_When_GetPrimaryRequester_Then_ReturnsFirst()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong(),
            Requests =
            [
                new SpinnerRequest { Name = "First" },
                new SpinnerRequest { Name = "Second" }
            ]
        };
        Assert.Equal("First", SpinnerDataService.GetPrimaryRequester(q));
    }

    // ── BuildWheelLabel ──────────────────────────────────────────────────────

    [Fact]
    public void Given_SongWithArtistTitleAndRequester_When_BuildWheelLabel_Then_ReturnsFormattedLabel()
    {
        Assert.Equal("Artist A - Song One (User1)", SpinnerDataService.BuildWheelLabel(Q()));
    }

    [Fact]
    public void Given_SongWithEmptyArtist_When_BuildWheelLabel_Then_UsesUnknownForArtist()
    {
        Assert.Equal("Unknown - Song One (User1)", SpinnerDataService.BuildWheelLabel(Q(artist: "")));
    }

    [Fact]
    public void Given_SongWithEmptyTitle_When_BuildWheelLabel_Then_UsesUnknownForTitle()
    {
        Assert.Equal("Artist A - Unknown (User1)", SpinnerDataService.BuildWheelLabel(Q(title: "")));
    }

    [Fact]
    public void Given_SongWithNoRequests_When_BuildWheelLabel_Then_LabelContainsUnknownRequester()
    {
        var q = new SpinnerQueueItem { Song = new SpinnerSong { Artist = "A", Title = "T" }, Requests = [] };
        Assert.Equal("A - T (Unknown)", SpinnerDataService.BuildWheelLabel(q));
    }

    // ── FormatDonation ───────────────────────────────────────────────────────

    [Fact]
    public void Given_RequestWithDonationAmount_When_FormatDonation_Then_ReturnsDonationAmountValue()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong(),
            Requests = [new SpinnerRequest { DonationAmount = 5.50m }]
        };
        Assert.Equal("5.50", SpinnerDataService.FormatDonation(q));
    }

    [Fact]
    public void Given_RequestWithNoDonationAmountButHasDonation_When_FormatDonation_Then_ReturnsDonationValue()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong(),
            Requests = [new SpinnerRequest { Donation = 3m }]
        };
        Assert.Equal("3", SpinnerDataService.FormatDonation(q));
    }

    [Fact]
    public void Given_RequestWithNoDonationOrDonationAmountButHasAmount_When_FormatDonation_Then_ReturnsAmountValue()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong(),
            Requests = [new SpinnerRequest { Amount = 2m }]
        };
        Assert.Equal("2", SpinnerDataService.FormatDonation(q));
    }

    [Fact]
    public void Given_RequestWithOnlyPriceField_When_FormatDonation_Then_ReturnsPriceValue()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong(),
            Requests = [new SpinnerRequest { Price = 7.99m }]
        };
        Assert.Equal("7.99", SpinnerDataService.FormatDonation(q));
    }

    [Fact]
    public void Given_RequestWithNoDonationFields_When_FormatDonation_Then_ReturnsNone()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong(),
            Requests = [new SpinnerRequest { Name = "User1" }]
        };
        Assert.Equal("None", SpinnerDataService.FormatDonation(q));
    }

    [Fact]
    public void Given_QueueItemWithNoRequests_When_FormatDonation_Then_ReturnsNone()
    {
        var q = new SpinnerQueueItem { Song = new SpinnerSong(), Requests = [] };
        Assert.Equal("None", SpinnerDataService.FormatDonation(q));
    }

    // ── GetSongFieldValue ────────────────────────────────────────────────────

    [Fact]
    public void Given_ArtistField_When_GetSongFieldValue_Then_ReturnsArtistName()
    {
        Assert.Equal("Artist A", SpinnerDataService.GetSongFieldValue(Q(), "artist"));
    }

    [Fact]
    public void Given_TitleField_When_GetSongFieldValue_Then_ReturnsSongTitle()
    {
        Assert.Equal("Song One", SpinnerDataService.GetSongFieldValue(Q(), "title"));
    }

    [Fact]
    public void Given_RequesterField_When_GetSongFieldValue_Then_ReturnsPrimaryRequester()
    {
        Assert.Equal("User1", SpinnerDataService.GetSongFieldValue(Q(), "requester"));
    }

    [Fact]
    public void Given_DonationField_When_GetSongFieldValue_Then_ReturnsFormattedDonation()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong(),
            Requests = [new SpinnerRequest { DonationAmount = 10m }]
        };
        Assert.Equal("10", SpinnerDataService.GetSongFieldValue(q, "donation"));
    }

    [Fact]
    public void Given_UnknownField_When_GetSongFieldValue_Then_ReturnsEmptyString()
    {
        Assert.Equal("", SpinnerDataService.GetSongFieldValue(Q(), "foobar"));
    }

    [Fact]
    public void Given_FieldNameInUppercase_When_GetSongFieldValue_Then_ReturnsValue()
    {
        Assert.Equal("Artist A", SpinnerDataService.GetSongFieldValue(Q(), "ARTIST"));
    }

    [Fact]
    public void Given_EmptyArtistValue_When_GetSongFieldValue_With_ArtistField_Then_ReturnsUnknown()
    {
        Assert.Equal("Unknown", SpinnerDataService.GetSongFieldValue(Q(artist: ""), "artist"));
    }

    // ── CreateSongTextForFields ──────────────────────────────────────────────

    [Fact]
    public void Given_MultipleValidFields_When_CreateSongTextForFields_Then_JoinsPartsWithPipe()
    {
        var result = SpinnerDataService.CreateSongTextForFields(Q(), ["artist", "title"]);
        Assert.Equal("Artist: Artist A | Title: Song One", result);
    }

    [Fact]
    public void Given_FieldWithNoValue_When_CreateSongTextForFields_Then_SkipsField()
    {
        var result = SpinnerDataService.CreateSongTextForFields(Q(), ["artist", "foobar"]);
        Assert.Equal("Artist: Artist A", result);
    }

    [Fact]
    public void Given_SingleField_When_CreateSongTextForFields_Then_CapitalizesFieldLabel()
    {
        var result = SpinnerDataService.CreateSongTextForFields(Q(), ["requester"]);
        Assert.StartsWith("Requester:", result);
    }

    [Fact]
    public void Given_AllUnknownFields_When_CreateSongTextForFields_Then_ReturnsEmptyString()
    {
        var result = SpinnerDataService.CreateSongTextForFields(Q(), ["unknown1", "unknown2"]);
        Assert.Equal("", result);
    }

    [Fact]
    public void Given_EmptyFieldList_When_CreateSongTextForFields_Then_ReturnsEmptyString()
    {
        var result = SpinnerDataService.CreateSongTextForFields(Q(), []);
        Assert.Equal("", result);
    }

    // ── CreatePlayedSongText (SpinnerQueueItem overload) ─────────────────────

    [Fact]
    public void Given_ConfigWithSpecificFields_When_CreatePlayedSongText_QueueItem_Then_UsesConfigFields()
    {
        var result = SpinnerDataService.CreatePlayedSongText(Q(), Cfg(["artist", "requester"]));
        Assert.Equal("Artist: Artist A | Requester: User1", result);
    }

    [Fact]
    public void Given_ConfigWithEmptyFields_When_CreatePlayedSongText_QueueItem_Then_DefaultsToArtistTitle()
    {
        var cfg = new SpinnerConfig { SongList = new SpinnerSongListConfig { Fields = [] } };
        var result = SpinnerDataService.CreatePlayedSongText(Q(), cfg);
        Assert.Equal("Artist: Artist A | Title: Song One", result);
    }

    [Fact]
    public void Given_ConfigWithDonationField_When_CreatePlayedSongText_QueueItem_Then_IncludesDonation()
    {
        var q = new SpinnerQueueItem
        {
            Song = new SpinnerSong { Artist = "Band", Title = "Track" },
            Requests = [new SpinnerRequest { Name = "Fan", DonationAmount = 5m }]
        };
        var result = SpinnerDataService.CreatePlayedSongText(q, Cfg(["artist", "donation"]));
        Assert.Equal("Artist: Band | Donation: 5", result);
    }

    // ── CreatePlayedSongText (PlayHistoryItem overload) ──────────────────────

    [Fact]
    public void Given_HistoryItemWithSong_When_CreatePlayedSongText_HistoryItem_Then_ReturnsArtistAndTitle()
    {
        var result = SpinnerDataService.CreatePlayedSongText(H(), Cfg(["artist", "title"]));
        Assert.Equal("Artist: Artist A | Title: Song One", result);
    }

    [Fact]
    public void
        Given_HistoryItemWithEmptyConfigFields_When_CreatePlayedSongText_HistoryItem_Then_DefaultsToArtistTitle()
    {
        var cfg = new SpinnerConfig { SongList = new SpinnerSongListConfig { Fields = [] } };
        var result = SpinnerDataService.CreatePlayedSongText(H(), cfg);
        Assert.Equal("Artist: Artist A | Title: Song One", result);
    }

    [Fact]
    public void Given_HistoryItemWithRequester_When_CreatePlayedSongText_HistoryItem_Then_IncludesRequester()
    {
        var h = H(requester: "Fan1");
        var result = SpinnerDataService.CreatePlayedSongText(h, Cfg(["artist", "requester"]));
        Assert.Equal("Artist: Artist A | Requester: Fan1", result);
    }

    [Fact]
    public void Given_HistoryItemWithDonation_When_CreatePlayedSongText_HistoryItem_Then_IncludesDonation()
    {
        var h = H(requester: "Fan1", donationAmount: 5m);
        var result = SpinnerDataService.CreatePlayedSongText(h, Cfg(["artist", "donation"]));
        Assert.Equal("Artist: Artist A | Donation: 5", result);
    }

    [Fact]
    public void Given_HistoryItemWithNoDonation_When_CreatePlayedSongText_HistoryItem_Then_OmitsDonationField()
    {
        var result = SpinnerDataService.CreatePlayedSongText(H(), Cfg(["artist", "donation"]));
        Assert.Equal("Artist: Artist A", result);
    }

    [Fact]
    public void Given_HistoryItemWithNullSong_When_CreatePlayedSongText_HistoryItem_Then_ReturnsUnknownValues()
    {
        var h = new PlayHistoryItem { Song = null };
        var result = SpinnerDataService.CreatePlayedSongText(h, Cfg(["artist", "title"]));
        Assert.Equal("Artist: Unknown | Title: Unknown", result);
    }

    // ── GetWinnerFields ──────────────────────────────────────────────────────

    [Fact]
    public void Given_ConfigWithoutRequesterField_When_GetWinnerFields_Then_AddsRequester()
    {
        var fields = SpinnerDataService.GetWinnerFields(Cfg(["artist", "title"]));
        Assert.Contains("requester", fields);
    }

    [Fact]
    public void Given_ConfigWithRequesterAlreadyPresent_When_GetWinnerFields_Then_DoesNotDuplicateRequester()
    {
        var fields = SpinnerDataService.GetWinnerFields(Cfg(["artist", "requester"]));
        Assert.Single(fields, f => f == "requester");
    }

    [Fact]
    public void Given_ConfigWithMultipleFields_When_GetWinnerFields_Then_PreservesAllOriginalFields()
    {
        var fields = SpinnerDataService.GetWinnerFields(Cfg(["artist", "title"]));
        Assert.Contains("artist", fields);
        Assert.Contains("title", fields);
    }

    [Fact]
    public void Given_ConfigWithEmptyFields_When_GetWinnerFields_Then_DefaultsToArtistTitleAndRequester()
    {
        var cfg = new SpinnerConfig { SongList = new SpinnerSongListConfig { Fields = [] } };
        var fields = SpinnerDataService.GetWinnerFields(cfg);
        Assert.Contains("artist", fields);
        Assert.Contains("title", fields);
        Assert.Contains("requester", fields);
    }

    [Fact]
    public void Given_FieldsInMixedCase_When_GetWinnerFields_Then_ReturnsLowercaseFields()
    {
        var fields = SpinnerDataService.GetWinnerFields(Cfg(["ARTIST", "Title"]));
        Assert.Contains("artist", fields);
        Assert.Contains("title", fields);
    }

    // ── FilterAvailableSongs ─────────────────────────────────────────────────

    [Fact]
    public void Given_ExcludeDisabled_When_FilterAvailableSongs_Then_ReturnsAllSongs()
    {
        var all = new List<SpinnerQueueItem> { Q(1), Q(2) };
        var played = new List<PlayHistoryItem> { H(1) };
        var result = SpinnerDataService.FilterAvailableSongs(all, played, Cfg(exclude: false));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Given_ExcludeEnabled_And_SomePlayedById_When_FilterAvailableSongs_Then_ExcludesPlayedSongs()
    {
        var all = new List<SpinnerQueueItem> { Q(1), Q(2) };
        var played = new List<PlayHistoryItem> { H(1) };
        var result = SpinnerDataService.FilterAvailableSongs(all, played, Cfg(exclude: true));
        Assert.Single(result);
        Assert.Equal(2, result[0].Song.Id);
    }

    [Fact]
    public void Given_ExcludeEnabled_And_NoPlayedSongs_When_FilterAvailableSongs_Then_ReturnsAll()
    {
        var all = new List<SpinnerQueueItem> { Q(1), Q(2) };
        var result = SpinnerDataService.FilterAvailableSongs(all, [], Cfg(exclude: true));
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Given_ExcludeEnabled_And_AllSongsPlayed_When_FilterAvailableSongs_Then_ReturnsEmpty()
    {
        var all = new List<SpinnerQueueItem> { Q(1), Q(2) };
        var played = new List<PlayHistoryItem> { H(1), H(2) };
        var result = SpinnerDataService.FilterAvailableSongs(all, played, Cfg(exclude: true));
        Assert.Empty(result);
    }

    [Fact]
    public void Given_ExcludeEnabled_And_MatchByArtistTitle_When_FilterAvailableSongs_Then_ExcludesMatch()
    {
        var all = new List<SpinnerQueueItem>
        {
            Q(artist: "X", title: "Y"),
            Q(artist: "A", title: "B")
        };
        var played = new List<PlayHistoryItem> { H(artist: "X", title: "Y") };
        var result = SpinnerDataService.FilterAvailableSongs(all, played, Cfg(exclude: true));
        Assert.Single(result);
        Assert.Equal("A", result[0].Song.Artist);
    }

    [Fact]
    public void Given_EmptyAllSongs_When_FilterAvailableSongs_Then_ReturnsEmpty()
    {
        var played = new List<PlayHistoryItem> { H(1) };
        var result = SpinnerDataService.FilterAvailableSongs([], played, Cfg(exclude: true));
        Assert.Empty(result);
    }
}