using BattleShip.Models;

public class Grid
{
    public PositionData[][] PositionsData { get; set; }

    public Grid(int rows, int cols)
    {
        PositionsData = new PositionData[rows][];
        for (int i = 0; i < rows; i++)
        {
            PositionsData[i] = new PositionData[cols];
            for (int j = 0; j < cols; j++)
            {
                PositionsData[i][j] = new PositionData
                {
                    Position = new Position(i, j)
                };
            }
        }
    }
}
