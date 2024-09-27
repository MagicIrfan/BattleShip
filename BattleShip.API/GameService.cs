using BattleShip.Models;

namespace BattleShip.API;

public interface IGameService
{
    List<Boat> GenerateRandomBoats();
    bool ProcessAttack(List<Boat> boats, Position attackPosition);
    bool CheckIfAllBoatsSunk(List<Boat> boats);
    Position GenerateRandomPosition();
}

public class GameService : IGameService
{
    private const int GridSize = 10;
    
    public List<Boat> GenerateRandomBoats()
    {
        var random = new Random();
        var boats = new List<Boat>();

        while (boats.Count < 2) 
        {
            var startPosition = new Position(random.Next(0, GridSize), random.Next(0, GridSize));
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

            if (position.X >= GridSize || position.Y >= GridSize || position.X < 0 || position.Y < 0) 
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

    public bool CheckIfAllBoatsSunk(List<Boat> boats)
    {
        return boats.All(b => b.Positions.All(p => p.IsHit));
    }

    public Position GenerateRandomPosition()
    {
        var random = new Random();
        return new Position(random.Next(0, GridSize), random.Next(0, GridSize));
    }
}