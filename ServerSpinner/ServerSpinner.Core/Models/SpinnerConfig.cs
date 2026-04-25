namespace ServerSpinner.Core.Models;

public class SpinnerConfig
{
    public static readonly string[] DefaultWheelColors =
    [
        "#ff6b6b", "#4ecdc4", "#45b7d1", "#f9ca24",
        "#6c5ce7", "#a29bfe", "#fd79a8", "#fdcb6e"
    ];

    public bool Debug { get; init; }
    public string[] WheelColors { get; init; } = DefaultWheelColors;
    public SpinnerBackground Background { get; init; } = new();
    public SpinnerStreamerConfig Streamer { get; init; } = new();
    public SpinnerSongListConfig SongList { get; init; } = new();
    public SpinnerPlayedListConfig PlayedList { get; init; } = new();
    public SpinnerColors Colors { get; init; } = new();
}