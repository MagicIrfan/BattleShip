using BattleShip.Models;

namespace BattleShip.API.Helpers;

public class IaHelper
{
    private const int GridSize = 10;
    
    public static Task<Position> GenerateIaAttackRequest(GameState gameState)
    {
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
                var attackPosition = new Position(random.Next(0, GridSize), random.Next(0, GridSize));
                targetPositions.Add(attackPosition);
            }
        }

        var selectedPosition = targetPositions.FirstOrDefault(pos =>
                                   pos.X is >= 0 and < GridSize && pos.Y is >= 0 and < GridSize &&
                                   !history.Any(h => h.AttackPosition.X == pos.X && h.AttackPosition.Y == pos.Y)) ??
                               new Position(random.Next(0, GridSize), random.Next(0, GridSize));

        return Task.FromResult(selectedPosition);
    }
}