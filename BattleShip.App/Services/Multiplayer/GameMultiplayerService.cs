namespace BattleShip.Services.Multiplayer;

using BattleShip.Components;
using BattleShip.Models;
using BattleShip.Models.State;
using BattleShip.Services;
using BattleShip.Services.Game;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;
using BattleShip.Utils;

public interface IGameMultiplayerService
{
    Task StartGame(Guid _gameId);
    Task Attack(Position attackPosition);
    Task CreateHubConnection();
    Task PlaceBoats();
    void PlaceBoat(List<Position> positions);
    bool IsBoatAtPosition(Position position);
    Task LeaveGame();
    Grid GetPlayerGrid();
    Grid GetOpponentGrid();
    bool IsPlacingBoat();
    Guid? GetGameId();
    List<string> GetHistorique();
    List<Boat> GetBoats();
    PlayerInfo GetPlayer();
    PlayerInfo GetOpponent();
    string GetTurnStatus();
    bool CanPlaceBoat(int row, int col, bool isVertical, int boatSize, PositionData[][] positionsData);
    bool ArePositionsOverlapping(int row, int col, bool isVertical, int boatSize);
    List<Position> GetBoatPositions(Position position, bool isVertical, int size);
}

public class GameMultiplayerService : IGameMultiplayerService
{
    private HubConnection HubConnection;
    private readonly IUserService _userService;
    private readonly IGameEventService gameEventService;
    private readonly IBoatPlacementService _boatPlacementService;
    private readonly IGameUIService _gameUIService;
    private readonly IGameModalService _modalService;
    private readonly IGameEventService _eventService;
    private readonly SignalRService _signalRService;
    private readonly IGameStateMultiplayerService _gameStateMultiplayerService;

    public GameMultiplayerService( 
        IGameModalService modalService, 
        IGameEventService eventService, 
        SignalRService signalRService, 
        IUserService userService, 
        IGameUIService gameUIService, 
        IBoatPlacementService boatPlacementService,
        IGameStateMultiplayerService gameStateMultiplayerService)
    {
        _modalService = modalService;
        _eventService = eventService;
        _signalRService = signalRService;
        _userService = userService;
        _gameUIService = gameUIService;
        _boatPlacementService = boatPlacementService;
        _gameStateMultiplayerService = gameStateMultiplayerService;
    }

    public async Task Attack(Position attackPosition)
    {
        var gameId = _gameStateMultiplayerService.GameId;
        await HubConnection.SendAsync("SendAttack", gameId, attackPosition);
        await HubConnection.SendAsync("CheckPlayerTurn", gameId);
        await _eventService.NotifyChange();
    }

    public async Task StartGame(Guid _gameId)
    {
        _gameStateMultiplayerService.InitializeGame(_gameId);
        var gameId = _gameStateMultiplayerService.GameId;
        _gameStateMultiplayerService.Player = await _userService.LoadPlayerProfile();
        await HubConnection.SendAsync("SendProfile", gameId, _gameStateMultiplayerService.Player.Username, _gameStateMultiplayerService.Player.Picture);
    }

    public async Task CreateHubConnection()
    {
        HubConnection = _signalRService.GetConnection();

        HubConnection.On<string?>("BoatPlaced", (playerId) =>
        {
            Console.WriteLine($"Le joueur {playerId} a bien placé ses bateaux !");
        });

        HubConnection.On("BothPlayersReady", async () =>
        {
            _gameStateMultiplayerService.IsPlacingBoat = false;
            await HubConnection.SendAsync("CheckPlayerTurn", _gameStateMultiplayerService.GameId);
        });

        HubConnection.On<AttackModel.AttackResponse?>("AttackResult", async (attackResponse) =>
        {
            _gameStateMultiplayerService.UpdateOpponentGameState(attackResponse);

            if (attackResponse.PlayerIsWinner)
            {
                Console.WriteLine("Gagné !");
                var result = await _modalService.ShowModal<GameModal>("Gagné", "Vous avez gagné la partie");
                if (result == "return")
                {
                    _gameUIService.NavigateTo("/");
                }
            }
        });

        HubConnection.On<AttackModel.AttackResponse?>("ReceiveAttackResult", async (attackResponse) =>
        {
            _gameStateMultiplayerService.UpdatePlayerGameState(attackResponse);
            if (attackResponse.PlayerIsWinner)
            {
                Console.WriteLine("Perdu !");
                var result = await _modalService.ShowModal<GameModal>("Perdu", "Vous avez perdu la partie");
                if (result == "return")
                {
                    _gameUIService.NavigateTo("/");
                }
            }

            GridUtils.RecordAttack(_gameStateMultiplayerService.Historique, attackResponse.PlayerAttackPosition, attackResponse.PlayerIsHit, attackResponse.PlayerIsSunk, _gameStateMultiplayerService.Player.Username);

            await _eventService.NotifyChange();
        });

        HubConnection.On<bool>("IsPlayerTurn", async (isPlayerTurn) =>
        {
            _gameStateMultiplayerService.TurnStatus = isPlayerTurn ? "C'est ton tour !" : "Au tour de l'adversaire";
            await _eventService.NotifyChange();
        });

        HubConnection.On<PlayerInfo>("SendPlayerInfo", async (player) =>
        {
            _gameStateMultiplayerService.Opponent = player;
            await _eventService.NotifyChange();
        });

        HubConnection.On("GameIsFinished", async () =>
        {
            var result = await _modalService.ShowModal<GameModal>("Gagné", "Vous avez gagné la partie");
            if (result == "return")
            {
                _gameUIService.NavigateTo("/");
            }
        });
    }

    public async Task PlaceBoats()
    {
        await HubConnection.SendAsync("PlaceBoat", GetBoats(), GetGameId());
    }

    public void PlaceBoat(List<Position> positions)
    {
        _boatPlacementService.PlaceBoat(GetBoats(), positions);
    }

    public bool IsBoatAtPosition(Position position)
    {
        return _boatPlacementService.IsBoatAtPosition(GetBoats(), position);
    }

    public async Task LeaveGame()
    {
        if (HubConnection != null)
        {
            await HubConnection.SendAsync("LeaveGame", GetGameId());
            await HubConnection.DisposeAsync();
        }
    }

    public Grid GetPlayerGrid()
    {
        return _gameStateMultiplayerService.PlayerGrid;
    }

    public Grid GetOpponentGrid()
    {
        return _gameStateMultiplayerService.OpponentGrid;
    }

    public bool IsPlacingBoat()
    {
        return _gameStateMultiplayerService.IsPlacingBoat;
    }

    public Guid? GetGameId()
    {
        return _gameStateMultiplayerService.GameId;
    }

    public List<string> GetHistorique()
    {
        return _gameStateMultiplayerService.Historique;
    }

    public List<Boat> GetBoats()
    {
        return _gameStateMultiplayerService.Boats;
    }

    public PlayerInfo GetPlayer()
    {
        return _gameStateMultiplayerService.Player;
    }

    public PlayerInfo GetOpponent()
    {
        return _gameStateMultiplayerService.Opponent;
    }

    public string GetTurnStatus()
    {
        return _gameStateMultiplayerService.TurnStatus;
    }

    public bool CanPlaceBoat(int row, int col, bool isVertical, int boatSize, PositionData[][] positionsData)
    {
        return _boatPlacementService.CanPlaceBoat(row, col, isVertical, boatSize, positionsData);
    }

    public bool ArePositionsOverlapping(int row, int col, bool isVertical, int boatSize)
    {
        return _boatPlacementService.ArePositionsOverlapping(row, col, isVertical, boatSize, GetBoats());
    }

    public List<Position> GetBoatPositions(Position position, bool isVertical, int size)
    {
        return _boatPlacementService.GetBoatPositions(position, isVertical, size);
    }
}
