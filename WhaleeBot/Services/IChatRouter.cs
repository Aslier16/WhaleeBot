using WhaleeBot.Models;
using WhaleeBot.Models.OneBot;

namespace WhaleeBot.Services;

/// <summary>
/// Interface for routing messages to appropriate behaviors
/// </summary>
public interface IChatRouter
{
    Task RouteMessageAsync(MessageEvent messageEvent);
    Task<HttpResponseMessage> PostNapcatRequestAsync(string address, object body);
    void AddMessageHistory(long id, ParsedMessage message);
}