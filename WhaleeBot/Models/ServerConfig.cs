using System.Text.Json.Serialization;

namespace WhaleeBot.Models;

public class ServerConfig
{
    [JsonPropertyName("wsAddress")]
    public required string WSAddress { get; set; }
    public string? HttpAddress { get; set; }
    public required string Token { get; set; }
}