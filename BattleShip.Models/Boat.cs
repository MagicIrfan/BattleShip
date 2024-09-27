namespace BattleShip.Models;

public class Boat(string name, Position startPosition, bool isVertical, int size)
{
    public string Name { get; set; } = name;
    public Position StartPosition { get; set; } = startPosition;
    public bool IsVertical { get; set; } = isVertical;
    public int Size { get; set; } = size;
    public List<Position> Positions { get; set; } = [];
}