namespace WhaleeBot.Services;

public interface IResponseRouter
{
    Task RouteResponseAsync(string response);
}