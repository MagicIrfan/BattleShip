namespace BattleShip.Models;

public class AttackResult
{
    public bool Hit { get; set; }
    public bool Sunk { get; set; }
    public bool GameWon { get; set; }
    public string Message { get; set; }
    public (int X, int Y) ComputerMove { get; set; }
    public bool ComputerHit { get; set; }
}
