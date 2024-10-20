namespace BattleShip.Services;

using BattleShip.Components;
using BattleShip.Models;
using BattleShip.Services.Game;
using Google.Protobuf;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System.Text.Json;

public interface IGameMultiplayerService
{
    public event Func<Task>? OnStateChanged;
    Guid gameId { get; set; }
    List<Boat> boats { get; set; }
    Grid playerGrid { get; set; }
    Grid opponentGrid { get; set; }
    List<string> historique { get; set; }
    bool IsPlacingBoat { get; set; }
    Task StartGame(Guid _gameId);
    Task Attack(Position attackPosition);
    Task CreateHubConnection();
    Task PlaceBoats();
    void PlaceBoat(List<Position> positions);
    bool IsBoatAtPosition(Position position);
    Task NotifyChange();
}
public class GameMultiplayerService : IGameMultiplayerService
{
    private HubConnection HubConnection;
    private readonly ITokenService _tokenService;
    public Guid gameId { get; set; }
    public required List<Boat> boats { get; set; } = new List<Boat>();
    public required Grid playerGrid { get; set; }
    public required Grid opponentGrid { get; set; }
    public required List<string> historique { get; set; } = new List<string>();
    public required bool IsPlacingBoat { get; set; } = true;
    public event Func<Task>? OnStateChanged;

    private readonly IGameModalService _modalService;
    private readonly NavigationManager _navManager;
    private readonly IGameEventService _eventService;
    private readonly SignalRService _signalRService;

    public GameMultiplayerService(ITokenService tokenService , IGameModalService modalService, NavigationManager navManager, IGameEventService eventService, SignalRService signalRService)
    {
        _tokenService = tokenService;
        _modalService = modalService;
        _navManager = navManager;
        _eventService = eventService;
        _signalRService = signalRService;
    }

    public async Task NotifyChange()
    {
        if (OnStateChanged != null)
        {
            await OnStateChanged.Invoke(); // Déclenche l'événement pour les composants abonnés
        }
    }

    public async Task Attack(Position attackPosition)
    {
        await HubConnection.SendAsync("SendAttack", gameId, attackPosition);
    }

    public async Task StartGame(Guid _gameId)
    {
        gameId = _gameId;
        await HubConnection.SendAsync("SetReady", gameId);
        playerGrid = new Grid(10, 10);
        opponentGrid = new Grid(10, 10);
        boats = new List<Boat>();
        historique = new List<string>();
        IsPlacingBoat = true;
    }

    public async Task CreateHubConnection()
    {
        var token = await _tokenService.GetAccessTokenAsync();

        // Créez la connexion au hub
        HubConnection = _signalRService.GetConnection();

        // Abonnez-vous aux événements SignalR ici
        HubConnection.On<string?>("BoatPlaced", (playerId) =>
        {
            Console.WriteLine($"Le joueur {playerId} a bien placé ses bateaux !");
        });

        HubConnection.On("BothPlayersReady", () =>
        {
            IsPlacingBoat = false;
        });

        HubConnection.On<AttackModel.AttackResponse?>("AttackResult", async (attackResponse) =>
        {
            Console.WriteLine("ATTAQUE");

            var attackResponseJson = JsonSerializer.Serialize(attackResponse, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(attackResponseJson);

            attackResponse.PlayerAttackPosition.IsHit = attackResponse.PlayerIsHit;
            var state = attackResponse.PlayerIsHit ? PositionState.HIT : PositionState.MISS;
            opponentGrid.PositionsData[attackResponse.PlayerAttackPosition.X][attackResponse.PlayerAttackPosition.Y].Position = attackResponse.PlayerAttackPosition;
            opponentGrid.PositionsData[attackResponse.PlayerAttackPosition.X][attackResponse.PlayerAttackPosition.Y].State = state;
            Console.WriteLine($"{opponentGrid.PositionsData[attackResponse.PlayerAttackPosition.X][attackResponse.PlayerAttackPosition.Y].Position.X} {opponentGrid.PositionsData[attackResponse.PlayerAttackPosition.X][attackResponse.PlayerAttackPosition.Y].Position.Y} {opponentGrid.PositionsData[attackResponse.PlayerAttackPosition.X][attackResponse.PlayerAttackPosition.Y].State}");

            if (attackResponse.PlayerIsWinner)
            {
                Console.WriteLine("Gagné !");
                var result = await _modalService.ShowModal<GameModal>("Gagné", "Vous avez gagné la partie");
                if (result == "restart")
                {
                    _eventService.RaiseGameRestarted();
                    await StartGame(gameId);
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
            await NotifyChange();
        });

        HubConnection.On<AttackModel.AttackResponse?>("ReceiveAttackResult", async (attackResponse) =>
        {
            UpdateGrid(attackResponse.PlayerAttackPosition, attackResponse.PlayerIsHit, playerGrid);
            if (attackResponse.PlayerIsWinner)
            {
                Console.WriteLine("Perdu !");
                var result = await _modalService.ShowModal<GameModal>("Perdu", "Vous avez perdu la partie");
                if (result == "restart")
                {
                    _eventService.RaiseGameRestarted();
                    await StartGame(gameId);
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
            await NotifyChange();
            await NotifyChange();
        });
    }

    private void UpdateGrid(Position position, bool isHit, Grid grid)
    {
        position.IsHit = isHit;
        var state = isHit ? PositionState.HIT : PositionState.MISS;
        grid.PositionsData[position.X][position.Y].Position = position;
        grid.PositionsData[position.X][position.Y].State = state;
        Console.WriteLine($"{grid.PositionsData[position.X][position.Y].Position} {state.ToString()}");
    }

    public async Task PlaceBoats()
    {
        Console.WriteLine($"{HubConnection.State}");
        Console.WriteLine($"{boats.Count} {gameId}");
        await HubConnection.SendAsync("PlaceBoat", boats, gameId);
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
