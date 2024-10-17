using BattleShip.Models;
using Microsoft.IdentityModel.Tokens;

namespace BattleShip.API.Helpers;

public static class GameHelper
{
    public static bool ValidateBoatPositions(List<Boat> boats, int gridSize)
    {
        var allPositions = new HashSet<(int X, int Y)>();

        foreach (var position in boats.SelectMany(boat => boat.Positions))
        {
            if (position.X >= gridSize || position.Y >= gridSize || position.X < 0 || position.Y < 0)
                return false;

            if (!allPositions.Add((position.X, position.Y)))
                return false;
        }

        return true;
    }

    public static void ValidateTurn(GameState gameState, string playerId)
    {
        if (gameState.AttackHistory.IsNullOrEmpty())
        {
            if (!gameState.PlayerOneId.Equals(playerId))
                throw new UnauthorizedAccessException("User not allowed to play this turn");
        }
        else if (gameState.AttackHistory.Last().PlayerId.Equals(playerId))
        {
            throw new UnauthorizedAccessException("User not allowed to play this turn");
        }
    }

    public static List<Boat> GetPlayerBoats(GameState gameState, string playerId)
    {
        return playerId switch
        {
            _ when playerId == gameState.PlayerOneId => gameState.PlayerTwoBoats,
            _ when playerId == gameState.PlayerTwoId || gameState.PlayerTwoId == "IA" => gameState.PlayerOneBoats,
            _ => throw new UnauthorizedAccessException("Player not recognized in this game.")
        };
    }

    public static bool UpdateGameState(GameState gameState, string playerId, List<Boat> updatedBoats,
        IGameRepository gameRepository)
    {
        var isWinner = false;

        if (playerId == gameState.PlayerOneId)
        {
            gameState.PlayerOneBoats = updatedBoats;
            if (CheckIfAllBoatsSunk(updatedBoats))
            {
                isWinner = true;
                gameState.IsPlayerOneWinner = isWinner;
                gameRepository.UpdatePlayerWins(playerId);
            }
        }
        else
        {
            gameState.PlayerTwoBoats = updatedBoats;
            if (CheckIfAllBoatsSunk(updatedBoats))
            {
                isWinner = true;
                gameState.IsPlayerTwoWinner = isWinner;
                gameRepository.UpdatePlayerWins(playerId);
            }
        }

        return isWinner;
    }


    private static bool CheckIfAllBoatsSunk(List<Boat> boats)
    {
        return boats.All(b => b.Positions.All(p => p.IsHit));
    }
    
    public static List<Boat> GenerateRandomBoats(int? gridSize)
    {
        var effectiveGridSize = gridSize ?? 10;
        
        var random = new Random();
        var boatDefinitions = new Dictionary<string, int>
        {
            { "Porte-avions", 5 },
            { "Croiseur", 4 },
            { "Contre-torpilleur", 3 },
            { "Contre-torpilleur2", 3 },
            { "Torpilleur", 2 }
        };

        var boats = new List<Boat>();

        foreach (var (_, size) in boatDefinitions)
        {
            bool isValid;

            do
            {
                var positions = new List<Position>();
                var startX = random.Next(0, effectiveGridSize);
                var startY = random.Next(0, effectiveGridSize);
                var isVertical = random.Next(0, 2) == 0;

                for (var i = 0; i < size; i++)
                {
                    positions.Add(new Position(
                        isVertical ? startX : startX + i,
                        isVertical ? startY + i : startY
                    ));
                }

                var boat = new Boat(positions);
                boats.Add(boat);
                isValid = ValidateBoatPositions(boats, effectiveGridSize);

                if (!isValid)
                    boats.Remove(boat);

            } while (!isValid);
        }

        return boats;
    }
}