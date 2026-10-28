using WhaleeBot.Services;

namespace WhaleeBot.Models.NapcatRequests;

public class SendPrivateMessage : NapcatRequestBase
{
    public required long UserId { get; set; }
    public required string Message { get; set; }
}