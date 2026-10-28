using WhaleeBot.Services;
using WhaleeBot.Models;

namespace WhaleeBot.Commands;

public class OpCommand(ILogger<OpCommand> logger, AppConfig config)  : IMessageCommand
{
    public string CommandKeyword => "/op";
    public string CommandFormat => "/op <userid>";

    public void Execute(ParsedMessage message)
    {
        if (!config.Admins.Contains(message.SenderId))
        {
            logger.LogWarning("User {userId} attempted to execute /op command without permission", message.SenderId);
            return;
        }
        var targetUserId = Convert.ToInt64(message.Text.Split(' ')[1]);
        if (config.Admins.Contains(targetUserId))
        {
            logger.LogWarning("User {userId} is already an admin", targetUserId);
            return;
        }
        config.Admins.Add(targetUserId);
        logger.LogInformation("User {userId} has been granted admin privileges by {senderId}", targetUserId, message.SenderId);
    }
}