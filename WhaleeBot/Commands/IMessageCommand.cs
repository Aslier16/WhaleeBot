using WhaleeBot.Models;

namespace WhaleeBot.Commands;

public interface IMessageCommand
{
    /// <summary>
    /// The keyword that triggers this command.
    /// start with '/'
    /// </summary>
    string CommandKeyword { get; }
    /// <summary>
    /// The format of the command, including any parameters.
    /// </summary>
    string CommandFormat { get; }
    /// <summary>
    /// Executes the command with the given message.
    /// </summary>
    void Execute(ParsedMessage message);
}