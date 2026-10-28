namespace WhaleeBot.Models;

public class OpenAICompletionResponse
{
    public required string Id { get; set; }
    public required string Object { get; set; }
    public required int Created { get; set; }
    public required string Model { get; set; }
    public required List<OpenAIChoice> Choices { get; set; }
    public required OpenAIUsage Usage { get; set; }
}

public class OpenAIChoice
{
    public required int Index { get; set; }
    public required OpenAIMessage Message { get; set; }
    public required string FinishReason { get; set; }
}

public class OpenAIUsage
{
    public required int PromptTokens { get; set; }
    public required int CompletionTokens { get; set; }
    public required int TotalTokens { get; set; }
}