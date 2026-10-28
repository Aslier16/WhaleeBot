using System.Text.Json;
using WhaleeBot.Models.OneBot;
using WhaleeBot.Utils;

namespace WhaleeBot.Test.JsonTest;

public class MessageSegmentConverterTest
{
    [Fact]
    public void TestReadTextSegment()
    {
        var json = """
                   {
                     "type": "text",
                     "data": {
                       "text": "some text"
                     }
                   }
                   """;
        var result = JsonSerializer.Deserialize<MessageSegment>(json, SharedJsonSetting.JsonOptions);
        Assert.NotNull(result);
        Assert.IsType<TextSegment>(result);
        Assert.Equal("some text", ((TextSegment)result).Text);
    }
    
    [Fact]
    public void TestReadImageSegment()
    {
        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            Converters = { new MessageSegmentConverter() }
        };
        var json = """
                   {
                     "type": "image",
                     "data": {
                       "summary": "",
                       "file": "ABCDEFG.png",
                       "sub_type": 0,
                       "url": "https://multimedia.nt.qq.com.cn/download?someurl",
                       "file_size": "5034"
                     }
                   }
                   """;
        var result = JsonSerializer.Deserialize<MessageSegment>(json, options);
        Assert.NotNull(result);
        Assert.IsType<ImageSegment>(result);
        Assert.Equal("ABCDEFG.png", ((ImageSegment)result).File);
        Assert.Equal(0, ((ImageSegment)result).SubType);
        Assert.Equal("https://multimedia.nt.qq.com.cn/download?someurl", ((ImageSegment)result).Url);
        Assert.Equal("5034", ((ImageSegment)result).FileSize);
    }
    
    [Fact]
    public void TestReadAtSegment()
    {
        var json = """
                   {
                     "type": "at",
                     "data": {
                       "qq": "12345678"
                     }
                   }
                   """;
        var result = JsonSerializer.Deserialize<MessageSegment>(json, SharedJsonSetting.JsonOptions);
        Assert.NotNull(result);
        Assert.IsType<AtSegment>(result);
        Assert.Equal("12345678", ((AtSegment)result).QQ);
    }
}