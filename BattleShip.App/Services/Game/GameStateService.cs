namespace BattleShip.Services.Game;

using BattleShip.Components;
using BattleShip.Models;
using BattleShip.Models.Response;

public interface IGameStateService
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
    void PlaceBoat(List<Position> positions);
    bool IsBoatAtPosition(Position position);
    void RestorePreviousState(RollbackResponse rollbackResponse);
}

public class GameStateService : IGameStateService
{
    public Guid? GameId { get; set; }
    public List<Boat> Boats { get; private set; } = new List<Boat>();
    public Grid PlayerGrid { get; private set; }
    public Grid OpponentGrid { get; private set; }
    public List<string> Historique { get; private set; } = new List<string>();
    public bool IsPlacingBoat { get; set; } = true;
    public GameParameter GameParameter { get; set; } = new GameParameter();

    private readonly IGameEventService _eventService;

    public GameStateService(IGameEventService eventService)
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
        UpdateGrid(attackResponse.PlayerAttackPosition, attackResponse.PlayerIsHit, OpponentGrid);
        RecordAttack(attackResponse.PlayerAttackPosition, attackResponse.PlayerIsHit, attackResponse.PlayerIsSunk, "Le joueur");

        UpdateGrid(attackResponse.AiAttackPosition, attackResponse.AiIsHit ?? false, PlayerGrid);
        RecordAttack(attackResponse.AiAttackPosition, attackResponse.AiIsHit ?? false, attackResponse.AiIsSunk ?? false, "L'ordinateur");
        _eventService.NotifyChange();   
    }

    private void UpdateGrid(Position position, bool isHit, Grid grid)
    {
        position.IsHit = isHit;
        var state = isHit ? PositionState.HIT : PositionState.MISS;

        grid.PositionsData[position.X][position.Y].Position = position;
        grid.PositionsData[position.X][position.Y].State = state;
    }

    private void RecordAttack(Position position, bool isHit, bool isSunk, string attacker)
    {
        string result = isHit ? "Touché" : "Raté";
        string sinkInfo = isSunk ? " et a coulé un bateau" : "";

        Historique.Add($"{attacker} a attaqué la position ({position.X}, {position.Y}) - {result}{sinkInfo}.");
    }

    public void PlaceBoat(List<Position> positions)
    {
        Boats.Add(new Boat(positions));
    }

    public bool IsBoatAtPosition(Position position)
    {
        return Boats.Any(boat => boat.Positions.Any(p => p.X == position.X && p.Y == position.Y));
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

