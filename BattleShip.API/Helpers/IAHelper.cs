using BattleShip.Models;
using BattleShip.Models.State;

namespace BattleShip.API.Helpers;

public static class IaHelper
{
    public static Task<Position> GenerateIaAttackRequest(GameState gameState)
    {
        return gameState.Difficulty switch
        {
            1 => GenerateIaAttackRequestEasy(gameState),
            2 => GenerateIaAttackRequestMedium(gameState),
            3 => GenerateIaAttackRequestHard(gameState),
            _ => throw new ArgumentException("Invalid difficulty level")
        };
    }

    private static Task<Position> GenerateIaAttackRequestMedium(GameState gameState) {
    var gridSize = gameState.GridSize;
    var random = new Random();
    var history = gameState.AttackHistory.Where(x => x.PlayerId == "IA").ToList();

    var targetPositions = new HashSet<Position>();

    if (history.Any(h => h.IsHit))
    {
        var lastHit = history.Last(h => h.IsHit);
        var hitPosition = lastHit.AttackPosition;

        targetPositions.Add(new Position(hitPosition.X - 1, hitPosition.Y));
        targetPositions.Add(new Position(hitPosition.X + 1, hitPosition.Y));
        targetPositions.Add(new Position(hitPosition.X, hitPosition.Y - 1));
        targetPositions.Add(new Position(hitPosition.X, hitPosition.Y + 1));
    }

    if (targetPositions.Count == 0)
    {
        while (targetPositions.Count < 5)
        {
            var attackPosition = new Position(random.Next(0, gridSize), random.Next(0, gridSize));
            if (!history.Any(h => h.AttackPosition.X == attackPosition.X && h.AttackPosition.Y == attackPosition.Y))
            {
                targetPositions.Add(attackPosition);
            }
        }
    }

    var validPositions = targetPositions.Where(pos =>
                               pos.X >= 0 && pos.X < gridSize && 
                               pos.Y >= 0 && pos.Y < gridSize &&
                               !history.Any(h => h.AttackPosition.X == pos.X && h.AttackPosition.Y == pos.Y)).ToList();

    Position selectedPosition;
    if (validPositions.Count > 0)
    {
        selectedPosition = validPositions.First();
    }
    else
    {
        selectedPosition = new Position(random.Next(0, gridSize), random.Next(0, gridSize));
        while (history.Any(h => h.AttackPosition.X == selectedPosition.X && h.AttackPosition.Y == selectedPosition.Y))
        {
            selectedPosition = new Position(random.Next(0, gridSize), random.Next(0, gridSize));
        }
    }

    return Task.FromResult(selectedPosition);
}

    private static Task<Position> GenerateIaAttackRequestEasy(GameState gameState)
    {
        var random = new Random();
        var gridSize = gameState.GridSize;
        var history = gameState.AttackHistory.Where(x => x.PlayerId == "IA").ToList();

        Position attackPosition;
        do
        {
            attackPosition = new Position(random.Next(0, gridSize), random.Next(0, gridSize));
        } while (history.Any(h => h.AttackPosition.X == attackPosition.X && h.AttackPosition.Y == attackPosition.Y));

        return Task.FromResult(attackPosition);
    }
    
    private static Task<Position> GenerateIaAttackRequestHard(GameState gameState)
    {
        var gridSize = gameState.GridSize;
        var random = new Random();
        var history = gameState.AttackHistory.Where(x => x.PlayerId == "IA").ToList();
        var targetPositions = new HashSet<Position>();

        if (history.Any(h => h.IsHit))
        {
            var lastHit = history.Last(h => h.IsHit);
            var hitPosition = lastHit.AttackPosition;

            targetPositions.Add(new Position(hitPosition.X - 1, hitPosition.Y));
            targetPositions.Add(new Position(hitPosition.X + 1, hitPosition.Y));
            targetPositions.Add(new Position(hitPosition.X, hitPosition.Y - 1));
            targetPositions.Add(new Position(hitPosition.X, hitPosition.Y + 1));
        }
        else
        {
            for (var x = 0; x < gridSize; x++)
            {
                for (var y = 0; y < gridSize; y++)
                {
                    if ((x + y) % 2 == 0) 
                    {
                        var potentialPosition = new Position(x, y);
                        if (history.All(h => h.AttackPosition != potentialPosition))
                        {
                            targetPositions.Add(potentialPosition);
                        }
                    }
                }
            }
        }

        Position selectedPosition;
        var maxAttempts = gridSize * gridSize;
        var attempts = 0;

        do
        {
            selectedPosition = targetPositions.FirstOrDefault(pos =>
                                   pos.X >= 0 && pos.X < gridSize &&
                                   pos.Y >= 0 && pos.Y < gridSize) 
                               ?? new Position(random.Next(0, gridSize), random.Next(0, gridSize));
        
            attempts++;
        } while (history.Any(h => h.AttackPosition.X == selectedPosition.X && 
                                  h.AttackPosition.Y == selectedPosition.Y) && attempts < maxAttempts);

        return Task.FromResult(selectedPosition);
    }

}