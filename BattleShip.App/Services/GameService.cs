using BattleShip.Grpc;
using BattleShip.Models;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;

namespace BattleShip.Services;

public interface IGameService
{

    Guid? gameId { get; set; }
    List<Boat>? boats { get; set; }
    Grid playerGrid { get; set; }
    Grid oponnentGrid { get; set; }
    Task StartGame();
    Task Attack(Position position);
    Task HandleGameResult(AttackResponse responseData);
    void InitializeBoatPositions();
    bool IsBoatAtPosition(Position position);
    Boat? GetBoatAtPosition(Position position);
    string GetBoatSymbol(Position position);
    string GetCellColor(Position position);
    bool IsFinished(AttackResponse responseData);
}

public class GameService(Grpc.GameService.GameServiceClient gameClient, IGameModalService modalService, NavigationManager navManager) : IGameService
{
    public Guid? gameId { get; set; }
    public List<Boat>? boats { get; set; }
    public required Grid playerGrid { get; set; }
    public required Grid oponnentGrid { get; set; }
    private Dictionary<Position, Boat> boatPositions = new Dictionary<Position, Boat>();

    private readonly Grpc.GameService.GameServiceClient _gameClient = gameClient;
    private readonly IGameModalService _modalService = modalService;
    private readonly NavigationManager _navManager = navManager;

    public async Task Attack(Position position)
    {
        var attackRequest = new AttackRequest
        {
            GameId = gameId?.ToString(),
            AttackPosition = position
        };

        var attackResponse = await _gameClient.AttackAsync(attackRequest);

        if (attackResponse != null)
        {
            if (attackResponse.PlayerAttackResult == "Hit")
            {
                position.IsHit = true;
                oponnentGrid.Positions[position.X][position.Y] = new PositionWrapper(position);
            }
            if (attackResponse.ComputerAttackResult == "Hit")
            {
                Position playerBoatPosition = attackResponse.ComputerAttackPosition;
                playerBoatPosition.IsHit = true;
                playerGrid.Positions[playerBoatPosition.X][playerBoatPosition.Y] = new PositionWrapper(playerBoatPosition);
            }
            await HandleGameResult(attackResponse);
        }
    }

    public Boat? GetBoatAtPosition(Position position)
    {
        boatPositions.TryGetValue(position, out var boat);
        return boat;
    }

    public string GetBoatSymbol(Position position)
    {
        Boat? boat = GetBoatAtPosition(position);
        return boat != null ? boat.Name[0].ToString() : "";
    }

    public string GetCellColor(Position position)
    {
        return position != null && position.IsHit ? "red" : "white";
    }

    public async Task HandleGameResult(AttackResponse responseData)
    {
        if (IsFinished(responseData))
        {
            string title = "";
            string message = "";
            if (responseData.IsPlayerWinner)
            {
                title = "Victoire !";
                message = "Vous avez gagné !";
            }
            if (responseData.IsComputerWinner)
            {
                title = "Défaite";
                message = "Vous avez perdu";
            }

            var modalResult = await _modalService.ShowModal(title, message);

            if (modalResult == "restart")
            {
                await StartGame();
            }
            else if (modalResult == "return")
            {
                _navManager.NavigateTo("/");
            }
        }
    }

    public void InitializeBoatPositions()
    {
        boatPositions.Clear();
        foreach (var boat in boats)
        {
            foreach (var position in boat.Positions)
            {
                boatPositions[position] = boat;
            }
        }
    }

    public bool IsBoatAtPosition(Position position)
    {
        return boatPositions.ContainsKey(position);
    }

    public bool IsFinished(AttackResponse responseData)
    {
        return responseData.IsPlayerWinner || responseData.IsComputerWinner;
    }

    public async Task StartGame()
    {
        var startGameRequest = new StartGameRequest();
        var startGameResponse = await _gameClient.StartGameAsync(startGameRequest);

        playerGrid = new Grid(10, 10);
        oponnentGrid = new Grid(10, 10);

        if (startGameResponse != null)
        {
            gameId = new Guid(startGameResponse.GameId);
            boats = startGameResponse.PlayerBoats.ToList();
        }
        InitializeBoatPositions();
    }
}