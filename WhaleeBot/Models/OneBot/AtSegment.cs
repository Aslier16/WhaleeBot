using System.Text.Json.Serialization;

namespace WhaleeBot.Models.OneBot;

public class AtSegment : MessageSegment
{
    [JsonPropertyName("qq")] public required string QQ { get; set; }
}