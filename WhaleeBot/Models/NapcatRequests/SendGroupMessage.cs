namespace WhaleeBot.Models.NapcatRequests;

public class SendGroupMessage : NapcatRequestBase
{
    public long GroupId { get; init; }
    public required string Message { get; init; }
}