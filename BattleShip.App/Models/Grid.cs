namespace BattleShip.Models;
public class Grid
{
    public Position[][] Positions { get; set; }

    public Grid(int rows, int cols)
    {
        Positions = new Position[rows][];
        for (int i = 0; i < rows; i++)
        {
            Positions[i] = new Position[cols];
            for (int j = 0; j < cols; j++)
            {
                Positions[i][j] = new Position(i, j);
            }
        }
    }
}
