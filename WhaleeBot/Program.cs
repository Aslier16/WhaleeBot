using System.Net.Http.Headers;
using System.Text.Json;
using DeepseekWebChatWrapper.Core;
using WhaleeBot;
using WhaleeBot.Commands;
using WhaleeBot.Models;
using WhaleeBot.Services;
using Microsoft.Extensions.DependencyInjection;
using WhaleeBot.ResponseHandlers;

var builder = Host.CreateApplicationBuilder(args);

// Load Config.json
var appconfig = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText("./Config.json"), new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                }) ??
                throw new Exception("Failed to load Config.json");
builder.Services.AddSingleton(appconfig);

// Add HttpClient for NapcatClient and OpenAIClient
builder.Services.AddHttpClient<NapcatClient>(client =>
{
    client.BaseAddress = new Uri(appconfig.Server.HttpAddress ?? throw new Exception("Server HttpAddress is not configured"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", appconfig.Server.Token);
});
builder.Services.AddHttpClient<OpenAIClient>(client =>
{
    client.BaseAddress = new Uri(appconfig.CompletionProvider.BaseUrl);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", appconfig.CompletionProvider.ApiKey);
});
builder.Services.AddSingleton<DsWebChatClient>(new DsWebChatClient(appconfig.CompletionProvider.AuthToken, appconfig.CompletionProvider.DsSessionId));

builder.Services.AddSingleton<IMessageCommand, OpCommand>();
builder.Services.AddSingleton<ICommandRouter, CommandRouter>();

// Register response handlers
builder.Services.AddSingleton<IResponseHandler, SendPrivateMessageHandler>();
builder.Services.AddSingleton<IResponseHandler, SendGroupMessageHandler>();

builder.Services.AddSingleton<IChatRouter, ChatRouter>();
builder.Services.AddSingleton<IMessageRouter, MessageRouter>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

File.WriteAllText("./Config.json", JsonSerializer.Serialize(appconfig, new JsonSerializerOptions()
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
}));