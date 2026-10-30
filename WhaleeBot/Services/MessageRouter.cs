using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WhaleeBot.Models;
using WhaleeBot.Models.OneBot;
using WhaleeBot.Utils;

namespace WhaleeBot.Services;

public class MessageRouter(ILogger<MessageRouter> logger, ICommandRouter commandRouter, IChatRouter chatRouter) : IMessageRouter
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        AllowOutOfOrderMetadataProperties = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new MessageSegmentConverter() }
    };

    public async Task RouteMessageAsync(Stream messageStream)
    {
#if DEBUG
        using var jsonCache = new MemoryStream();
        await messageStream.CopyToAsync(jsonCache);
        var json = Encoding.UTF8.GetString(jsonCache.ToArray());
        var oneBotEvent = JsonSerializer.Deserialize<OneBotEvent>(json, _serializerOptions);
        // logger.LogDebug("Raw event json: {event}", json);
#else
        var oneBotEvent = await JsonSerializer.DeserializeAsync<OneBotEvent>(messageStream, _serializerOptions);
#endif
        switch (oneBotEvent)
        {
            case null: logger.LogError("null event received"); break;
            case MessageEvent messageEvent: await HandleMessageEventAsync(messageEvent); break;
            case MetaEvent metaEvent: await HandleMetaEventAsync(metaEvent); break;
            default: logger.LogWarning("Unknown event type {eventType}", oneBotEvent.GetType()); break;
        }
    }

    private async Task HandleMessageEventAsync(MessageEvent messageEvent)
    {
        // logger.LogInformation("Handling MessageEvent: {message}", messageEvent.Message);
        await chatRouter.RouteMessageAsync(messageEvent);
    }

    private async Task HandleMetaEventAsync(MetaEvent metaEvent)
    {
        // logger.LogInformation("Received meta event: {meta}", metaEvent.Time);
    }
}