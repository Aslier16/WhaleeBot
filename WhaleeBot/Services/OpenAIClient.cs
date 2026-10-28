using System.Net.Http.Headers;
using System.Net.Http.Json;
using WhaleeBot.Models;

namespace WhaleeBot.Services;

public class OpenAIClient(HttpClient httpClient, AppConfig appConfig)
{
    public Task<HttpResponseMessage> RequestCompletionAsync(OpenAICompletionRequest request)
    {
        return httpClient.PostAsync(appConfig.CompletionProvider.CompletionPath, JsonContent.Create(request));
    }
}