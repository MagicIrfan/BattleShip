using BattleShip.Models;
using Microsoft.IdentityModel.Tokens;

namespace BattleShip.API.Helpers;

public static class GameHelper
{
    private const int GridSize = 10;

    public static bool ValidateBoatPositions(List<Boat> boats)
    {
        var allPositions = new HashSet<(int X, int Y)>();

        foreach (var position in boats.SelectMany(boat => boat.Positions))
        {
            if (position.X >= GridSize || position.Y >= GridSize || position.X < 0 || position.Y < 0)
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
            _ when playerId == gameState.PlayerOneId => gameState.PlayerOneBoats,
            _ when playerId == gameState.PlayerTwoId || gameState.PlayerTwoId == "IA" => gameState.PlayerTwoBoats,
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
    
    public static List<Boat> GenerateRandomBoats()
    {
        var random = new Random();
        var boats = new List<Boat>();

        while (boats.Count < 2)
        {
            var startPosition = new Position(random.Next(0, GridSize), random.Next(0, GridSize));
            var isVertical = random.Next(0, 2) == 0;
            var size = boats.Count == 0 ? 3 : 4;

            /*var boat = new Boat(boats.Count == 0 ? "Destroyer" : "Cruiser", startPosition, isVertical, size);

            if (BoatPlacementHelper.PlaceBoat(boat, boats))
                boats.Add(boat);*/
        }

        return boats;
    }
}