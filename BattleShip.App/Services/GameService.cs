using BattleShip.Exceptions;
using BattleShip.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace BattleShip.Services;

public interface IGameService
{
    Guid? gameId { get; set; }
    List<Boat> boats { get; set; }
    Grid playerGrid { get; set; }
    Grid opponentGrid { get; set; }
    List<string> historique { get; set; }
    Task StartGame();
    Task PlaceBoats();
    Task Attack(Position attackPosition);
    void PlaceBoat(List<Position> positions);
    bool IsBoatAtPosition(Position position);
}

public class GameService : IGameService
{
    public Guid? gameId { get; set; }
    public required List<Boat> boats { get; set; } = new List<Boat>();
    public required Grid playerGrid { get; set; }
    public required Grid opponentGrid { get; set; }
    public required List<string> historique { get; set; } = new List<string>();
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
        var attackRequest = new AttackModel.AttackRequest(gameId ?? Guid.Empty, attackPosition);
        var playerAttackResponse = await _httpService.SendHttpRequestAsync(HttpMethod.Post, $"/attack?gameId={gameId}", attackRequest);

        if (playerAttackResponse.IsSuccessStatusCode)
        {
            var jsonString = await playerAttackResponse.Content.ReadAsStringAsync();

            var attackResponse = JsonSerializer.Deserialize<AttackResponse>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (attackResponse == null)
            {
                throw new Exception("Invalid response from the server.");
            }

            Console.WriteLine(jsonString);

            UpdateGrid(attackResponse.PlayerAttackPosition, attackResponse.PlayerIsHit, opponentGrid);

            if (attackResponse.PlayerIsWinner)
            {
                Console.WriteLine("Le joueur a gagné !");
            }

			historique.Add($"Le joueur 1 a attaqué la position {attackResponse.PlayerAttackPosition.X}, {attackResponse.PlayerAttackPosition.Y}");
			if (attackResponse.PlayerIsHit)
			{
				historique.Add($"Le joueur 1 a touché un bateau");
			}
			else
			{
				historique.Add($"Le joueur 1 s'est raté");
			}

			if (attackResponse.PlayerIsSunk)
            {
                historique.Add("Le joueur 1 a coulé un bateau !");
            }

            UpdateGrid(attackResponse.AiAttackPosition, attackResponse.AiIsHit, playerGrid);

			historique.Add($"L'ordinateur a attaqué la position {attackResponse.AiAttackPosition.X}, {attackResponse.AiAttackPosition.Y}");
            if (attackResponse.AiIsHit)
            {
				historique.Add($"L'ordinateur a touché un bateau !");
			}
            else
            {
				historique.Add($"L'ordinateur s'est raté");
			}

			if (attackResponse.AiIsSunk)
			{
				historique.Add("L'ordinateur a coulé un bateau !");
			}

			if (attackResponse.AiIsWinner)
            {
                Console.WriteLine("L'ordinateur a gagné !");
            }
        }

		else
        {
            throw new AttackException($"Error calling attack: {playerAttackResponse.StatusCode}", playerAttackResponse.StatusCode);
        }
    }

    private void UpdateGrid(Position position, bool isHit, Grid grid)
    {
        position.IsHit = isHit;
        var state = isHit ? PositionState.HIT : PositionState.MISS;
        grid.PositionsData[position.X][position.Y].Position = position;
        grid.PositionsData[position.X][position.Y].State = state;
    }



    public void PlaceBoat(List<Position> positions)
    {
        boats.Add(new Boat(positions));
    }

    public bool IsBoatAtPosition(Position position)
    {
        return boats.Any(boat => boat.Positions.Any(p => p.X == position.X && p.Y == position.Y));
    }
}
