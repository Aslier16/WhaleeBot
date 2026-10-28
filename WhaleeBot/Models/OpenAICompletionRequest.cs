namespace WhaleeBot.Models;

public class OpenAICompletionRequest
{
    public required string Model { get; set; }
    public required List<OpenAIMessage> Messages { get; set; }
}