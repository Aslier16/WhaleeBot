using System.Text.Json.Serialization;

namespace WhaleeBot.Models;

public class ChatFilter
{
    public bool BlacklistMode { get; set; } = false;
    public List<long> Whitelist { get; set; } = new List<long>();
    public List<long> Blacklist { get; set; } = new List<long>();
}