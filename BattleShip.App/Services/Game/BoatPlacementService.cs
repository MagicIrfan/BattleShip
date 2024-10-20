using BattleShip.Models;

namespace BattleShip.Services.Game;

public interface IBoatPlacementService
{
    bool CanPlaceBoat(int row, int col, bool isVertical, int boatSize, PositionData[][] positionsData);
    List<Position> GetBoatPositions(Position position, bool isVertical, int size);
    void PlaceBoat(List<Boat> boats, List<Position> positions);
    bool IsBoatAtPosition(List<Boat> boats, Position position);
    bool ArePositionsOverlapping(int row, int col, bool isVertical, int boatSize, List<Boat> boats);
}

public class BoatPlacementService : IBoatPlacementService
{
    public bool CanPlaceBoat(int row, int col, bool isVertical, int boatSize, PositionData[][] positionsData)
    {
        if (isVertical)
        {
            if (row < 0 || row + boatSize > positionsData.Length || col < 0 || col >= positionsData[0].Length)
                return false;
        }
        else
        {
            if (col < 0 || col + boatSize > positionsData[0].Length || row < 0 || row >= positionsData.Length)
                return false;
        }

        return true;
    }

    public bool ArePositionsOverlapping(int row, int col, bool isVertical, int boatSize, List<Boat> boats)
    {
        return boats.Any(boat =>
            boat.Positions.Any(existingPosition =>
            Enumerable.Range(0, boatSize).Any(i =>
                existingPosition.X == (isVertical ? col : col + i) &&
                existingPosition.Y == (isVertical ? row + i : row)
            )));
    }

    public List<Position> GetBoatPositions(Position position, bool isVertical, int size)
    {
        var boatPositions = new List<Position>();

        for (int i = 0; i < size; i++)
        {
            boatPositions.Add(isVertical ? new Position(position.X, position.Y + i) : new Position(position.X + i, position.Y));
        }

        return boatPositions;
    }

    public void PlaceBoat(List<Boat> boats, List<Position> positions)
    {
        boats.Add(new Boat(positions));
    }

    public bool IsBoatAtPosition(List<Boat> boats, Position position)
    {
        return boats.Any(boat => boat.Positions.Any(p => p.X == position.X && p.Y == position.Y));
    }
}
