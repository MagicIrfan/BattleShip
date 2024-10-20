using BattleShip.Components;
using BattleShip.Exceptions;
using BattleShip.Models;
using BattleShip.Services.Game;
using Blazored.Modal.Services;
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
    bool IsPlacingBoat { get; set; }
    GameParameter gameParameter { get; set; }
    Task StartGame();
    Task PlaceBoats();
    Task Attack(Position attackPosition);
    void PlaceBoat(List<Position> positions);
    bool IsBoatAtPosition(Position position);
    Task<Dictionary<string, int>> GetLeaderboard();
    Task Rollback();
}

public class GameService : IGameService
{
    public Guid? gameId { get; set; }
    public required List<Boat> boats { get; set; } = new List<Boat>();
    public required Grid playerGrid { get; set; }
    public required Grid opponentGrid { get; set; }
    public required List<string> historique { get; set; } = new List<string>();
    public required bool IsPlacingBoat { get; set; } = true;
    public required GameParameter gameParameter { get; set; } = new GameParameter();

    private readonly IGameModalService _modalService;
    private readonly NavigationManager _navManager;
    private readonly ITokenService _tokenService;
    private readonly IHttpService _httpService;
    private readonly IGameEventService _eventService;

    public GameService(IGameModalService modalService, NavigationManager navManager, ITokenService tokenService, IHttpService httpService, IGameEventService eventService)
    {
        _modalService = modalService;
        _navManager = navManager;
        _tokenService = tokenService;
        _httpService = httpService;
        _eventService = eventService;
    }

    public async Task StartGame()
    {
        var startGameRequest = new StartGameRequest(gameParameter.GridSize, gameParameter.DifficultyLevel);
        var response = await _httpService.SendHttpRequestAsync(HttpMethod.Post, "/game/startGame", startGameRequest);

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
                    playerGrid = new Grid(gameParameter.GridSize, gameParameter.GridSize);
                    opponentGrid = new Grid(gameParameter.GridSize, gameParameter.GridSize);
                    boats = new List<Boat>();
                    historique = new List<string>();
                    IsPlacingBoat = true;
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
        var response = await _httpService.SendHttpRequestAsync(HttpMethod.Post, $"/game/placeBoats?gameId={gameId}", boats);
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
        /*var json = JsonSerializer.Serialize(attackPosition, new JsonSerializerOptions { WriteIndented = true });
        var attackRequest = new AttackModel.AttackRequest(gameId ?? Guid.Empty, attackPosition);
        var playerAttackResponse = await _httpService.SendHttpRequestAsync(HttpMethod.Post, $"/game/attack?gameId={gameId}", attackRequest);

        if (playerAttackResponse.IsSuccessStatusCode)
        {
            var jsonString = await playerAttackResponse.Content.ReadAsStringAsync();

            var attackResponse = JsonSerializer.Deserialize<AttackModel.AttackResponse>(jsonString, new JsonSerializerOptions
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
                var result = await _modalService.ShowModal<GameModal>("Gagné", "Vous avez gagné la partie");
                if (result == "restart")
                {
                    _eventService.RaiseGameRestarted();
                    await StartGame();
                }
                else if (result == "return")
                {
                    _navManager.NavigateTo("/");
                }
            }

            historique.Add($"Le joueur 1 a attaqué la position ({attackResponse.PlayerAttackPosition.X}, {attackResponse.PlayerAttackPosition.Y}) - {(attackResponse.PlayerIsHit ? "Touché" : "Raté")}");

			if (attackResponse.PlayerIsSunk)
            {
                historique.Add("Le joueur 1 a coulé un bateau !");
            }

            UpdateGrid(attackResponse.AiAttackPosition, attackResponse.AiIsHit, playerGrid);

            if (attackResponse.AiIsWinner)
            {
                Console.WriteLine("L'ordinateur a gagné !");
                var result = await _modalService.ShowModal<GameModal>("Perdu", "Vous avez perdu la partie");
                if (result == "restart")
                {
                    _eventService.RaiseGameRestarted();
                    await StartGame();

                }
                else if (result == "return")
                {
                    _navManager.NavigateTo("/");
                }
            }

            historique.Add($"L'ordinateur a attaqué la position ({attackResponse.AiAttackPosition.X}, {attackResponse.AiAttackPosition.Y}) - {(attackResponse.AiIsHit ? "Touché" : "Raté")}");

			if (attackResponse.AiIsSunk)
			{
				historique.Add("L'ordinateur a coulé un bateau !");
			}
        }
        else
        {
            throw new AttackException($"Error calling attack: {playerAttackResponse.StatusCode}", playerAttackResponse.StatusCode);
        }*/
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

    public async Task<Dictionary<string, int>> GetLeaderboard()
    {
        // Envoyer la requête HTTP pour récupérer le leaderboard
        var leaderboardResponse = await _httpService.SendHttpRequestAsync(HttpMethod.Get, "/game/leaderboard");

        // Lire le contenu de la réponse
        var leaderboardContent = await leaderboardResponse.Content.ReadAsStringAsync();

        // Désérialiser le contenu JSON en dictionnaire
        var leaderboard = JsonSerializer.Deserialize<Dictionary<string, int>>(leaderboardContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true // Permet d'ignorer la casse des noms de propriétés
        });

        return leaderboard;
    }

    public async Task Rollback()
    {
        var rollbackResponse = await _httpService.SendHttpRequestAsync(HttpMethod.Post, $"/game/rollback?gameId={gameId}");
        var rollbackContent = await rollbackResponse.Content.ReadAsStringAsync();
        var rollback = JsonSerializer.Deserialize<RollbackResponse>(rollbackContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var computerPosition = rollback.LastPlayerAttackPosition;
        var playerPosition = rollback.LastIaAttackPosition;
        playerGrid.PositionsData[playerPosition.X][playerPosition.Y].Position = playerPosition;
        playerGrid.PositionsData[playerPosition.X][playerPosition.Y].State = null;

        opponentGrid.PositionsData[computerPosition.X][computerPosition.Y].Position = computerPosition;
        opponentGrid.PositionsData[computerPosition.X][computerPosition.Y].State = null;
    }
}
