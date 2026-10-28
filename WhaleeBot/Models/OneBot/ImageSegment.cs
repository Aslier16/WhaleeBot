namespace WhaleeBot.Models.OneBot;

public class ImageSegment : MessageSegment
{
    public required string Summary { get; set; }
    public required string File { get; set; }
    public required long SubType { get; set; }
    public required string Url { get; set; }
    public required string FileSize { get; set; }
}