using BattleShip.Grpc;

namespace BattleShip.Models;

public class PositionWrapper
{
    public Position Position { get; private set; }

    public PositionWrapper(int x, int y)
    {
        Position = new Position
        {
            X = x,
            Y = y,
            IsHit = false
        };
    }

    public PositionWrapper(Position position)
    {
        Position = position;
    }
}
