namespace WhaleeBot.Models;

public class OpenAIMessage
{
    public required string Role { get; set; }
    public required string Content { get; set; }

    public override string ToString()
    {
        return $"{Role}: {Content}";
    }
}