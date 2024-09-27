namespace BattleShip.Models;

public class Position(int x, int y)
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
    public bool IsHit { get; set; } = false;
}