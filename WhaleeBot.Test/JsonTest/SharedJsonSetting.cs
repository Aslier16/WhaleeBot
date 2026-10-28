using System.Text.Json;
using WhaleeBot.Utils;

namespace WhaleeBot.Test.JsonTest;

public class SharedJsonSetting
{
    public static JsonSerializerOptions JsonOptions = new()
    {
        AllowOutOfOrderMetadataProperties = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new MessageSegmentConverter() }
    };
}