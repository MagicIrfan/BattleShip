namespace BattleShip.Models;
public class GameResult(
    Guid gameId,
    string playerAttackResult,
    Position computerAttackPosition,
    string computerAttackResult,
    bool isPlayerWinner,
    bool isComputerWinner)
{
    public Guid GameId { get; set; } = gameId;
    public string PlayerAttackResult { get; set; } = playerAttackResult;
    public Position ComputerAttackPosition { get; set; } = computerAttackPosition;
    public string ComputerAttackResult { get; set; } = computerAttackResult;
    public bool IsPlayerWinner { get; set; } = isPlayerWinner;
    public bool IsComputerWinner { get; set; } = isComputerWinner;
}
