using BattleShip.Models;

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
}
