namespace ServerSpinner.Functions.Entities;

public class SpinnerState
{
    public Guid StreamerId { get; set; }
    public string CurrentStreamer { get; set; } = "";
    public string PlayedSongIdsJson { get; set; } = "[]";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
