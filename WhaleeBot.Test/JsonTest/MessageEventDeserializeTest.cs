using System.Text.Json;
using WhaleeBot.Models.OneBot;

namespace WhaleeBot.Test.JsonTest;

public class MessageEventDeserializeTest
{
    private const long SelfId = 456123;
    private const long PrivateUserId = 12345678;
    private const long GroupUserId = 23456789;
    private const long GroupId = 987654321;
    private const long MessageId = 1227889053;
    private const long Time = 1792942873;

    [Fact]
    public void TestPrivateMessageEventDeserialize()
    {
        var json = """
                   {
                     "self_id": 456123,
                     "user_id": 12345678,
                     "time": 1792942873,
                     "message_id": 1227889053,
                     "message_seq": 1227889053,
                     "real_id": 1227889053,
                     "real_seq": "18",
                     "message_type": "private",
                     "sender": {
                       "user_id": 12345678,
                       "nickname": "user",
                       "card": ""
                     },
                     "raw_message": "test not included",
                     "font": 14,
                     "sub_type": "friend",
                     "message": [
                       {
                         "type": "image",
                         "data": {
                           "summary": "",
                           "file": "ABCDEFG.png",
                           "sub_type": 0,
                           "url": "https://multimedia.nt.qq.com.cn/download?someurl",
                           "file_size": "5034"
                         }
                       },
                       {
                         "type": "text",
                         "data": {
                           "text": "some text"
                         }
                       }
                     ],
                     "message_format": "array",
                     "post_type": "message",
                     "target_id": 12345678
                   }
                   """;
        var result = JsonSerializer.Deserialize<OneBotEvent>(json, SharedJsonSetting.JsonOptions);

        // 基类与事件类型
        Assert.NotNull(result);
        var me = Assert.IsType<MessageEvent>(result);
        Assert.Equal(SelfId, me.SelfId);
        Assert.Equal(Time, me.Time);

        // 事件基本信息
        Assert.Equal("private", me.MessageType);
        Assert.Equal("friend", me.SubType);
        Assert.Equal(MessageId, me.MessageId);
        Assert.Equal(PrivateUserId, me.UserId);

        // 私聊事件不应携带群信息
        Assert.Null(me.GroupId);
        Assert.Null(me.GroupName);

        // 消息段
        Assert.NotNull(me.Message);
        Assert.Equal(2, me.Message.Count);

        var image = Assert.IsType<ImageSegment>(me.Message[0]);
        Assert.Equal("", image.Summary);
        Assert.Equal("ABCDEFG.png", image.File);
        Assert.Equal(0, image.SubType);
        Assert.Equal("https://multimedia.nt.qq.com.cn/download?someurl", image.Url);
        Assert.Equal("5034", image.FileSize);

        var text = Assert.IsType<TextSegment>(me.Message[1]);
        Assert.Equal("some text", text.Text);

        Assert.DoesNotContain(me.Message, s => s is AtSegment);

        // 发送者
        Assert.NotNull(me.Sender);
        Assert.Equal(PrivateUserId, me.Sender.UserId);
        Assert.Equal("user", me.Sender.Nickname);
        Assert.Equal("", me.Sender.Card);
        Assert.Null(me.Sender.Role);
    }

    [Fact]
    public void TestGroupMessageEventDeserialize()
    {
        var json = """
                   {
                     "self_id": 456123,
                     "user_id": 23456789,
                     "time": 1792942873,
                     "message_id": 1227889053,
                     "message_seq": 1227889053,
                     "real_id": 1227889053,
                     "real_seq": "223",
                     "message_type": "group",
                     "sender": {
                       "user_id": 23456789,
                       "nickname": "group_user",
                       "card": "",
                       "role": "owner"
                     },
                     "raw_message": "[CQ:at,qq=456123] hello[CQ:image,file=GROUPIMG.png,sub_type=0,url=https://multimedia.nt.qq.com.cn/download?someurl,file_size=2237]",
                     "font": 14,
                     "sub_type": "normal",
                     "message": [
                       {
                         "type": "at",
                         "data": {
                           "qq": "456123"
                         }
                       },
                       {
                         "type": "text",
                         "data": {
                           "text": " hello"
                         }
                       },
                       {
                         "type": "image",
                         "data": {
                           "summary": "",
                           "file": "GROUPIMG.png",
                           "sub_type": 0,
                           "url": "https://multimedia.nt.qq.com.cn/download?someurl",
                           "file_size": "2237"
                         }
                       }
                     ],
                     "message_format": "array",
                     "post_type": "message",
                     "group_id": 987654321,
                     "group_name": "test group"
                   }
                   """;
        var result = JsonSerializer.Deserialize<OneBotEvent>(json, SharedJsonSetting.JsonOptions);

        // 基类与事件类型
        Assert.NotNull(result);
        var me = Assert.IsType<MessageEvent>(result);
        Assert.Equal(SelfId, me.SelfId);
        Assert.Equal(Time, me.Time);

        // 事件基本信息
        Assert.Equal("group", me.MessageType);
        Assert.Equal("normal", me.SubType);
        Assert.Equal(MessageId, me.MessageId);
        Assert.Equal(GroupUserId, me.UserId);

        // 群聊事件应携带群信息
        Assert.Equal(GroupId, me.GroupId);
        Assert.Equal("test group", me.GroupName);

        // 消息段
        Assert.NotNull(me.Message);
        Assert.Equal(3, me.Message.Count);

        var at = Assert.IsType<AtSegment>(me.Message[0]);
        Assert.Equal(SelfId.ToString(), at.QQ);

        var text = Assert.IsType<TextSegment>(me.Message[1]);
        Assert.Equal(" hello", text.Text);

        var image = Assert.IsType<ImageSegment>(me.Message[2]);
        Assert.Equal("", image.Summary);
        Assert.Equal("GROUPIMG.png", image.File);
        Assert.Equal(0, image.SubType);
        Assert.Equal("https://multimedia.nt.qq.com.cn/download?someurl", image.Url);
        Assert.Equal("2237", image.FileSize);

        // 发送者
        Assert.NotNull(me.Sender);
        Assert.Equal(GroupUserId, me.Sender.UserId);
        Assert.Equal("group_user", me.Sender.Nickname);
        Assert.Equal("", me.Sender.Card);
        Assert.Equal("owner", me.Sender.Role);
    }
}
