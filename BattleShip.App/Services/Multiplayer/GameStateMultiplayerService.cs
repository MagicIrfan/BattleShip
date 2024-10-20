namespace BattleShip.Services.Multiplayer;

using BattleShip.Models.Response;
using BattleShip.Models.State;
using BattleShip.Models;
using BattleShip.Services.Game;
using BattleShip.Utils;

public interface IGameStateMultiplayerService
{
    Guid? GameId { get; set; }
    List<Boat> Boats { get; set; }
    Grid PlayerGrid { get; set; }
    Grid OpponentGrid { get; set; }
    List<string> Historique { get; set; }
    string TurnStatus { get; set; }
    bool IsPlacingBoat { get; set; }
    PlayerInfo Player { get; set; }
    PlayerInfo Opponent { get; set; }
    bool IsReady { get; set; }
    void InitializeGame(Guid? gameId);
    void UpdateOpponentGameState(AttackModel.AttackResponse attackResponse);
    void UpdatePlayerGameState(AttackModel.AttackResponse attackResponse);
}

public class GameStateMultiplayerService : IGameStateMultiplayerService
{
    public Guid? GameId { get; set; }
    public required List<Boat> Boats { get; set; } = new List<Boat>();
    public required Grid PlayerGrid { get; set; }
    public required Grid OpponentGrid { get; set; }
    public required List<string> Historique { get; set; } = new List<string>();
    public required bool IsPlacingBoat { get; set; } = true;
    public required bool IsReady { get; set; } = false;
    public string TurnStatus { get; set; }
    public PlayerInfo Player { get; set; }
    public PlayerInfo Opponent { get; set; }

    public void InitializeGame(Guid? gameId)
    {
        GameId = gameId;
        PlayerGrid = new Grid(10,10);
        OpponentGrid = new Grid(10,10);
        Boats.Clear();
        Historique.Clear();
        IsPlacingBoat = true;
        IsReady = false;
        TurnStatus = "Les joueurs doivent placer leurs bateaux";
    }

    public void UpdateOpponentGameState(AttackModel.AttackResponse attackResponse)
    {
        GridUtils.UpdateGrid(attackResponse.PlayerAttackPosition, attackResponse.PlayerIsHit, OpponentGrid);
        GridUtils.RecordAttack(Historique, attackResponse.PlayerAttackPosition, attackResponse.PlayerIsHit, attackResponse.PlayerIsSunk, Player.Username);
    }

    public void UpdatePlayerGameState(AttackModel.AttackResponse attackResponse)
    {
        GridUtils.UpdateGrid(attackResponse.PlayerAttackPosition, attackResponse.PlayerIsHit, PlayerGrid);
        GridUtils.RecordAttack(Historique, attackResponse.PlayerAttackPosition, attackResponse.PlayerIsHit, attackResponse.PlayerIsSunk, Opponent.Username);
    }
}

