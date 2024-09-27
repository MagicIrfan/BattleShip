using BattleShip.Models;

namespace BattleShip.API.Helpers;

public static class BoatPlacementHelper
{
    private const int GridSize = 10;

    public static bool PlaceBoat(Boat boat, List<Boat> existingBoats)
    {
        boat.Positions = [];

        for (var i = 0; i < boat.Size; i++)
        {
            var position = boat.IsVertical
                ? new Position(boat.StartPosition.X, boat.StartPosition.Y + i)
                : new Position(boat.StartPosition.X + i, boat.StartPosition.Y);

            if (position.X >= GridSize || position.Y >= GridSize || position.X < 0 || position.Y < 0) 
                return false;

            if (existingBoats.Any(b => b.Positions.Any(p => p.X == position.X && p.Y == position.Y)))
                return false;

            boat.Positions.Add(position);
        }

        return true;
    }
}