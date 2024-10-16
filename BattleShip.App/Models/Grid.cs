using BattleShip.Grpc;

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

    public bool AddBoat(Boat boat)
    {
        // Vérifie si le bateau peut être placé à ces positions
        foreach (var position in boat.Positions)
        {
            // Vérifie les limites de la grille
            if (position.X < 0 || position.X >= Positions.Length ||
                position.Y < 0 || position.Y >= Positions[0].Length)
            {
                return false; // Le bateau déborde de la grille
            }

            // Vérifie si la position est déjà occupée par un autre bateau
            if (Positions[position.X][position.Y] != null)
            {
                return false; // La position est déjà occupée
            }
        }

        // Si toutes les positions sont valides, ajoute le bateau
        foreach (var position in boat.Positions)
        {
            Positions[position.X][position.Y] = position; // Place le bateau
        }

        return true; // Bateau ajouté avec succès
    }
}
