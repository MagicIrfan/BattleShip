using BattleShip.Components;
using BattleShip.Models;
using BattleShip.Models.Request;
using BattleShip.Models.Response;
using BattleShip.Pages;
using System.Text.Json;

namespace BattleShip.Services.Game;

public interface IGameApiService
{
    Task<Guid?> StartGameAsync(int gridSize, int difficulty);
    Task PlaceBoatsAsync(List<Boat> boats, Guid? gameId);
    Task<AttackModel.AttackResponse> AttackAsync(Guid? gameId, Position attackPosition);
    Task<RollbackResponse?> RollbackAsync(Guid? gameId);
}

public class GameApiService : IGameApiService
{
    private readonly IHttpService _httpService;

    public GameApiService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<Guid?> StartGameAsync(int gridSize, int difficulty)
    {
        var startGameRequest = new StartGameRequest(gridSize, difficulty);
        var response = await SendRequest(HttpMethod.Post, "/game/startGame", startGameRequest);
        using (JsonDocument doc = JsonDocument.Parse(response))
        {
            string result = doc.RootElement.GetProperty("result").GetString();
            if (result != null)
            {
                return Guid.Parse(result);
            }
        }
        return null;
    }

    public async Task PlaceBoatsAsync(List<Boat> boats, Guid? gameId)
    {
        await SendRequest(HttpMethod.Post, $"/game/placeBoats?gameId={gameId}", boats);
    }

    public async Task<AttackModel.AttackResponse> AttackAsync(Guid? gameId, Position attackPosition)
    {
        var attackRequest = new AttackModel.AttackRequest(gameId ?? Guid.Empty, attackPosition);
        var jsonString = await SendRequest(HttpMethod.Post, $"/game/attack?gameId={gameId}", attackRequest);
        return JsonSerializer.Deserialize<AttackModel.AttackResponse>(jsonString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<RollbackResponse?> RollbackAsync(Guid? gameId)
    {
        var content = await SendRequest(HttpMethod.Post, $"/game/rollback?gameId={gameId}");
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

