namespace WhaleeBot.Models;

public class CompletionProvider
{
    public bool EnableDeepseekWebChat { get; set; } = false;
    public string AuthToken { get; set; } = string.Empty;
    public string DsSessionId { get; set; } = string.Empty;
    public required string BaseUrl { get; set; }
    public required string CompletionPath { get; set; }
    public required string Model { get; set; }
    public required string ApiKey { get; set; }
}