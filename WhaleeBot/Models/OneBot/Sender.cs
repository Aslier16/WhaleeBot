namespace WhaleeBot.Models.OneBot;

public class Sender
{
    public required long UserId { get; set; }
    public required string Nickname { get; set; }
    public required string Card { get; set; }
    
    // group only
    public string? Role { get; set; }
}