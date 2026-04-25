namespace ServerSpinner.Core.Models;

public class SpinnerSongListConfig
{
    public string[] Fields { get; set; } = ["artist", "title"];
    public bool ExcludePlayedSongs { get; set; }
    public string PlayedListPosition { get; set; } = "right";
    public string PlayHistoryPeriod { get; set; } = "week";
}