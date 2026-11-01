namespace WhaleeBot.Models.OneBot;

public class ImageSegment : MessageSegment
{
    public required string Summary { get; set; }
    public string? File { get; set; }
    public long SubType { get; set; }
    public required string Url { get; set; }
    public string? FileSize { get; set; }
}