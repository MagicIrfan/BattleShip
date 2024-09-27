using BattleShip.Models;

namespace BattleShip.API;

public class GameService
{
    public List<Boat> GenerateRandomBoats()
    {
        var random = new Random();
        var boats = new List<Boat>();

        while (boats.Count < 2) 
        {
            var startPosition = new Position(random.Next(0, 10), random.Next(0, 10));
            var isVertical = random.Next(0, 2) == 0; 
            var size = boats.Count == 0 ? 3 : 4;

            var boat = new Boat(boats.Count == 0 ? "Destroyer" : "Cruiser", startPosition, isVertical, size);

            if (PlaceBoat(boat, boats)) 
                boats.Add(boat);
        }

        return boats;
    }

    private bool PlaceBoat(Boat boat, List<Boat> existingBoats)
    {
        boat.Positions = []; 

        for (var i = 0; i < boat.Size; i++)
        {
            var position = boat.IsVertical
                ? new Position(boat.StartPosition.X, boat.StartPosition.Y + i)
                : new Position(boat.StartPosition.X + i, boat.StartPosition.Y);

            if (position.X >= 10 || position.Y >= 10 || position.X < 0 || position.Y < 0) 
                return false;

            if (existingBoats.Any(b => b.Positions.Any(p => p.X == position.X && p.Y == position.Y)))
                return false;

            boat.Positions.Add(position);
        }

        return true;
    }
    
    public bool ProcessAttack(List<Boat> boats, Position attackPosition)
    {
        foreach (var positionHit in boats.Select(boat => boat.Positions.FirstOrDefault(p => p.X == attackPosition.X && p.Y == attackPosition.Y)).OfType<Position>())
        {
            positionHit.IsHit = true;
            return true; 
        }

        return false;
    }

}