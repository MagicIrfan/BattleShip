using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace BattleShip.Services;

public interface IHttpService
{
    Task<HttpResponseMessage> SendHttpRequestAsync(HttpMethod method, string endpoint, object? content = null);
}

public class HttpService : IHttpService
{
    private readonly ITokenService _tokenService;
    private readonly HttpClient _httpClient;
    private static readonly string BASE_API_URL = "https://localhost:5134/api/game";

    public HttpService(ITokenService tokenService, HttpClient httpClient)
    {
        _tokenService = tokenService;
        _httpClient = httpClient;
    }

    public async Task<HttpResponseMessage> SendHttpRequestAsync(HttpMethod method, string endpoint, object? content = null)
    {
        var token = await _tokenService.GetAccessTokenAsync();

        var request = new HttpRequestMessage(method, $"{BASE_API_URL}{endpoint}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (content != null)
        {
            var jsonContent = JsonSerializer.Serialize(content);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }

        try
        {
            return await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            throw;
        }
    }
}
