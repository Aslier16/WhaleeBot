namespace WhaleeBot.Models;

public class AppConfig
{
    public required ServerConfig Server { get; set; }
    public required ChatFilter ChatFilter { get; set; }
    public required List<long> Admins { get; set; }
    public required CompletionProvider CompletionProvider { get; set; }
    public required string PromptTemplate { get; set; }
}