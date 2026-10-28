using System.Net.Http.Json;
using WhaleeBot.Models.NapcatRequests;
using WhaleeBot.Services;

namespace WhaleeBot.ResponseHandlers;

public class SendPrivateMessageHandler : IResponseHandler<SendPrivateMessage>
{
    public string Description => "Send a private message to user {UserId} with the content: {Message}";

    public string Format => """
                            {"$type":"send_private_msg","user_id":{UserId},"message":"{Message}"}
                            """;

    public string Path => "/send_private_msg";

    public Task ProcessAsync(IChatRouter chatRouter, SendPrivateMessage request, CancellationToken ctx)
    {
        var payload = new
        {
            user_id = request.UserId,
            message = request.Message
        };
        return chatRouter.PostNapcatRequestAsync(Path, payload);
    }
}