using WhaleeBot.Commands;
using WhaleeBot.Models;

namespace WhaleeBot.Services;

public class CommandRouter : ICommandRouter
{
    private Dictionary<string, IMessageCommand> _commands = new(16);
    private readonly ILogger<CommandRouter> _logger;

    public CommandRouter(ILogger<CommandRouter> logger, IEnumerable<IMessageCommand> commands)
    {
        _logger = logger;
        foreach (var command in commands)
        {
            _commands.Add(command.CommandKeyword, command);
        }
    }

    public void RouteMessage(ParsedMessage rawMessage)
    {
        var keyword = rawMessage.Text.Split(' ')[0];
        if (_commands.TryGetValue(keyword, out var command))
        {
            try
            {
                command.Execute(rawMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError("Command {commandKeyword} execution failed with Exception", command.CommandKeyword);
            }

            return;
        }

        _logger.LogWarning("Command not found for keyword: {keyword}", keyword);
    }
}