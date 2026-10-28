using System.Net.Http.Json;
using WhaleeBot.Models.NapcatRequests;
using WhaleeBot.Services;

namespace WhaleeBot.ResponseHandlers;

public class SendGroupMessageHandler : IResponseHandler<SendGroupMessage>
{
    public string Description => "Send a group message to specified {GroupId} with the content: {Message}";

    public string Format => """
                            {"$type":"send_group_msg","group_id":{GroupId},"message":"{Message}"}
                            """;

    public string Path => "/send_group_msg";

    public Task ProcessAsync(IChatRouter chatRouter, SendGroupMessage request, CancellationToken ctx)
    {
        var payload = new
        {
            group_id = request.GroupId,
            message = request.Message
        };
        return chatRouter.PostNapcatRequestAsync(Path, payload);
    }
}