using WhaleeBot.Commands;
using WhaleeBot.Models;

namespace WhaleeBot.Services;

public interface ICommandRouter
{
    void RouteMessage(ParsedMessage rawMessage);
}