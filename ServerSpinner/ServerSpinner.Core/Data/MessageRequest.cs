namespace ServerSpinner.Core.Data;

public class MessageRequest
{
    public string StreamerId { get; set; } = "";
    public string MessageType { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
}