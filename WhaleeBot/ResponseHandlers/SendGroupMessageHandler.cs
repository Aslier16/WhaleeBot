using System.Net.Http.Json;
using WhaleeBot.Models;
using WhaleeBot.Models.NapcatRequests;
using WhaleeBot.Services;

namespace WhaleeBot.ResponseHandlers;

public class SendGroupMessageHandler(ILogger<SendGroupMessageHandler> logger) : IResponseHandler<SendGroupMessage>
{
    public string Description => "Send a group message to specified {GroupId} with the content: {Message}";

    public string Format => """
                            {"$type":"send_group_msg","group_id":{GroupId},"message":"{Message}"}
                            """;

    public string Path => "/send_group_msg";

    public async Task ProcessAsync(IChatRouter chatRouter, SendGroupMessage request, CancellationToken ctx)
    {
        var payload = new
        {
            group_id = request.GroupId,
            message = request.Message
        };
        var response = await chatRouter.PostNapcatRequestAsync(Path, payload);
        try
        {
            response.EnsureSuccessStatusCode();
            chatRouter.AddMessageHistory(request.GroupId, new ParsedMessage()
            {
                Text = request.Message,
                SenderNickname = "me", // TODO: Insert bot's nickname here if available
                SenderId = 0, // Assuming 0 is the bot's ID; adjust as necessary
            });
            logger.LogInformation(
                "Successfully posted napcat request to {address} with body {body}. Response: {response}",
                Path, payload, await response.Content.ReadAsStringAsync(ctx));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to post napcat request to {Path} with body {body}. Response: {response}",
                Path, payload, await response.Content.ReadAsStringAsync(ctx));
        }
    }
}