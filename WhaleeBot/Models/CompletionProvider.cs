namespace WhaleeBot.Models;

public class CompletionProvider
{
    public required string BaseUrl { get; set; }
    public required string CompletionPath { get; set; }
    public required string Model { get; set; }
    public required string ApiKey { get; set; }
}