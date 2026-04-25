using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ServerSpinner.Functions.Entities;
using ServerSpinner.Functions.Data;
using ServerSpinner.Functions.Services;
using Xunit;

namespace ServerSpinner.Functions.Tests.Services;

public class SpinnerStateServiceTests
{
    private static readonly Guid StreamerId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");

    private static AppDbContext CreateContext()
    {
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    private static SpinnerStateService CreateService(AppDbContext db)
    {
        return new SpinnerStateService(db, NullLogger<SpinnerStateService>.Instance);
    }

    private static string SetStreamerPayload(string name)
    {
        return JsonSerializer.Serialize(new { streamer = name });
    }

    private static string SpinCommandPayload(string songId, int queuePosition = 1)
    {
        return JsonSerializer.Serialize(new { songId, queuePosition });
    }

    private static string EmptyPayload()
    {
        return "{}";
    }

    // ── set_streamer / client_state_push ─────────────────────────────────────

    [Fact]
    public async Task Given_SetStreamer_And_NoExistingState_When_UpdateAsync_Then_CreatesNewStateWithStreamerName()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await service.UpdateAsync(StreamerId, "set_streamer", SetStreamerPayload("StreamerA"));

        var state = await db.SpinnerStates.FindAsync([StreamerId], TestContext.Current.CancellationToken);
        Assert.NotNull(state);
        Assert.Equal("StreamerA", state.CurrentStreamer);
    }

    [Fact]
    public async Task Given_SetStreamer_And_ExistingState_When_UpdateAsync_Then_UpdatesStreamerName()
    {
        await using var db = CreateContext();
        db.SpinnerStates.Add(new SpinnerState { StreamerId = StreamerId, CurrentStreamer = "OldName" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);

        await service.UpdateAsync(StreamerId, "set_streamer", SetStreamerPayload("NewName"));

        var state = await db.SpinnerStates.FindAsync([StreamerId], TestContext.Current.CancellationToken);
        Assert.Equal("NewName", state!.CurrentStreamer);
    }

    [Fact]
    public async Task Given_ClientStatePush_When_UpdateAsync_Then_UpdatesStreamerName()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await service.UpdateAsync(StreamerId, "client_state_push", SetStreamerPayload("PushedStreamer"));

        var state = await db.SpinnerStates.FindAsync([StreamerId], TestContext.Current.CancellationToken);
        Assert.Equal("PushedStreamer", state!.CurrentStreamer);
    }

    // ── spin_command ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_SpinCommand_And_NoExistingState_When_UpdateAsync_Then_CreatesStateWithSongId()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await service.UpdateAsync(StreamerId, "spin_command", SpinCommandPayload("song-42"));

        var state = await db.SpinnerStates.FindAsync([StreamerId], TestContext.Current.CancellationToken);
        var ids = JsonSerializer.Deserialize<List<string>>(state!.PlayedSongIdsJson)!;
        Assert.Contains("song-42", ids);
    }

    [Fact]
    public async Task Given_SpinCommand_And_ExistingPlayedSongs_When_UpdateAsync_Then_AppendsSongId()
    {
        await using var db = CreateContext();
        db.SpinnerStates.Add(new SpinnerState
        {
            StreamerId = StreamerId,
            PlayedSongIdsJson = """["song-1"]"""
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);

        await service.UpdateAsync(StreamerId, "spin_command", SpinCommandPayload("song-2"));

        var state = await db.SpinnerStates.FindAsync([StreamerId], TestContext.Current.CancellationToken);
        var ids = JsonSerializer.Deserialize<List<string>>(state!.PlayedSongIdsJson)!;
        Assert.Equal(2, ids.Count);
        Assert.Contains("song-1", ids);
        Assert.Contains("song-2", ids);
    }

    [Fact]
    public async Task Given_SpinCommand_And_SongAlreadyPlayed_When_UpdateAsync_Then_DoesNotDuplicateSongId()
    {
        await using var db = CreateContext();
        db.SpinnerStates.Add(new SpinnerState
        {
            StreamerId = StreamerId,
            PlayedSongIdsJson = """["song-42"]"""
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);

        await service.UpdateAsync(StreamerId, "spin_command", SpinCommandPayload("song-42"));

        var state = await db.SpinnerStates.FindAsync([StreamerId], TestContext.Current.CancellationToken);
        var ids = JsonSerializer.Deserialize<List<string>>(state!.PlayedSongIdsJson)!;
        Assert.Single(ids);
    }

    [Fact]
    public async Task Given_SpinCommand_And_CorruptPlayedSongIdsJson_When_UpdateAsync_Then_ResetsAndAddsSongId()
    {
        await using var db = CreateContext();
        db.SpinnerStates.Add(new SpinnerState
        {
            StreamerId = StreamerId,
            PlayedSongIdsJson = "NOT-VALID-JSON"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);

        await service.UpdateAsync(StreamerId, "spin_command", SpinCommandPayload("song-99"));

        var state = await db.SpinnerStates.FindAsync([StreamerId], TestContext.Current.CancellationToken);
        var ids = JsonSerializer.Deserialize<List<string>>(state!.PlayedSongIdsJson)!;
        Assert.Single(ids);
        Assert.Contains("song-99", ids);
    }

    // ── reset_played ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Given_ResetPlayed_And_ExistingPlayedSongs_When_UpdateAsync_Then_ClearsPlayedSongIds()
    {
        await using var db = CreateContext();
        db.SpinnerStates.Add(new SpinnerState
        {
            StreamerId = StreamerId,
            PlayedSongIdsJson = """["song-1","song-2","song-3"]"""
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);

        await service.UpdateAsync(StreamerId, "reset_played", EmptyPayload());

        var state = await db.SpinnerStates.FindAsync([StreamerId], TestContext.Current.CancellationToken);
        Assert.Equal("[]", state!.PlayedSongIdsJson);
    }

    [Fact]
    public async Task Given_ResetPlayed_And_NoExistingState_When_UpdateAsync_Then_NoStateIsCreated()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await service.UpdateAsync(StreamerId, "reset_played", EmptyPayload());

        var state = await db.SpinnerStates.FindAsync([StreamerId], TestContext.Current.CancellationToken);
        Assert.Null(state);
    }

    // ── Unknown message type ──────────────────────────────────────────────────

    [Fact]
    public async Task Given_UnrecognizedMessageType_When_UpdateAsync_Then_NoStateIsCreated()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        await service.UpdateAsync(StreamerId, "unknown_type", EmptyPayload());

        var state = await db.SpinnerStates.FindAsync([StreamerId], TestContext.Current.CancellationToken);
        Assert.Null(state);
    }

    [Fact]
    public async Task Given_UnrecognizedMessageType_And_ExistingState_When_UpdateAsync_Then_StateIsUnchanged()
    {
        await using var db = CreateContext();
        db.SpinnerStates.Add(new SpinnerState
        {
            StreamerId = StreamerId,
            CurrentStreamer = "Original",
            PlayedSongIdsJson = """["song-1"]"""
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = CreateService(db);

        await service.UpdateAsync(StreamerId, "unknown_type", EmptyPayload());

        var state = await db.SpinnerStates.FindAsync([StreamerId], TestContext.Current.CancellationToken);
        Assert.Equal("Original", state!.CurrentStreamer);
        Assert.Equal("""["song-1"]""", state.PlayedSongIdsJson);
    }
}