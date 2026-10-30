using System.Net.Http.Json;
using WhaleeBot.Models;
using WhaleeBot.Models.NapcatRequests;
using WhaleeBot.Services;

namespace WhaleeBot.ResponseHandlers;

public class SendPrivateMessageHandler(ILogger<SendPrivateMessageHandler> logger) : IResponseHandler<SendPrivateMessage>
{
    public string Description => "Send a private message to user {UserId} with the content: {Message}";

    public string Format => """
                            {"$type":"send_private_msg","user_id":{UserId},"message":"{Message}"}
                            """;

    public string Path => "/send_private_msg";

    public async Task ProcessAsync(IChatRouter chatRouter, SendPrivateMessage request, CancellationToken ctx)
    {
        var payload = new
        {
            user_id = request.UserId,
            message = request.Message
        };
        var response = await chatRouter.PostNapcatRequestAsync(Path, payload);
        try
        {
            response.EnsureSuccessStatusCode();
            chatRouter.AddMessageHistory(request.UserId, new ParsedMessage()
            {
                Text = request.Message,
                SenderNickname = "me", // TODO: Insert bot's nickname here if available
                SenderId = 0, // Assuming 0 is the bot's ID; adjust as necessary
            });
            logger.LogInformation(
                "Successfully posted napcat request to \"{address}\" with body \"{body}\". Response: {response}",
                Path, payload, await response.Content.ReadAsStringAsync(ctx));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to post napcat request to \"{address}\" with body \"{body}\". Response: {response}",
                Path, payload, await response.Content.ReadAsStringAsync(ctx));
        }
    }
}