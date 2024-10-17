using BattleShip.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace BattleShip.Services;

public interface IGameService
{
    Guid? gameId { get; set; }
    List<Boat>? boats { get; set; }
    Grid playerGrid { get; set; }
    Grid opponentGrid { get; set; }
    Task StartGame();
    Task PlaceBoats();
    Task Attack(Position attackPosition);
}

public class GameService : IGameService
{
    public Guid? gameId { get; set; }
    public List<Boat>? boats { get; set; }
    public required Grid playerGrid { get; set; }
    public required Grid opponentGrid { get; set; }
    private Dictionary<Position, Boat> boatPositions = new Dictionary<Position, Boat>();

    private readonly IGameModalService _modalService;
    private readonly NavigationManager _navManager;
    private readonly ITokenService _tokenService;
    private readonly IHttpService _httpService;

    public GameService(IGameModalService modalService, NavigationManager navManager, ITokenService tokenService, IHttpService httpService)
    {
        _modalService = modalService;
        _navManager = navManager;
        _tokenService = tokenService;
        _httpService = httpService;
    }

    public async Task StartGame()
    {
        var startGameRequest = new StartGameRequest(10, 2);
        var response = await _httpService.SendHttpRequestAsync(HttpMethod.Post, "/startGame", startGameRequest);

        if (response.IsSuccessStatusCode)
        {
            var jsonString = await response.Content.ReadAsStringAsync();

            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                string result = doc.RootElement.GetProperty("result").GetString();
                Console.WriteLine(result);
                if (result != null)
                {
                    gameId = Guid.Parse(result);
                    playerGrid = new Grid(10, 10);
                    opponentGrid = new Grid(10, 10);
                    boats = new List<Boat>();
                }
            }
        }
        else
        {
            throw new Exception($"Error calling startGame: {response.StatusCode}");
        }
    }

    public async Task PlaceBoats()
    {
        var response = await _httpService.SendHttpRequestAsync(HttpMethod.Post, $"/placeBoats?gameId={gameId}", boats);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Les bateaux sont placés !");
        }
        else
        {
            throw new Exception($"Error calling placeBoats: {response.StatusCode}");
        }
    }

    public async Task Attack(Position attackPosition)
    {
        var json = JsonSerializer.Serialize(attackPosition, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(gameId);
        Console.WriteLine(json);
        var attackRequest = new AttackModel.AttackRequest(gameId ?? Guid.Empty, attackPosition);
        var playerAttackResponse = await _httpService.SendHttpRequestAsync(HttpMethod.Post, $"/attack?gameId={gameId}", attackRequest);
        if (playerAttackResponse.IsSuccessStatusCode)
        {
            var jsonString = await playerAttackResponse.Content.ReadAsStringAsync();
            Console.WriteLine(jsonString);
            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                var playerAttackResult = doc.RootElement.GetProperty("playerAttackResult").GetString();
                if ("Hit".Equals(playerAttackResult))
                {
                    attackPosition.IsHit = true;
                }
                Console.WriteLine(playerAttackResult);
                var computerAttackResponse = await _httpService.SendHttpRequestAsync(HttpMethod.Post, $"/attack?gameId={gameId}", new AttackModel.AttackRequest(gameId ?? Guid.Empty, null));
                if (computerAttackResponse.IsSuccessStatusCode)
                {
                    var opponentAttackResult = doc.RootElement.GetProperty("playerAttackResult").GetString();
                    if ("Hit".Equals(playerAttackResult))
                    {
                        attackPosition.IsHit = true;
                    }
                }
            }
        }
        else
        {
            throw new Exception($"Error calling startGame: {playerAttackResponse.StatusCode}");
        }
    }
}
