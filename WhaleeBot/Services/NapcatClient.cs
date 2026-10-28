using System.Net.Http.Headers;
using System.Net.Http.Json;
using WhaleeBot.Models;

namespace WhaleeBot.Services;

public class NapcatClient(HttpClient httpClient)
{
    public async Task<HttpResponseMessage> PostJsonAsync(string apiPath,object payload)
    {
        var content = JsonContent.Create(payload);
        var response = await httpClient.PostAsync($"{apiPath}", content);
        return response;
    }
}