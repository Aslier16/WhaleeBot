namespace WhaleeBot.Services;

public interface IMessageRouter
{
    Task RouteMessageAsync(Stream messageStream);
}