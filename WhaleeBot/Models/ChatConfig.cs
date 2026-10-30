namespace WhaleeBot.Models;

public class ChatConfig
{
    public required double MessageBatchDelaySeconds { get; set; }
    public required int MessageBatchSize { get; set; }
}