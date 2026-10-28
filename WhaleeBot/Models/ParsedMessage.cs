using WhaleeBot.Models.OneBot;

namespace WhaleeBot.Models;

public class ParsedMessage
{
    public long SenderId { get; set; }
    public string SenderNickname { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public List<ImageSegment>? Images { get; set; } = null;
}