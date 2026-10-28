using System.Text.Json;
using System.Text.Json.Serialization;
using WhaleeBot.Models.OneBot;

namespace WhaleeBot.Utils;

public class MessageSegmentConverter : JsonConverter<MessageSegment>
{
    public override MessageSegment? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string propertyName = reader.GetString()!;
                if (propertyName == "type")
                {
                    reader.Read();
                    string type = reader.GetString()!;
                    reader.Read(); // Move to data PropertyName
                    reader.Read(); // Move to data value
                    MessageSegment segment = type switch
                    {
                        "text" => JsonSerializer.Deserialize<TextSegment>(ref reader, options)!,
                        "image" => JsonSerializer.Deserialize<ImageSegment>(ref reader, options)!,
                        "at" => JsonSerializer.Deserialize<AtSegment>(ref reader, options)!,
                        _ => throw new JsonException($"Unknown message segment type: {type}")
                    };
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }
                        // reader.Skip();
                    }
                    return segment;
                }
            }
        }

        throw new JsonException("Invalid message segment JSON");
    }

    public override void Write(Utf8JsonWriter writer, MessageSegment value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}