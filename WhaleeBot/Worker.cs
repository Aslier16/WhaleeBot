using System.Net.WebSockets;
using WhaleeBot.Models;
using WhaleeBot.Services;

namespace WhaleeBot;

public class Worker(ILogger<Worker> logger, AppConfig appConfig, IMessageRouter messageRouter) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var client = new ClientWebSocket();
        client.Options.SetRequestHeader("Authorization", $"Bearer {appConfig.Server.Token}");
        try
        {
            await client.ConnectAsync(new Uri(appConfig.Server.WSAddress), stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to connect to the server at {address}", appConfig.Server.WSAddress);
            throw;
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            while (client.State == WebSocketState.Open)
            {
                await using WebSocketStream messageStream = WebSocketStream.CreateReadableMessageStream(client);
                await messageRouter.RouteMessageAsync(messageStream);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}