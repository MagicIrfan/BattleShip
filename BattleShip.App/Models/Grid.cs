using BattleShip.Grpc;

namespace BattleShip.Models;

public class Grid
{
    public PositionWrapper[][] Positions { get; set; }

    public Grid(int rows, int cols)
    {
        Positions = new PositionWrapper[rows][];
        for (int i = 0; i < rows; i++)
        {
            Positions[i] = new PositionWrapper[cols];
            for (int j = 0; j < cols; j++)
            {
                Positions[i][j] = new PositionWrapper(i, j);
            }
        }
    }

}
