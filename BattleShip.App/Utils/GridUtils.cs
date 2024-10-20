using BattleShip.Models;
using BattleShip.Models.State;

namespace BattleShip.Utils;

public static class GridUtils
{
    public static string GetCellBackgroundColor(PositionData positionData)
    {
        return positionData.State switch
        {
            PositionState.HIT => "red",
            PositionState.MISS => "blue",
            _ => "transparent"
        };
    }

    public static void UpdateGrid(Position position, bool isHit, Grid grid)
    {
        position.IsHit = isHit;
        var state = isHit ? PositionState.HIT : PositionState.MISS;

        grid.PositionsData[position.X][position.Y].Position = position;
        grid.PositionsData[position.X][position.Y].State = state;
    }

    public static void RecordAttack(List<string> historique, Position position, bool isHit, bool isSunk, string attacker)
    {
        string result = isHit ? "Touché" : "Raté";
        string sinkInfo = isSunk ? " et a coulé un bateau" : "";

        historique.Add($"{attacker} a attaqué la position ({position.X}, {position.Y}) - {result}{sinkInfo}.");
    }
}
