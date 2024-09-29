namespace BattleShip.Models;

public class GameResult
{
    public Guid GameId { get; set; }
    public string PlayerAttackResult { get; set; }
    public Position ComputerAttackPosition { get; set; }
    public string ComputerAttackResult { get; set; }
    public bool IsPlayerWinner { get; set; }
    public bool IsComputerWinner { get; set; }
}
