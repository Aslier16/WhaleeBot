using System.Text;
using System.Text.Json;
using DeepseekWebChatWrapper.Core;
using WhaleeBot.Models;
using WhaleeBot.Models.NapcatRequests;
using WhaleeBot.Models.OneBot;
using WhaleeBot.ResponseHandlers;

namespace WhaleeBot.Services;

/// <summary>
/// general router for messages
/// manage chat strategy and history
/// </summary>
public class ChatRouter : IChatRouter
{
    private JsonSerializerOptions _serializerOptions = new()
    {
        AllowOutOfOrderMetadataProperties = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly ILogger<ChatRouter> _logger;
    private readonly ICommandRouter _commandRouter;
    private readonly AppConfig _appConfig;
    private readonly NapcatClient _napcatClient;
    private readonly OpenAIClient _openAiClient;
    private readonly DsWebChatClient _dsWebChatClient;

    /// <summary>
    /// general router for messages
    /// manage chat strategy and history
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="commandRouter"></param>
    /// <param name="appConfig"></param>
    /// <param name="napcatClient"></param>
    /// <param name="openAiClient"></param>
    /// <param name="dsWebChatClient"></param>
    /// <param name="responseHandlers"></param>
    public ChatRouter(ILogger<ChatRouter> logger,
        ICommandRouter commandRouter,
        AppConfig appConfig,
        NapcatClient napcatClient,
        OpenAIClient openAiClient,
        DsWebChatClient dsWebChatClient,
        IEnumerable<IResponseHandler> responseHandlers)
    {
        _logger = logger;
        _commandRouter = commandRouter;
        _appConfig = appConfig;
        _napcatClient = napcatClient;
        _openAiClient = openAiClient;
        _dsWebChatClient = dsWebChatClient;
        PromptTemplate = appConfig.PromptTemplate;
        var promptBuiler = new StringBuilder();
        ResponseHandlersDict = new Dictionary<Type, IResponseHandler>();
        foreach (var responseHandler in responseHandlers)
        {
            promptBuiler.Append(responseHandler.Description);
            promptBuiler.Append(responseHandler.Format);
            ResponseHandlersDict[responseHandler.HandledRequestType] = responseHandler;
        }

        RequestFormatPrompt = promptBuiler.ToString();
    }

    protected long LastSendTime { get; set; } = -1;
    protected long LastReceiveTime { get; set; } = -1;
    protected Dictionary<long, List<ParsedMessage>> MessageHistory { get; set; } = new();
    protected string PromptTemplate { get; set; }

    protected string FrontFormatPrompt { get; set; } = """
                                                        You must only output the result in a list of JSON objects, strictly guarantee response start with '[' and end with ']' and don't place it in markdown code block or the parse will fail, each object will behave sequentially, empty list means do nothing. You are in a chat app has text wrap no md format support, so don't output too long line or with md syntax.
                                                        """;

    protected const string EndingFormatPrompt = """
                                                OUTPUT CONTRACT
                                                - Your entire reply is ONE JSON array. First character '[', last character ']'. Nothing before, nothing after — no greeting, no explanation, no markdown fences.
                                                - Each array element is ONE action(send message or sth. else), top to bottom. Empty array [] means stay silent.
                                                - Use group or user id that exists in the chat, don't make up random numbers.
                                                
                                                JSON LEGALITY — hard rules, any violation = failure
                                                1. Double quotes only. Escape inner quotes as \". No single quotes, no unquoted keys.
                                                2. NO literal newline, carriage return, or tab inside any string. Use \n only if truly needed — but see CHAT RULES: you almost never need it.
                                                3. No trailing commas, no comments, no NaN/Infinity, no leading zeros, no + sign.
                                                4. No markdown inside message: no **bold**, no _italic_, no backticks, no ``` fences, no # headings, no bullet lists, no numbered lists, no tables, no links with md syntax. The app renders plain text only.
                                                5. You may put \n inside a message to manually break lines the way a human does when a bubble runs long or wants visual blocks newlines between elements are fine right after the comma never inside an object never after [ never before ]
                                                7. If you have nothing to say, output exactly: []

                                                CHAT RULES — sound like a human, not a document
                                                1. Split your reply into multiple message objects. Default 1-3 bubbles; more only if the thought really needs it. Never dump one giant paragraph.
                                                2. Keep each bubble short — aim under ~60 characters, hard ceiling ~120. One idea per bubble.
                                                3. Want separate sends use a new object, want a visual break inside one bubble use \n
                                                4. Write like texting: contractions, casual wording, sentence fragments, the occasional emoji, no formal transitions like "Furthermore" or "In conclusion".
                                                5. No lists, no headings, no step-by-step enumeration. If you must convey several points, spread them across bubbles.
                                                6. Don't over-explain, don't lecture, don't repeat the question back. Reply, then stop.
                                                7. Match the user's language and tone.

                                                VALID EXAMPLE
                                                [{"$type":"send_group_msg","group_id":12345678,"message":"some message"},{"$type":"send_group_msg","group_id":12345678,"message":"second message"},{"$type":"send_group_msg","group_id":12345678,"message":"ending message"}]

                                                VALID EXAMPLE — multi-line layout, still one object per line
                                                [{"$type":"send_private_msg","user_id":654321,"message":"some message"},
                                                {"$type":"send_private_msg","user_id":654321,"message":"another message"}]

                                                INVALID — NEVER DO THIS
                                                ```json
                                                [{"type(should be $type)":"xxx(should be valid type)","user_id":7777, "message": "第一行
                                                第二行"}, {"type":"send_private_msg(should be send_group_msg)","group_id":8888,"content (should be message)":"**加粗** 和 - 列表项"},]
                                                """;
    
    protected string RequestFormatPrompt { get; set; }

    protected Dictionary<Type, IResponseHandler> ResponseHandlersDict { get; set; }

    public async Task RouteMessageAsync(MessageEvent messageEvent)
    {
        // check if the user is in the whitelist or blacklist
        if (_appConfig.ChatFilter.BlacklistMode)
        {
            if (_appConfig.ChatFilter.Blacklist.Contains(messageEvent.UserId))
            {
                _logger.LogInformation("Message from user {userId} is blacklisted. Skipped", messageEvent.UserId);
                return;
            }

            if (_appConfig.ChatFilter.Blacklist.Contains(messageEvent.GroupId ?? -1))
            {
                _logger.LogInformation("Message from group {groupId} is blacklisted. Skipped", messageEvent.GroupId);
                return;
            }
        }
        else
        {
            if (!_appConfig.ChatFilter.Whitelist.Contains(messageEvent.UserId))
            {
                _logger.LogInformation("Message from user {userId} is not whitelisted. Skipped", messageEvent.UserId);
                return;
            }

            if (!_appConfig.ChatFilter.Whitelist.Contains(messageEvent.GroupId ?? -1))
            {
                _logger.LogInformation("Message from group {groupId} is not whitelisted. Skipped",
                    messageEvent.GroupId);
                return;
            }
        }

        LastReceiveTime = messageEvent.Time;

        // Concat text segments and collect image segments
        var textBuilder = new StringBuilder();
        List<ImageSegment> imageSegments = new();
        foreach (var segment in messageEvent.Message)
        {
            switch (segment)
            {
                case TextSegment textSegment: textBuilder.Append(textSegment.Text); break;
                case ImageSegment imageSegment: imageSegments.Add(imageSegment); break;
                case AtSegment atSegment: textBuilder.Append($"@{atSegment.QQ} "); break;
                default:
                    _logger.LogError("Unknown message segment type {segmentType}. Skipped", segment.GetType()); break;
            }
        }

        var parsed = new ParsedMessage()
        {
            SenderId = messageEvent.Sender.UserId,
            SenderNickname = messageEvent.Sender.Nickname,
            Text = textBuilder.ToString(),
            Images = imageSegments.Count > 0 ? imageSegments : null
        };

        if (parsed.Text.StartsWith('/'))
        {
            _logger.LogInformation("Routing command: {command}", parsed.Text);
            _commandRouter.RouteMessage(parsed);
            return;
        }

        long chatId = messageEvent.MessageType switch
        {
            "private" => messageEvent.UserId,
            "group" => messageEvent.GroupId ?? -1,
            _ => -1
        };
        if (chatId == -1)
        {
            _logger.LogError("Unknown message type {messageType} or missing group id. Skipped",
                messageEvent.MessageType);
            return;
        }

        if (!MessageHistory.ContainsKey(chatId))
        {
            MessageHistory[chatId] = [];
        }

        MessageHistory[chatId].Add(parsed);

        var sysPrompt = new OpenAIMessage()
        {
            Role = "system",
            Content =
                $"{FrontFormatPrompt}\nYou are chatting" +
                (messageEvent.GroupId is not null
                    ? $"in Group {messageEvent.GroupName}({messageEvent.GroupId})."
                    : $"You are chatting with user {messageEvent.Sender.Nickname}({messageEvent.UserId}).")
        };
        var prompt = new List<OpenAIMessage>() { sysPrompt };
        foreach (var parsedMessage in MessageHistory[chatId])
        {
            prompt.Add(new OpenAIMessage()
            {
                Role = parsedMessage.SenderId == messageEvent.UserId ? "user" : "assistant",
                Content = $"{parsedMessage.SenderNickname}({parsedMessage.SenderId}):{parsedMessage.Text}"
            });
        }
        var endingPrompt = new OpenAIMessage()
        {
            Role = "system",
            Content = $"You can construct following objects:{RequestFormatPrompt}\n{EndingFormatPrompt}"
        };
        prompt.Add(endingPrompt);

        // Decide whether to use webchat
        string responseContent;
        if (_appConfig.CompletionProvider.EnableDeepseekWebChat &&
            !string.IsNullOrEmpty(_appConfig.CompletionProvider.AuthToken) &&
            !string.IsNullOrEmpty(_appConfig.CompletionProvider.DsSessionId))
        {
            var promptBuiler = new StringBuilder();
            foreach (var seg in prompt)
            {
                promptBuiler.AppendLine(seg.ToString());
            }

            responseContent = (await _dsWebChatClient.RequestCompletionAsync(promptBuiler.ToString())).Response;
        }
        else
        {
            var request = new OpenAICompletionRequest()
            {
                Model = _appConfig.CompletionProvider.Model,
                Messages = prompt,
                // TODO: Add image segments to the request if any
            };
            _logger.LogDebug("Sending OpenAI request for chat {chatId} with messages: {messages}", chatId,
                JsonSerializer.Serialize(request.Messages));

            var response = await _openAiClient.RequestCompletionAsync(request);
            try
            {
                response.EnsureSuccessStatusCode();
                responseContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "OpenAI request failed for); chat {chatId}. Response: {response}", chatId,
                    response);
                return;
            }

            OpenAICompletionResponse? completionResponse;
            try
            {
                completionResponse =
                    JsonSerializer.Deserialize<OpenAICompletionResponse>(responseContent, _serializerOptions);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx,
                    "Failed to deserialize OpenAI response for chat {chatId}. Raw Response:\n{response}",
                    chatId, responseContent);
                return;
            }

            if (completionResponse is null || completionResponse.Choices.Count == 0)
            {
                _logger.LogError("OpenAI response is null or empty for chat {chatId}. Raw Response:\n{response}",
                    chatId,
                    responseContent);
                return;
            }

            responseContent = completionResponse.Choices[0].Message.Content;
        }

        if (string.IsNullOrEmpty(responseContent))
        {
            _logger.LogError("OpenAI response content is null or empty for chat {chatId}.", chatId);
            return;
        }

        await ParseResponseAndExec(responseContent);

        async Task<bool> ParseResponseAndExec(string completionResponse)
        {
            try
            {
                var components =
                    JsonSerializer.Deserialize<List<NapcatRequestBase>>(completionResponse,
                        _serializerOptions);
                _logger.LogInformation("Received OpenAI response for chat {chatId}: {response}", chatId,
                    completionResponse);
                if (components is null)
                {
                    _logger.LogError("Failed to deserialize OpenAI response for chat {chatId}. Raw Reply:\n{response}",
                        chatId,
                        completionResponse);
                    return true;
                }

                foreach (var component in components)
                {
                    if (ResponseHandlersDict.TryGetValue(component.GetType(), out var handler))
                    {
                        await handler.ProcessAsync(this, component, CancellationToken.None);
                    }
                    else
                    {
                        _logger.LogWarning("No response handler found for request type {requestType}. Skipped",
                            component.GetType());
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to deserialize OpenAI response for chat {chatId}. Response: {response}",
                    chatId,
                    completionResponse);
            }

            return false;
        }
    }

    public async Task PostNapcatRequestAsync(string address, object body)
    {
        var response = await _napcatClient.PostJsonAsync(address, body);
        try
        {
            response.EnsureSuccessStatusCode();
            _logger.LogInformation(
                "Successfully posted napcat request to {address} with body {body}. Response: {response}",
                address, body, await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to post napcat request to {address} with body {body}. Response: {response}",
                address, body, await response.Content.ReadAsStringAsync());
        }
    }
}