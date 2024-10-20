namespace BattleShip.Services.Game;

using BattleShip.Components;
using BattleShip.Models;
using BattleShip.Models.Response;
using BattleShip.Models.State;
using BattleShip.Utils;

public interface IGameStateSoloService
{
    Guid? GameId { get; set; }
    List<Boat> Boats { get; }
    Grid PlayerGrid { get; }
    Grid OpponentGrid { get; }
    List<string> Historique { get; }
    bool IsPlacingBoat { get; set; }
    GameParameter GameParameter { get; set; }

    void InitializeGame(Guid? gameId);
    void UpdateGameState(AttackModel.AttackResponse attackResponse);
    void RestorePreviousState(RollbackResponse rollbackResponse);
}

public class GameStateSoloService : IGameStateSoloService
{
    public Guid? GameId { get; set; }
    public List<Boat> Boats { get; private set; } = new List<Boat>();
    public Grid PlayerGrid { get; private set; }
    public Grid OpponentGrid { get; private set; }
    public List<string> Historique { get; private set; } = new List<string>();
    public bool IsPlacingBoat { get; set; } = true;
    public GameParameter GameParameter { get; set; } = new GameParameter();

    private readonly IGameEventService _eventService;

    public GameStateSoloService(IGameEventService eventService)
    {
        _eventService = eventService;
    }

    public void InitializeGame(Guid? gameId)
    {
        int GridSize = GameParameter.GridSize;
        GameId = gameId;
        PlayerGrid = new Grid(GridSize, GridSize);
        OpponentGrid = new Grid(GridSize, GridSize);
        Boats.Clear();
        Historique.Clear();
        IsPlacingBoat = true;
    }

    public void UpdateGameState(AttackModel.AttackResponse attackResponse)
    {
        GridUtils.UpdateGrid(attackResponse.PlayerAttackPosition, attackResponse.PlayerIsHit, OpponentGrid);
        GridUtils.RecordAttack(Historique, attackResponse.PlayerAttackPosition, attackResponse.PlayerIsHit, attackResponse.PlayerIsSunk, "Le joueur");

        GridUtils.UpdateGrid(attackResponse.AiAttackPosition, attackResponse.AiIsHit ?? false, PlayerGrid);
        GridUtils.RecordAttack(Historique, attackResponse.AiAttackPosition, attackResponse.AiIsHit ?? false, attackResponse.AiIsSunk ?? false, "L'ordinateur");
        _eventService.NotifyChange();   
    }

    public void RestorePreviousState(RollbackResponse rollbackResponse)
    {

        var computerPosition = rollbackResponse.LastPlayerAttackPosition;
        var playerPosition = rollbackResponse.LastIaAttackPosition;
        PlayerGrid.PositionsData[playerPosition.X][playerPosition.Y].Position = playerPosition;
        PlayerGrid.PositionsData[playerPosition.X][playerPosition.Y].State = null;

        OpponentGrid.PositionsData[computerPosition.X][computerPosition.Y].Position = computerPosition;
        OpponentGrid.PositionsData[computerPosition.X][computerPosition.Y].State = null;

        if (Historique.Any())
        {
            Historique.RemoveAt(Historique.Count - 1);
        }
    }
}

