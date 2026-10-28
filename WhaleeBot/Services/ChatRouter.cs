using System.Text;
using System.Text.Json;
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

    /// <summary>
    /// general router for messages
    /// manage chat strategy and history
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="commandRouter"></param>
    /// <param name="appConfig"></param>
    /// <param name="napcatClient"></param>
    /// <param name="openAiClient"></param>
    public ChatRouter(ILogger<ChatRouter> logger,
        ICommandRouter commandRouter,
        AppConfig appConfig,
        NapcatClient napcatClient,
        OpenAIClient openAiClient,
        IEnumerable<IResponseHandler> responseHandlers)
    {
        _logger = logger;
        _commandRouter = commandRouter;
        _appConfig = appConfig;
        _napcatClient = napcatClient;
        _openAiClient = openAiClient;
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

    protected string OutputFormatPrompt { get; set; } = """
                                                        You should only output the result in a list of JSON objects, each object will behave sequentially, empty list means do nothing. You can use the following format:
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
        }
        else
        {
            if (!_appConfig.ChatFilter.Whitelist.Contains(messageEvent.UserId))
            {
                _logger.LogInformation("Message from user {userId} is not whitelisted. Skipped", messageEvent.UserId);
                return;
            }
        }

        LastReceiveTime = messageEvent.Time;
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
            Content = $"{PromptTemplate}\n{OutputFormatPrompt}\n{RequestFormatPrompt}"
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
            var responseContent = await response.Content.ReadAsStringAsync();

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

            try
            {
                var components =
                    JsonSerializer.Deserialize<List<NapcatRequestBase>>(completionResponse.Choices[0].Message.Content,
                        _serializerOptions);
                _logger.LogInformation("Received OpenAI response for chat {chatId}: {response}", chatId,
                    completionResponse.Choices[0].Message.Content);
                if (components is null)
                {
                    _logger.LogError("Failed to deserialize OpenAI response for chat {chatId}. Raw Reply:\n{response}",
                        chatId,
                        completionResponse.Choices[0].Message.Content);
                    return;
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
                _logger.LogError(e, "Failed to deserialize OpenAI response for chat {chatId}. Response: {response}", chatId,
                    completionResponse.Choices[0].Message.Content);
                throw;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to process OpenAI response for chat {chatId}. Response: {response}", chatId,
                response);
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