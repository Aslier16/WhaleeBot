using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhaleeBot.Models.OneBot;

public class MessageEvent : OneBotEvent
{
    public required string MessageType { get; set; }
    public required string SubType { get; set; }
    public long MessageId { get; set; }
    public List<MessageSegment> Message { get; set; }
    public required Sender Sender { get; set; }
    // sender id (both private and group)
    public long UserId { get; set; }
    
    // group only
    public long? GroupId { get; set; }
    public string? GroupName { get; set; }
}