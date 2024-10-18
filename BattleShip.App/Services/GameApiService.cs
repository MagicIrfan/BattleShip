using BattleShip.Components;
using BattleShip.Models;
using System.Text.Json;

namespace BattleShip.Services;

public interface IGameApiService
{
    Task<Guid?> StartGameAsync();
    Task PlaceBoatsAsync(List<Boat> boats, Guid? gameId);
    Task<AttackResponse> AttackAsync(Guid? gameId, Position attackPosition);
    Task<RollbackResponse?> RollbackAsync(Guid? gameId);
}

public class GameApiService : IGameApiService
{
    private readonly IHttpService _httpService;

    public GameApiService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<Guid?> StartGameAsync()
    {
        var startGameRequest = new StartGameRequest(10, 2);
        var response = await SendRequest(HttpMethod.Post, "/startGame", startGameRequest);
        return response != null ? Guid.Parse(response) : null;
    }

    public async Task PlaceBoatsAsync(List<Boat> boats, Guid? gameId)
    {
        await SendRequest(HttpMethod.Post, $"/placeBoats?gameId={gameId}", boats);
    }

    public async Task<AttackResponse> AttackAsync(Guid? gameId, Position attackPosition)
    {
        var attackRequest = new AttackModel.AttackRequest(gameId ?? Guid.Empty, attackPosition);
        var jsonString = await SendRequest(HttpMethod.Post, $"/attack?gameId={gameId}", attackRequest);
        return JsonSerializer.Deserialize<AttackResponse>(jsonString);
    }

    public async Task<RollbackResponse?> RollbackAsync(Guid? gameId)
    {
        var content = await SendRequest(HttpMethod.Post, $"/rollback?gameId={gameId}");
        var rollback = JsonSerializer.Deserialize<RollbackResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return rollback;
    }

    private async Task<string> SendRequest(HttpMethod method, string url, object? content = null)
    {
        var response = await _httpService.SendHttpRequestAsync(method, url, content);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Error calling {url}: {response.StatusCode}");
        }
        return await response.Content.ReadAsStringAsync();
    }
}

