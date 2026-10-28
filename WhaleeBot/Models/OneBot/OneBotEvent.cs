using System.Text.Json.Serialization;

namespace WhaleeBot.Models.OneBot;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "post_type")]
[JsonDerivedType(typeof(MessageEvent), "message")]
[JsonDerivedType(typeof(MetaEvent), "meta_event")]
public abstract class OneBotEvent
{
    // public string PostType { get; set; } = string.Empty;
    public long Time { get; set; }
    public long SelfId { get; set; }
}